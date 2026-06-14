using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    /// <summary>
    /// 数字人形象视图 - 加载外部3D模型，通过RenderTexture渲染到UI RawImage
    /// 支持姿态动画（挥手、说话、庆祝等）
    /// </summary>
    public class DigitalHumanAvatarView : MonoBehaviour
    {
        [Header("渲染")]
        [SerializeField] private RawImage viewport;
        [SerializeField] private int textureSize = 768;

        [Header("外部模型")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private string resourcesAvatarPath = "DigitalHuman/Avatar";
        [SerializeField] private Vector3 externalAvatarLocalPosition = new Vector3(0f, -0.55f, 0.3f);
        [SerializeField] private Vector3 externalAvatarLocalEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private float externalAvatarScale = 0.9f;

        [Header("姿态动画")]
        [SerializeField] private float poseBlendSeconds = 0.28f;

        private RenderTexture renderTexture;
        private Camera avatarCamera;
        private Transform sceneRoot;
        private Transform modelRoot;
        private SkinnedMeshRenderer skinnedMeshRenderer;

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

        private void Awake()
        {
            EnsureAvatarScene();
        }

        private void OnEnable()
        {
            DigitalHumanEventBus.AvatarPoseRequested += ApplyPose;
        }

        private void OnDisable()
        {
            DigitalHumanEventBus.AvatarPoseRequested -= ApplyPose;
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
                return;

            // Animator驱动：让Animator控制动画，只在需要时叠加姿态
            if (externalAnimator != null)
            {
                externalAnimator.speed = animationSpeed;
                externalAnimator.enabled = true;

                // 说话时嘴巴张合（叠加在Animator之上）
                if (mouth != null && currentPose == DigitalHumanAvatarPose.Speaking)
                {
                    float talk = Mathf.Abs(Mathf.Sin(Time.time * 7.5f * animationSpeed));
                    mouth.localScale = new Vector3(1f, Mathf.Lerp(0.35f, 1.15f, talk), 1f);
                }
                return;
            }

            // 无Animator时使用呼吸+姿态系统
            float breath = Mathf.Sin(Time.time * 1.15f * animationSpeed);
            if (chest != null)
                chest.localRotation = Quaternion.Euler(breath * 1.4f, 0f, 0f);
            if (spine != null)
                spine.localRotation = Quaternion.Euler(-breath * 0.75f, 0f, 0f);

            if (!blendingPose && HasPoseBones())
                ApplyLoopingPoseMotion();

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
                viewport.texture = renderTexture;
        }

        public void PlayInteractiveGreeting()
        {
            ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
            DigitalHumanEventBus.PublishReward("我在这里陪你。", 1.2f);
        }

        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = Mathf.Max(0f, speed);
            if (externalAnimator != null)
                externalAnimator.speed = animationSpeed;
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
                yield break;

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
                DigitalHumanEventBus.PublishReward("完成啦！", 1.4f);
        }

        private void ApplyLoopingPoseMotion()
        {
            if (leftHand != null && (currentPose == DigitalHumanAvatarPose.Greeting || currentPose == DigitalHumanAvatarPose.ImitationWave))
            {
                float wave = Mathf.Sin(Time.time * 5.5f) * 0.2f;
                rightHand.localRotation = Quaternion.Euler(wave * 15f, 0f, 0f);
            }

            if (head != null && currentPose == DigitalHumanAvatarPose.ImitationNod)
            {
                float nod = Mathf.Sin(Time.time * 3.5f) * 0.5f;
                head.localRotation = Quaternion.Euler(headPoseRotation.eulerAngles.x + nod * 10f, headPoseRotation.eulerAngles.y, headPoseRotation.eulerAngles.z);
            }
        }

        private bool HasPoseBones()
        {
            return leftUpperArm != null && rightUpperArm != null && head != null;
        }

        private void PlayExternalAnimatorCue(DigitalHumanAvatarPose pose)
        {
            if (externalAnimator == null)
                return;

            string trigger = pose switch
            {
                DigitalHumanAvatarPose.Greeting => "Greet",
                DigitalHumanAvatarPose.Celebrate => "Celebrate",
                DigitalHumanAvatarPose.ImitationWave => "Wave",
                DigitalHumanAvatarPose.ImitationClap => "Clap",
                _ => null
            };

            if (!string.IsNullOrEmpty(trigger))
            {
                try { externalAnimator.SetTrigger(trigger); }
                catch { }
            }
        }

        private void EnsureAvatarScene()
        {
            if (sceneRoot != null)
                return;

            sceneRoot = new GameObject("DigitalHumanAvatarScene").transform;
            sceneRoot.SetParent(transform, false);
            sceneRoot.localPosition = Vector3.zero;
            CreateCameraAndLight();

            if (TryLoadExternalAvatar())
            {
                if (viewport != null)
                    viewport.texture = renderTexture;
                ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
            }
            else
            {
                Debug.LogError("[DigitalHumanAvatarView] 无法加载外部模型！请确保 Resources/DigitalHuman/Avatar.prefab 存在");
            }
        }

        private bool TryLoadExternalAvatar()
        {
            GameObject prefab = avatarPrefab;
            if (prefab == null && !string.IsNullOrWhiteSpace(resourcesAvatarPath))
                prefab = Resources.Load<GameObject>(resourcesAvatarPath);

            if (prefab == null)
                return false;

            externalAvatarInstance = Instantiate(prefab, sceneRoot, false);
            externalAvatarInstance.name = "RuntimeDigitalHumanAvatar";
            modelRoot = externalAvatarInstance.transform;
            modelRoot.localPosition = externalAvatarLocalPosition;
            modelRoot.localRotation = Quaternion.Euler(externalAvatarLocalEuler);
            modelRoot.localScale = Vector3.one * Mathf.Max(0.01f, externalAvatarScale);

            externalAnimator = externalAvatarInstance.GetComponentInChildren<Animator>();
            skinnedMeshRenderer = externalAvatarInstance.GetComponentInChildren<SkinnedMeshRenderer>();

            BindExternalHumanoidBones();
            usingExternalAvatar = true;
            return true;
        }

        private void BindExternalHumanoidBones()
        {
            if (externalAnimator != null && externalAnimator.isHuman)
            {
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
                mouth = FindDeepChild(head, "mouth", "jaw", "lip");
                return;
            }

            // 非Humanoid回退：按命名查找
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
        }

        private static Transform FindDeepChild(Transform root, params string[] keys)
        {
            if (root == null)
                return null;

            foreach (string key in keys)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (NormalizeBoneName(child.name) == key)
                        return child;

                    Transform found = FindDeepChild(child, key);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private static string NormalizeBoneName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        }

        private void CreateCameraAndLight()
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "DigitalHumanAvatarTexture"
            };
            renderTexture.Create();

            // 相机位置：对准模型，让角色完整显示在viewport中
            GameObject cameraObject = new GameObject("AvatarCamera");
            cameraObject.transform.SetParent(sceneRoot, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.75f, -2.4f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            avatarCamera = cameraObject.AddComponent<Camera>();
            avatarCamera.targetTexture = renderTexture;
            avatarCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarCamera.backgroundColor = new Color32(230, 240, 252, 255);
            avatarCamera.fieldOfView = 30f;
            avatarCamera.nearClipPlane = 0.05f;
            avatarCamera.farClipPlane = 20f;

            // 主方向光
            GameObject keyLight = new GameObject("AvatarKeyLight");
            keyLight.transform.SetParent(sceneRoot, false);
            keyLight.transform.localPosition = new Vector3(-1.2f, 2.7f, -1.8f);
            keyLight.transform.localRotation = Quaternion.Euler(44f, 28f, 0f);
            Light light = keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.18f;

            // 补光
            GameObject fillLight = new GameObject("AvatarFillLight");
            fillLight.transform.SetParent(sceneRoot, false);
            fillLight.transform.localPosition = new Vector3(1.35f, 1.45f, -1.2f);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 4f;
            fill.intensity = 0.7f;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            Material material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.42f);

            return material;
        }
    }
}