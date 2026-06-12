using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanAvatarView : MonoBehaviour
    {
        [SerializeField] private RawImage viewport;
        [SerializeField] private int textureSize = 768;
        [SerializeField] private float poseBlendSeconds = 0.28f;
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private string resourcesAvatarPath = "DigitalHuman/Avatar";
        [SerializeField] private Vector3 externalAvatarLocalPosition = new Vector3(0f, -0.85f, 0f);
        [SerializeField] private Vector3 externalAvatarLocalEuler = Vector3.zero;
        [SerializeField] private float externalAvatarScale = 1f;

        private readonly List<Transform> bones = new List<Transform>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<BoneWeight> boneWeights = new List<BoneWeight>();
        private readonly List<int>[] submeshTriangles =
        {
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<int>()
        };

        private RenderTexture renderTexture;
        private Camera avatarCamera;
        private Transform sceneRoot;
        private Transform modelRoot;
        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Material skinMaterial;
        private Material shirtMaterial;
        private Material pantsMaterial;
        private Material hairMaterial;
        private Material shoeMaterial;
        private Transform hips;
        private Transform spine;
        private Transform chest;
        private Transform neck;
        private Transform head;
        private Transform leftUpperArm;
        private Transform leftLowerArm;
        private Transform leftHand;
        private Transform rightUpperArm;
        private Transform rightLowerArm;
        private Transform rightHand;
        private Transform leftUpperLeg;
        private Transform leftLowerLeg;
        private Transform leftFoot;
        private Transform rightUpperLeg;
        private Transform rightLowerLeg;
        private Transform rightFoot;
        private Transform mouth;
        private GameObject externalAvatarInstance;
        private Animator externalAnimator;
        private bool usingExternalAvatar;
        private float animationSpeed = 1f;
        private Coroutine poseRoutine;
        private bool blendingPose;
        private DigitalHumanAvatarPose currentPose = DigitalHumanAvatarPose.Idle;

        private Quaternion leftUpperPoseRotation = Quaternion.identity;
        private Quaternion leftLowerPoseRotation = Quaternion.identity;
        private Quaternion rightUpperPoseRotation = Quaternion.identity;
        private Quaternion rightLowerPoseRotation = Quaternion.identity;
        private Quaternion leftLegPoseRotation = Quaternion.identity;
        private Quaternion rightLegPoseRotation = Quaternion.identity;
        private Quaternion headPoseRotation = Quaternion.identity;

        private enum AvatarMaterial
        {
            Skin = 0,
            Shirt = 1,
            Pants = 2,
            Hair = 3,
            Shoes = 4
        }

        private void Awake()
        {
            EnsureAvatarScene();
        }

        private void OnEnable()
        {
            DigitalHumanEventBus.AvatarPoseRequested += ApplyPose;
            DigitalHumanEventBus.AvatarCustomAnimationRequested += PlayCustomAnimation;
        }

        private void OnDisable()
        {
            DigitalHumanEventBus.AvatarPoseRequested -= ApplyPose;
            DigitalHumanEventBus.AvatarCustomAnimationRequested -= PlayCustomAnimation;
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        private void Update()
        {
            if (modelRoot == null || Mathf.Approximately(animationSpeed, 0f))
            {
                return;
            }

            if (externalAnimator != null)
            {
                externalAnimator.speed = animationSpeed;
            }

            if (!blendingPose && HasPoseBones())
            {
                float breath = Mathf.Sin(Time.time * 1.15f * animationSpeed);
                if (chest != null)
                {
                    chest.localRotation = Quaternion.Euler(breath * 1.4f, 0f, 0f);
                }

                if (spine != null)
                {
                    spine.localRotation = Quaternion.Euler(-breath * 0.75f, 0f, 0f);
                }

                ApplyLoopingPoseMotion();
            }

            if (mouth != null)
            {
                float talk = Mathf.Abs(Mathf.Sin(Time.time * 7.5f * animationSpeed));
                mouth.localScale = new Vector3(1f, Mathf.Lerp(0.35f, 1.15f, talk), 1f);
            }
        }

        public void BindViewport(RawImage targetViewport)
        {
            viewport = targetViewport;
            EnsureAvatarScene();
            if (viewport != null)
            {
                viewport.texture = renderTexture;
            }
        }

        public void SwitchToExternalAvatar(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                Debug.LogWarning("[DigitalHumanAvatarView] SwitchToExternalAvatar: resourcesPath is empty.");
                return;
            }

            resourcesAvatarPath = resourcesPath;
            avatarPrefab = null;

            if (usingExternalAvatar && externalAvatarInstance != null)
            {
                Destroy(externalAvatarInstance);
                externalAvatarInstance = null;
                externalAnimator = null;
                usingExternalAvatar = false;
                modelRoot = null;
            }

            if (sceneRoot != null)
            {
                Destroy(sceneRoot.gameObject);
                sceneRoot = null;
            }

            EnsureAvatarScene();
            if (viewport != null)
            {
                viewport.texture = renderTexture;
            }
        }

        public void PlayInteractiveGreeting()
        {
            ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
            DigitalHumanEventBus.PublishReward("我在这里陪你？", 1.2f);
        }

        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = Mathf.Max(0f, speed);
            if (externalAnimator != null)
            {
                externalAnimator.speed = animationSpeed;
            }
        }

        public void ApplyPose(DigitalHumanAvatarPose pose, DigitalHumanEmotion emotion)
        {
            EnsureAvatarScene();
            if (poseRoutine != null)
            {
                StopCoroutine(poseRoutine);
                blendingPose = false;
            }

            poseRoutine = StartCoroutine(BlendPoseRoutine(pose, emotion));
        }

        private IEnumerator BlendPoseRoutine(DigitalHumanAvatarPose pose, DigitalHumanEmotion emotion)
        {
            currentPose = pose;
            PlayExternalAnimatorCue(pose);

            if (!HasPoseBones())
            {
                yield break;
            }

            blendingPose = true;

            Quaternion leftUpperStart = leftUpperArm.localRotation;
            Quaternion leftLowerStart = leftLowerArm.localRotation;
            Quaternion rightUpperStart = rightUpperArm.localRotation;
            Quaternion rightLowerStart = rightLowerArm.localRotation;
            Quaternion headStart = head.localRotation;
            Quaternion leftLegStart = leftUpperLeg.localRotation;
            Quaternion rightLegStart = rightUpperLeg.localRotation;

            Quaternion leftUpperTarget = Quaternion.Euler(0f, 0f, 12f);
            Quaternion leftLowerTarget = Quaternion.Euler(0f, 0f, 8f);
            Quaternion rightUpperTarget = Quaternion.Euler(0f, 0f, -12f);
            Quaternion rightLowerTarget = Quaternion.Euler(0f, 0f, -8f);
            Quaternion headTarget = Quaternion.identity;
            Quaternion leftLegTarget = Quaternion.identity;
            Quaternion rightLegTarget = Quaternion.identity;

            switch (pose)
            {
                case DigitalHumanAvatarPose.Greeting:
                case DigitalHumanAvatarPose.ImitationWave:
                    rightUpperTarget = Quaternion.Euler(-14f, 0f, -118f);
                    rightLowerTarget = Quaternion.Euler(-2f, 0f, -30f);
                    leftUpperTarget = Quaternion.Euler(0f, 0f, 18f);
                    leftLowerTarget = Quaternion.Euler(0f, 0f, 10f);
                    headTarget = Quaternion.Euler(0f, -7f, 0f);
                    break;
                case DigitalHumanAvatarPose.Speaking:
                    rightUpperTarget = Quaternion.Euler(-18f, 0f, -40f);
                    leftUpperTarget = Quaternion.Euler(-10f, 0f, 28f);
                    headTarget = Quaternion.Euler(0f, 3f, 0f);
                    break;
                case DigitalHumanAvatarPose.OfferItem:
                    rightUpperTarget = Quaternion.Euler(-58f, 0f, -56f);
                    rightLowerTarget = Quaternion.Euler(-12f, 0f, -18f);
                    leftUpperTarget = Quaternion.Euler(-18f, 0f, 28f);
                    break;
                case DigitalHumanAvatarPose.ColorPrompt:
                    leftUpperTarget = Quaternion.Euler(-34f, 0f, 52f);
                    rightUpperTarget = Quaternion.Euler(-34f, 0f, -52f);
                    break;
                case DigitalHumanAvatarPose.ImitationClap:
                    leftUpperTarget = Quaternion.Euler(-58f, 0f, 70f);
                    rightUpperTarget = Quaternion.Euler(-58f, 0f, -70f);
                    leftLowerTarget = Quaternion.Euler(-8f, 0f, 34f);
                    rightLowerTarget = Quaternion.Euler(-8f, 0f, -34f);
                    break;
                case DigitalHumanAvatarPose.ImitationNod:
                    headTarget = Quaternion.Euler(18f, 0f, 0f);
                    break;
                case DigitalHumanAvatarPose.Celebrate:
                    leftUpperTarget = Quaternion.Euler(-12f, 0f, 128f);
                    rightUpperTarget = Quaternion.Euler(-12f, 0f, -128f);
                    leftLegTarget = Quaternion.Euler(0f, 0f, 5f);
                    rightLegTarget = Quaternion.Euler(0f, 0f, -5f);
                    break;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, poseBlendSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * Mathf.Max(0.25f, animationSpeed);
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                leftUpperArm.localRotation = Quaternion.Slerp(leftUpperStart, leftUpperTarget, t);
                leftLowerArm.localRotation = Quaternion.Slerp(leftLowerStart, leftLowerTarget, t);
                rightUpperArm.localRotation = Quaternion.Slerp(rightUpperStart, rightUpperTarget, t);
                rightLowerArm.localRotation = Quaternion.Slerp(rightLowerStart, rightLowerTarget, t);
                leftUpperLeg.localRotation = Quaternion.Slerp(leftLegStart, leftLegTarget, t);
                rightUpperLeg.localRotation = Quaternion.Slerp(rightLegStart, rightLegTarget, t);
                head.localRotation = Quaternion.Slerp(headStart, headTarget, t);
                yield return null;
            }

            leftUpperPoseRotation = leftUpperTarget;
            leftLowerPoseRotation = leftLowerTarget;
            rightUpperPoseRotation = rightUpperTarget;
            rightLowerPoseRotation = rightLowerTarget;
            leftLegPoseRotation = leftLegTarget;
            rightLegPoseRotation = rightLegTarget;
            headPoseRotation = headTarget;

            leftUpperArm.localRotation = leftUpperPoseRotation;
            leftLowerArm.localRotation = leftLowerPoseRotation;
            rightUpperArm.localRotation = rightUpperPoseRotation;
            rightLowerArm.localRotation = rightLowerPoseRotation;
            leftUpperLeg.localRotation = leftLegPoseRotation;
            rightUpperLeg.localRotation = rightLegPoseRotation;
            head.localRotation = headPoseRotation;

            blendingPose = false;
            poseRoutine = null;

            if (emotion == DigitalHumanEmotion.Celebrating)
            {
                DigitalHumanEventBus.PublishReward("完成啦！", 1.4f);
            }
        }

        private void ApplyLoopingPoseMotion()
        {
            if (!HasPoseBones()) return;
            float t = Time.time * animationSpeed;
            leftUpperArm.localRotation = leftUpperPoseRotation;
            leftLowerArm.localRotation = leftLowerPoseRotation;
            rightUpperArm.localRotation = rightUpperPoseRotation;
            rightLowerArm.localRotation = rightLowerPoseRotation;
            leftUpperLeg.localRotation = leftLegPoseRotation;
            rightUpperLeg.localRotation = rightLegPoseRotation;
            head.localRotation = headPoseRotation;

            switch (currentPose)
            {
                case DigitalHumanAvatarPose.Greeting:
                case DigitalHumanAvatarPose.ImitationWave:
                    rightLowerArm.localRotation = rightLowerPoseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 14f);
                    head.localRotation = headPoseRotation * Quaternion.Euler(Mathf.Sin(t * 1.6f) * 2f, 0f, 0f);
                    break;
                case DigitalHumanAvatarPose.Speaking:
                    head.localRotation = headPoseRotation * Quaternion.Euler(Mathf.Sin(t * 2f) * 2.6f, Mathf.Sin(t * 1.4f) * 2f, 0f);
                    rightLowerArm.localRotation = rightLowerPoseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.3f) * 4f);
                    break;
                case DigitalHumanAvatarPose.ImitationClap:
                    leftLowerArm.localRotation = leftLowerPoseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4.2f) * 7f);
                    rightLowerArm.localRotation = rightLowerPoseRotation * Quaternion.Euler(0f, 0f, -Mathf.Sin(t * 4.2f) * 7f);
                    break;
                case DigitalHumanAvatarPose.Celebrate:
                    leftUpperArm.localRotation = leftUpperPoseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4f) * 8f);
                    rightUpperArm.localRotation = rightUpperPoseRotation * Quaternion.Euler(0f, 0f, -Mathf.Sin(t * 4f) * 8f);
                    head.localRotation = headPoseRotation * Quaternion.Euler(Mathf.Sin(t * 2f) * 2f, 0f, 0f);
                    break;
            }
        }

        private bool HasPoseBones()
        {
            return leftUpperArm != null &&
                   leftLowerArm != null &&
                   rightUpperArm != null &&
                   rightLowerArm != null &&
                   leftUpperLeg != null &&
                   rightUpperLeg != null &&
                   head != null;
        }

        public void PlayCustomAnimation(string animationName)
        {
            if (!usingExternalAvatar || externalAnimator == null || string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            if (poseRoutine != null)
            {
                StopCoroutine(poseRoutine);
                blendingPose = false;
                poseRoutine = null;
            }

            try
            {
                externalAnimator.CrossFade(animationName, 0.2f);
            }
            catch
            {
                // State may not exist; that's acceptable
            }
        }
        private void PlayExternalAnimatorCue(DigitalHumanAvatarPose pose)
        {
            if (!usingExternalAvatar || externalAnimator == null || externalAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            string poseName = pose.ToString();
            AnimatorControllerParameter[] parameters = externalAnimator.parameters;
            bool triggered = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type != AnimatorControllerParameterType.Trigger)
                {
                    continue;
                }

                if (string.Equals(parameter.name, poseName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter.name, "Action", System.StringComparison.OrdinalIgnoreCase))
                {
                    externalAnimator.SetTrigger(parameter.name);
                    triggered = true;
                }
            }

            if (!triggered)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    AnimatorControllerParameter parameter = parameters[i];
                    if (parameter.type == AnimatorControllerParameterType.Bool &&
                        string.Equals(parameter.name, poseName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        externalAnimator.SetBool(parameter.name, true);
                        triggered = true;
                        break;
                    }
                }
            }

            if (!triggered)
            {
                try
                {
                    externalAnimator.CrossFade(poseName, 0.15f);
                }
                catch
                {
                }
            }
        }

        private void EnsureAvatarScene()
        {
            if (sceneRoot != null)
            {
                return;
            }

            sceneRoot = new GameObject("DigitalHumanAvatarScene").transform;
            sceneRoot.SetParent(transform, false);
            sceneRoot.localPosition = Vector3.zero;
            CreateCameraAndLight();

            if (TryLoadExternalAvatar())
            {
                if (viewport != null)
                {
                    viewport.texture = renderTexture;
                }

                ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
                return;
            }

            Debug.LogError("[DigitalHumanAvatarView] No external avatar prefab found! " +
                           "Place a Mixamo/FBX prefab at Resources/DigitalHuman/Avatar.prefab " +
                           "or assign one to the avatarPrefab field in the Inspector.");
        }



        private bool TryLoadExternalAvatar()
        {
            GameObject prefab = avatarPrefab;
            if (prefab == null && !string.IsNullOrWhiteSpace(resourcesAvatarPath))
            {
                prefab = Resources.Load<GameObject>(resourcesAvatarPath);
            }

            if (prefab == null)
            {
                return false;
            }

            externalAvatarInstance = Instantiate(prefab, sceneRoot, false);
            externalAvatarInstance.name = "RuntimeDigitalHumanAvatar";
            modelRoot = externalAvatarInstance.transform;
            modelRoot.localPosition = externalAvatarLocalPosition;
            modelRoot.localRotation = Quaternion.Euler(externalAvatarLocalEuler) * Quaternion.Euler(0f, 180f, 0f);
            modelRoot.localScale = Vector3.one * Mathf.Max(0.01f, externalAvatarScale);
            externalAnimator = externalAvatarInstance.GetComponentInChildren<Animator>();
            BindExternalHumanoidBones();


            EnsureExternalAvatarMaterials();

            Bounds modelBounds = CalculateModelBounds();
            if (modelBounds.extents.magnitude > 0.01f)
            {
                FrameCameraToModel(modelBounds);
            }
            else
            {
                Debug.LogWarning("[DigitalHumanAvatarView] Loaded external avatar has near-zero bounds. " +
                                 "Camera will use default position. Make sure the model has visible " +
                                 "mesh renderers with materials assigned.");
            }

            usingExternalAvatar = true;
            return true;
        }

        private void EnsureExternalAvatarMaterials()
        {
            if (modelRoot == null) return;

            // Find a suitable shader (try URP/LW first, fall back to Standard, then default)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                       ?? Shader.Find("Standard")
                       ?? Shader.Find("Legacy Shaders/Diffuse")
                       ?? Shader.Find("Diffuse");
            if (shader == null)
            {
                Debug.LogWarning("[DigitalHumanAvatarView] No shader found for auto materials.");
                return;
            }

            System.Action<Renderer, Color> assignColor = (renderer, color) =>
            {
                Material mat = new Material(shader)
                {
                    name = "AutoMat_" + renderer.gameObject.name,
                    color = color
                };
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.35f);
                renderer.sharedMaterial = mat;
            };

            MeshRenderer[] meshRenderers = modelRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                Material existing = meshRenderers[i].sharedMaterial;
                if (existing != null && existing.mainTexture != null) continue;
                Color color = PickBodyPartColor(meshRenderers[i].transform);
                assignColor(meshRenderers[i], color);
            }

            SkinnedMeshRenderer[] skinnedRenderers = modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                Material existing = skinnedRenderers[i].sharedMaterial;
                if (existing != null && existing.mainTexture != null) continue;
                Color color = PickBodyPartColor(skinnedRenderers[i].transform);
                assignColor(skinnedRenderers[i], color);
            }
        }

        private static Color PickBodyPartColor(Transform meshTransform)
        {
            Transform walker = meshTransform;
            for (int depth = 0; walker != null && depth < 10; depth++)
            {
                string name = walker.name.ToLowerInvariant();
                if (name.Contains("head") || name.Contains("neck"))
                    return new Color32(255, 222, 194, 255);
                if (name.Contains("hand"))
                    return new Color32(255, 222, 194, 255);
                if (name.Contains("upperarm") || name.Contains("lowerarm"))
                    return new Color32(255, 222, 194, 255);
                if (name.Contains("arm"))
                    return new Color32(38, 170, 211, 255);
                if (name.Contains("thigh") || name.Contains("upleg") || name.Contains("lowerleg"))
                    return new Color32(82, 108, 152, 255);
                if (name.Contains("leg"))
                    return new Color32(82, 108, 152, 255);
                if (name.Contains("foot"))
                    return new Color32(42, 45, 54, 255);
                if (name.Contains("spine") || name.Contains("chest") || name.Contains("hips"))
                    return new Color32(38, 170, 211, 255);
                if (name.Contains("mouth") || name.Contains("jaw"))
                    return new Color32(200, 100, 100, 255);
                if (name.Contains("eye"))
                    return Color.white;
                walker = walker.parent;
            }
            return new Color32(255, 222, 194, 255);
        }

        private Bounds CalculateModelBounds()
        {
            if (modelRoot == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new Bounds();
            bool hasBounds = false;
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds)
            {
                return new Bounds(modelRoot.position, Vector3.one * 0.5f);
            }

            return bounds;
        }

        private void FrameCameraToModel(Bounds modelBounds)
        {
            if (avatarCamera == null || sceneRoot == null)
            {
                return;
            }

            Vector3 center = modelBounds.center;
            float size = Mathf.Max(modelBounds.extents.magnitude, 0.3f);
            float fovRad = avatarCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = size / Mathf.Tan(fovRad) * 1.4f;
            distance = Mathf.Clamp(distance, 0.5f, 8f);

            Vector3 cameraPosition = new Vector3(0f, center.y * 0.5f + 0.15f, -distance);
            avatarCamera.transform.localPosition = cameraPosition;
            avatarCamera.transform.localRotation = Quaternion.LookRotation(
                center - avatarCamera.transform.position,
                Vector3.up
            );

            Transform keyLight = sceneRoot.Find("AvatarKeyLight");
            if (keyLight != null)
            {
                keyLight.localPosition = new Vector3(-size * 0.6f, size * 1.2f + 0.5f, -distance * 0.5f);
            }

            Transform fillLight = sceneRoot.Find("AvatarFillLight");
            if (fillLight != null)
            {
                fillLight.localPosition = new Vector3(size * 0.6f, size * 0.6f, -distance * 0.3f);
            }
        }

        private void BindExternalHumanoidBones()
        {
            if (externalAnimator == null || !externalAnimator.isHuman)
            {
                hips = FindDeepChild(modelRoot, "hips", "pelvis", "root");
                spine = FindDeepChild(modelRoot, "spine");
                chest = FindDeepChild(modelRoot, "chest", "upperchest", "spine2");
                neck = FindDeepChild(modelRoot, "neck");
                head = FindDeepChild(modelRoot, "head");
                leftUpperArm = FindDeepChild(modelRoot, "leftupperarm", "left_arm", "l_upperarm");
                leftLowerArm = FindDeepChild(modelRoot, "leftlowerarm", "leftforearm", "l_forearm");
                rightUpperArm = FindDeepChild(modelRoot, "rightupperarm", "right_arm", "r_upperarm");
                rightLowerArm = FindDeepChild(modelRoot, "rightlowerarm", "rightforearm", "r_forearm");
                leftUpperLeg = FindDeepChild(modelRoot, "leftupperleg", "leftupleg", "l_thigh");
                rightUpperLeg = FindDeepChild(modelRoot, "rightupperleg", "rightupleg", "r_thigh");
                mouth = FindDeepChild(modelRoot, "mouth", "jaw", "openjaw");
                return;
            }

            hips = externalAnimator.GetBoneTransform(HumanBodyBones.Hips);
            spine = externalAnimator.GetBoneTransform(HumanBodyBones.Spine);
            chest = externalAnimator.GetBoneTransform(HumanBodyBones.Chest);
            neck = externalAnimator.GetBoneTransform(HumanBodyBones.Neck);
            head = externalAnimator.GetBoneTransform(HumanBodyBones.Head);
            leftUpperArm = externalAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArm = externalAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = externalAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpperArm = externalAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArm = externalAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = externalAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            leftUpperLeg = externalAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            leftLowerLeg = externalAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            leftFoot = externalAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightUpperLeg = externalAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rightLowerLeg = externalAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            rightFoot = externalAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            mouth = externalAnimator.GetBoneTransform(HumanBodyBones.Jaw);
            if (mouth == null)
            {
                if (head != null)
                {
                    mouth = FindDeepChild(head, "mouth", "jaw", "openjaw");
                }
            }
        }

        private static Transform FindDeepChild(Transform root, params string[] keys)
        {
            if (root == null || keys == null)
            {
                return null;
            }

            string normalizedName = NormalizeBoneName(root.name);
            for (int i = 0; i < keys.Length; i++)
            {
                if (normalizedName.Contains(NormalizeBoneName(keys[i])))
                {
                    return root;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), keys);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string NormalizeBoneName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("_", string.Empty).Replace(" ", string.Empty).Replace(":", string.Empty).ToLowerInvariant();
        }
        private void CreateCameraAndLight()
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "DigitalHumanAvatarTexture"
            };
            renderTexture.Create();

            GameObject cameraObject = new GameObject("AvatarCamera");
            cameraObject.transform.SetParent(sceneRoot, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.93f, -3.05f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -0.04f, 1f), Vector3.up);
            avatarCamera = cameraObject.AddComponent<Camera>();
            avatarCamera.targetTexture = renderTexture;
            avatarCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarCamera.backgroundColor = new Color32(230, 240, 252, 255);
            avatarCamera.fieldOfView = 28f;
            avatarCamera.nearClipPlane = 0.05f;
            avatarCamera.farClipPlane = 20f;

            GameObject keyLight = new GameObject("AvatarKeyLight");
            keyLight.transform.SetParent(sceneRoot, false);
            keyLight.transform.localPosition = new Vector3(-1.2f, 2.7f, -1.8f);
            keyLight.transform.localRotation = Quaternion.Euler(44f, 28f, 0f);

            GameObject fillLight = new GameObject("AvatarFillLight");
            fillLight.transform.SetParent(sceneRoot, false);
            fillLight.transform.localPosition = new Vector3(1.35f, 1.45f, -1.2f);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 4f;
            fill.intensity = 0.7f;
        }

    }
}




