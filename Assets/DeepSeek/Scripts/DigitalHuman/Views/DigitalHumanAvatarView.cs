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
            {
                return;
            }

            if (externalAnimator != null)
            {
                externalAnimator.speed = animationSpeed;
            }

            float breath = Mathf.Sin(Time.time * 1.15f * animationSpeed);
            if (chest != null)
            {
                chest.localRotation = Quaternion.Euler(breath * 1.4f, 0f, 0f);
            }

            if (spine != null)
            {
                spine.localRotation = Quaternion.Euler(-breath * 0.75f, 0f, 0f);
            }

            if (!blendingPose && HasPoseBones())
            {
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

        public void PlayInteractiveGreeting()
        {
            ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
            DigitalHumanEventBus.PublishReward("我在这里陪你。", 1.2f);
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

        private void PlayExternalAnimatorCue(DigitalHumanAvatarPose pose)
        {
            if (!usingExternalAvatar || externalAnimator == null || externalAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            string poseName = pose.ToString();
            AnimatorControllerParameter[] parameters = externalAnimator.parameters;
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

            GameObject modelObject = new GameObject("SkinnedDigitalHuman");
            modelObject.transform.SetParent(sceneRoot, false);
            modelRoot = modelObject.transform;
            modelRoot.localPosition = new Vector3(0f, -0.08f, 0f);
            modelRoot.localRotation = Quaternion.identity;
            modelRoot.localScale = Vector3.one;

            CreateMaterials();
            CreateSkeleton();
            CreateSkinnedMesh();
            CreateFaceAndHairDetails();

            if (viewport != null)
            {
                viewport.texture = renderTexture;
            }

            ApplyPose(DigitalHumanAvatarPose.Greeting, DigitalHumanEmotion.Friendly);
        }

        private void CreateMaterials()
        {
            skinMaterial = CreateMaterial("DigitalHuman_Skin", new Color32(255, 222, 194, 255));
            shirtMaterial = CreateMaterial("DigitalHuman_Shirt", new Color32(38, 170, 211, 255));
            pantsMaterial = CreateMaterial("DigitalHuman_Pants", new Color32(52, 76, 116, 255));
            hairMaterial = CreateMaterial("DigitalHuman_Hair", new Color32(255, 190, 63, 255));
            shoeMaterial = CreateMaterial("DigitalHuman_Shoes", new Color32(42, 45, 54, 255));
        }

        private void CreateSkeleton()
        {
            bones.Clear();
            hips = CreateBone("hips", modelRoot, new Vector3(0f, 0.78f, 0f));
            spine = CreateBone("spine", hips, new Vector3(0f, 0.26f, 0f));
            chest = CreateBone("chest", spine, new Vector3(0f, 0.30f, 0f));
            neck = CreateBone("neck", chest, new Vector3(0f, 0.21f, 0f));
            head = CreateBone("head", neck, new Vector3(0f, 0.18f, 0f));

            leftUpperArm = CreateBone("left_upper_arm", chest, new Vector3(-0.38f, 0.02f, 0f));
            leftLowerArm = CreateBone("left_lower_arm", leftUpperArm, new Vector3(-0.31f, -0.30f, 0f));
            leftHand = CreateBone("left_hand", leftLowerArm, new Vector3(-0.04f, -0.31f, -0.01f));

            rightUpperArm = CreateBone("right_upper_arm", chest, new Vector3(0.38f, 0.02f, 0f));
            rightLowerArm = CreateBone("right_lower_arm", rightUpperArm, new Vector3(0.31f, -0.30f, 0f));
            rightHand = CreateBone("right_hand", rightLowerArm, new Vector3(0.04f, -0.31f, -0.01f));

            leftUpperLeg = CreateBone("left_upper_leg", hips, new Vector3(-0.14f, -0.08f, 0f));
            leftLowerLeg = CreateBone("left_lower_leg", leftUpperLeg, new Vector3(0f, -0.44f, 0f));
            leftFoot = CreateBone("left_foot", leftLowerLeg, new Vector3(0f, -0.39f, -0.08f));

            rightUpperLeg = CreateBone("right_upper_leg", hips, new Vector3(0.14f, -0.08f, 0f));
            rightLowerLeg = CreateBone("right_lower_leg", rightUpperLeg, new Vector3(0f, -0.44f, 0f));
            rightFoot = CreateBone("right_foot", rightLowerLeg, new Vector3(0f, -0.39f, -0.08f));
        }

        private Transform CreateBone(string boneName, Transform parent, Vector3 localPosition)
        {
            Transform bone = new GameObject(boneName).transform;
            bone.SetParent(parent, false);
            bone.localPosition = localPosition;
            bone.localRotation = Quaternion.identity;
            bone.localScale = Vector3.one;
            bones.Add(bone);
            return bone;
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
            modelRoot.localRotation = Quaternion.Euler(externalAvatarLocalEuler);
            modelRoot.localScale = Vector3.one * Mathf.Max(0.01f, externalAvatarScale);
            externalAnimator = externalAvatarInstance.GetComponentInChildren<Animator>();
            BindExternalHumanoidBones();
            usingExternalAvatar = true;
            return true;
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

        private void CreateSkinnedMesh()
        {
            vertices.Clear();
            normals.Clear();
            uvs.Clear();
            boneWeights.Clear();
            for (int i = 0; i < submeshTriangles.Length; i++)
            {
                submeshTriangles[i].Clear();
            }

            AddBodySurface();
            AddTaperedTube(WorldOf(neck), WorldOf(head), 0.105f, 0.08f, 0.13f, 0.10f, IndexOf(neck), IndexOf(head), AvatarMaterial.Skin, 14, 1);
            AddSphere(WorldOf(head) + new Vector3(0f, 0.15f, -0.015f), new Vector3(0.25f, 0.30f, 0.235f), IndexOf(head), AvatarMaterial.Skin, 18, 10, 0f, 1f);
            AddSphere(WorldOf(head) + new Vector3(0f, 0.24f, 0.02f), new Vector3(0.275f, 0.205f, 0.255f), IndexOf(head), AvatarMaterial.Hair, 18, 8, 0f, 0.46f);

            AddArm(leftUpperArm, leftLowerArm, leftHand, true);
            AddArm(rightUpperArm, rightLowerArm, rightHand, false);
            AddLeg(leftUpperLeg, leftLowerLeg, leftFoot);
            AddLeg(rightUpperLeg, rightLowerLeg, rightFoot);

            Mesh mesh = new Mesh
            {
                name = "ProceduralContinuousSkinnedDigitalHuman"
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.boneWeights = boneWeights.ToArray();
            mesh.bindposes = CreateBindPoses();
            mesh.subMeshCount = submeshTriangles.Length;
            for (int i = 0; i < submeshTriangles.Length; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i);
            }

            mesh.RecalculateBounds();

            skinnedMeshRenderer = modelRoot.gameObject.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = mesh;
            skinnedMeshRenderer.bones = bones.ToArray();
            skinnedMeshRenderer.rootBone = hips;
            skinnedMeshRenderer.updateWhenOffscreen = true;
            skinnedMeshRenderer.sharedMaterials = new[]
            {
                skinMaterial,
                shirtMaterial,
                pantsMaterial,
                hairMaterial,
                shoeMaterial
            };
            skinnedMeshRenderer.localBounds = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 2.6f, 1.4f));
        }

        private void AddBodySurface()
        {
            Vector3[] centers =
            {
                WorldOf(hips) + new Vector3(0f, -0.08f, 0f),
                WorldOf(hips) + new Vector3(0f, 0.05f, 0f),
                WorldOf(spine) + new Vector3(0f, 0.02f, 0f),
                WorldOf(chest) + new Vector3(0f, 0.02f, 0f),
                WorldOf(neck) + new Vector3(0f, -0.06f, 0f)
            };
            float[] radiusX = { 0.20f, 0.25f, 0.29f, 0.32f, 0.14f };
            float[] radiusZ = { 0.12f, 0.145f, 0.16f, 0.155f, 0.09f };
            int[] ringBones = { IndexOf(hips), IndexOf(hips), IndexOf(spine), IndexOf(chest), IndexOf(neck) };
            AddRingSurface(centers, radiusX, radiusZ, ringBones, AvatarMaterial.Shirt, 18);
        }

        private void AddArm(Transform upper, Transform lower, Transform hand, bool left)
        {
            Vector3 shoulder = WorldOf(upper);
            Vector3 elbow = WorldOf(lower);
            Vector3 wrist = WorldOf(hand);
            Vector3 midUpper = Vector3.Lerp(shoulder, elbow, 0.45f);
            Vector3 midLower = Vector3.Lerp(elbow, wrist, 0.52f);
            AddRingSurface(
                new[] { shoulder, midUpper, elbow, midLower, wrist },
                new[] { 0.082f, 0.078f, 0.068f, 0.061f, 0.052f },
                new[] { 0.074f, 0.070f, 0.062f, 0.055f, 0.048f },
                new[] { IndexOf(upper), IndexOf(upper), IndexOf(lower), IndexOf(lower), IndexOf(hand) },
                AvatarMaterial.Skin,
                14);

            Vector3 sleeveEnd = Vector3.Lerp(shoulder, elbow, 0.58f);
            AddTaperedTube(shoulder, sleeveEnd, 0.095f, 0.085f, 0.087f, 0.078f, IndexOf(upper), IndexOf(upper), AvatarMaterial.Shirt, 14, 1);
            AddSphere(wrist + new Vector3(left ? -0.02f : 0.02f, -0.045f, -0.01f), new Vector3(0.074f, 0.088f, 0.062f), IndexOf(hand), AvatarMaterial.Skin, 12, 7, 0f, 1f);
        }

        private void AddLeg(Transform upper, Transform lower, Transform foot)
        {
            Vector3 hip = WorldOf(upper);
            Vector3 knee = WorldOf(lower);
            Vector3 ankle = WorldOf(foot);
            AddRingSurface(
                new[] { hip, Vector3.Lerp(hip, knee, 0.48f), knee, Vector3.Lerp(knee, ankle, 0.55f), ankle },
                new[] { 0.105f, 0.100f, 0.085f, 0.078f, 0.070f },
                new[] { 0.090f, 0.086f, 0.078f, 0.070f, 0.064f },
                new[] { IndexOf(upper), IndexOf(upper), IndexOf(lower), IndexOf(lower), IndexOf(foot) },
                AvatarMaterial.Pants,
                14);

            AddTaperedTube(ankle + new Vector3(0f, 0.035f, 0.08f), ankle + new Vector3(0f, 0.035f, -0.18f), 0.078f, 0.052f, 0.12f, 0.058f, IndexOf(foot), IndexOf(foot), AvatarMaterial.Shoes, 14, 1);
        }

        private void AddRingSurface(
            Vector3[] centers,
            float[] radiusX,
            float[] radiusZ,
            int[] ringBones,
            AvatarMaterial material,
            int sides)
        {
            int first = vertices.Count;
            for (int ring = 0; ring < centers.Length; ring++)
            {
                Vector3 axis;
                if (ring == 0)
                {
                    axis = centers[1] - centers[0];
                }
                else if (ring == centers.Length - 1)
                {
                    axis = centers[ring] - centers[ring - 1];
                }
                else
                {
                    axis = centers[ring + 1] - centers[ring - 1];
                }

                axis.Normalize();
                Vector3 tangent = Vector3.Cross(axis, Vector3.up);
                if (tangent.sqrMagnitude < 0.001f)
                {
                    tangent = Vector3.right;
                }

                tangent.Normalize();
                Vector3 bitangent = Vector3.Cross(tangent, axis).normalized;

                for (int s = 0; s < sides; s++)
                {
                    float angle = Mathf.PI * 2f * s / sides;
                    Vector3 radial = tangent * Mathf.Cos(angle) * radiusX[ring] + bitangent * Mathf.Sin(angle) * radiusZ[ring];
                    int bone0 = ringBones[ring];
                    int bone1 = ring < ringBones.Length - 1 ? ringBones[ring + 1] : bone0;
                    AddVertex(centers[ring] + radial, radial.normalized, new Vector2(s / (float)sides, ring / (float)(centers.Length - 1)), bone0, bone1, 0.92f, 0.08f);
                }
            }

            for (int ring = 0; ring < centers.Length - 1; ring++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = first + ring * sides + s;
                    int b = first + ring * sides + (s + 1) % sides;
                    int c = first + (ring + 1) * sides + s;
                    int d = first + (ring + 1) * sides + (s + 1) % sides;
                    AddTriangle(material, a, c, b);
                    AddTriangle(material, b, c, d);
                }
            }
        }

        private void AddTaperedTube(
            Vector3 start,
            Vector3 end,
            float radiusXStart,
            float radiusZStart,
            float radiusXEnd,
            float radiusZEnd,
            int startBone,
            int endBone,
            AvatarMaterial material,
            int sides,
            int segments)
        {
            Vector3 axis = (end - start).normalized;
            Vector3 tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = Vector3.Cross(axis, Vector3.right);
            }

            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(tangent, axis).normalized;
            int first = vertices.Count;

            for (int y = 0; y <= segments; y++)
            {
                float t = y / (float)segments;
                float radiusX = Mathf.Lerp(radiusXStart, radiusXEnd, t);
                float radiusZ = Mathf.Lerp(radiusZStart, radiusZEnd, t);
                Vector3 center = Vector3.Lerp(start, end, t);

                for (int s = 0; s < sides; s++)
                {
                    float angle = Mathf.PI * 2f * s / sides;
                    Vector3 radial = tangent * Mathf.Cos(angle) * radiusX + bitangent * Mathf.Sin(angle) * radiusZ;
                    AddVertex(center + radial, radial.normalized, new Vector2(s / (float)sides, t), startBone, endBone, 1f - t, t);
                }
            }

            for (int y = 0; y < segments; y++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = first + y * sides + s;
                    int b = first + y * sides + (s + 1) % sides;
                    int c = first + (y + 1) * sides + s;
                    int d = first + (y + 1) * sides + (s + 1) % sides;
                    AddTriangle(material, a, c, b);
                    AddTriangle(material, b, c, d);
                }
            }
        }

        private void AddSphere(
            Vector3 center,
            Vector3 radius,
            int bone,
            AvatarMaterial material,
            int longitude,
            int latitude,
            float minVertical01,
            float maxVertical01)
        {
            int first = vertices.Count;
            int startLat = Mathf.Max(0, Mathf.FloorToInt(latitude * minVertical01));
            int endLat = Mathf.Min(latitude, Mathf.CeilToInt(latitude * maxVertical01));

            for (int lat = startLat; lat <= endLat; lat++)
            {
                float v = lat / (float)latitude;
                float theta = Mathf.PI * v;
                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);

                for (int lon = 0; lon <= longitude; lon++)
                {
                    float u = lon / (float)longitude;
                    float phi = Mathf.PI * 2f * u;
                    Vector3 normal = new Vector3(Mathf.Cos(phi) * sin, cos, Mathf.Sin(phi) * sin);
                    Vector3 point = center + Vector3.Scale(normal, radius);
                    AddVertex(point, normal.normalized, new Vector2(u, v), bone, bone, 1f, 0f);
                }
            }

            int rows = endLat - startLat;
            int stride = longitude + 1;
            for (int row = 0; row < rows; row++)
            {
                for (int lon = 0; lon < longitude; lon++)
                {
                    int a = first + row * stride + lon;
                    int b = first + row * stride + lon + 1;
                    int c = first + (row + 1) * stride + lon;
                    int d = first + (row + 1) * stride + lon + 1;
                    AddTriangle(material, a, c, b);
                    AddTriangle(material, b, c, d);
                }
            }
        }

        private void AddVertex(Vector3 position, Vector3 normal, Vector2 uv, int bone0, int bone1, float weight0, float weight1)
        {
            vertices.Add(position);
            normals.Add(normal);
            uvs.Add(uv);
            boneWeights.Add(new BoneWeight
            {
                boneIndex0 = bone0,
                boneIndex1 = bone1,
                weight0 = weight0,
                weight1 = weight1
            });
        }

        private void AddTriangle(AvatarMaterial material, int a, int b, int c)
        {
            List<int> triangles = submeshTriangles[(int)material];
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private Matrix4x4[] CreateBindPoses()
        {
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                bindPoses[i] = bones[i].worldToLocalMatrix * modelRoot.localToWorldMatrix;
            }

            return bindPoses;
        }

        private void CreateFaceAndHairDetails()
        {
            CreateFaceQuad("left_eye_white", head, new Vector3(-0.087f, 0.16f, -0.247f), new Vector2(0.082f, 0.044f), Color.white);
            CreateFaceQuad("right_eye_white", head, new Vector3(0.087f, 0.16f, -0.247f), new Vector2(0.082f, 0.044f), Color.white);
            CreateFaceQuad("left_iris", head, new Vector3(-0.087f, 0.158f, -0.252f), new Vector2(0.036f, 0.040f), new Color32(65, 201, 169, 255));
            CreateFaceQuad("right_iris", head, new Vector3(0.087f, 0.158f, -0.252f), new Vector2(0.036f, 0.040f), new Color32(65, 201, 169, 255));
            CreateFaceQuad("left_pupil", head, new Vector3(-0.087f, 0.158f, -0.257f), new Vector2(0.015f, 0.026f), Color.black);
            CreateFaceQuad("right_pupil", head, new Vector3(0.087f, 0.158f, -0.257f), new Vector2(0.015f, 0.026f), Color.black);
            CreateFaceQuad("left_brow", head, new Vector3(-0.087f, 0.215f, -0.255f), new Vector2(0.072f, 0.012f), new Color32(91, 63, 30, 255));
            CreateFaceQuad("right_brow", head, new Vector3(0.087f, 0.215f, -0.255f), new Vector2(0.072f, 0.012f), new Color32(91, 63, 30, 255));
            mouth = CreateFaceQuad("mouth", head, new Vector3(0f, 0.057f, -0.258f), new Vector2(0.122f, 0.026f), new Color32(196, 75, 88, 255));

            Color hair = new Color32(255, 194, 72, 255);
            CreateHairRibbon("back_hair_center", head, new Vector3(0f, 0.30f, 0.16f), new Vector3(0f, -0.05f, 0.20f), new Vector3(0f, -0.56f, 0.13f), 0.20f, 0.24f, 0.16f, hair);
            CreateHairRibbon("back_hair_left", head, new Vector3(-0.12f, 0.25f, 0.11f), new Vector3(-0.20f, -0.08f, 0.15f), new Vector3(-0.22f, -0.48f, 0.05f), 0.13f, 0.16f, 0.10f, hair);
            CreateHairRibbon("back_hair_right", head, new Vector3(0.12f, 0.25f, 0.11f), new Vector3(0.20f, -0.08f, 0.15f), new Vector3(0.22f, -0.48f, 0.05f), 0.13f, 0.16f, 0.10f, hair);
            CreateHairRibbon("front_bang_left", head, new Vector3(-0.08f, 0.31f, -0.18f), new Vector3(-0.11f, 0.16f, -0.25f), new Vector3(-0.06f, 0.03f, -0.22f), 0.08f, 0.07f, 0.02f, hair);
            CreateHairRibbon("front_bang_right", head, new Vector3(0.08f, 0.31f, -0.18f), new Vector3(0.11f, 0.16f, -0.25f), new Vector3(0.06f, 0.03f, -0.22f), 0.08f, 0.07f, 0.02f, hair);
        }

        private Transform CreateFaceQuad(string name, Transform parent, Vector3 localPosition, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh { name = $"{name}_mesh" };
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f)
            };
            mesh.uv = new[]
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial($"{name}_mat", color);
            return go.transform;
        }

        private void CreateHairRibbon(
            string name,
            Transform parent,
            Vector3 top,
            Vector3 middle,
            Vector3 bottom,
            float topWidth,
            float middleWidth,
            float bottomWidth,
            Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh { name = $"{name}_mesh" };
            mesh.vertices = new[]
            {
                top + Vector3.left * topWidth * 0.5f,
                top + Vector3.right * topWidth * 0.5f,
                middle + Vector3.left * middleWidth * 0.5f,
                middle + Vector3.right * middleWidth * 0.5f,
                bottom + Vector3.left * bottomWidth * 0.5f,
                bottom + Vector3.right * bottomWidth * 0.5f
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                Vector2.right
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
            mesh.RecalculateNormals();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial($"{name}_mat", color);
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
            Light light = keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.18f;

            GameObject fillLight = new GameObject("AvatarFillLight");
            fillLight.transform.SetParent(sceneRoot, false);
            fillLight.transform.localPosition = new Vector3(1.35f, 1.45f, -1.2f);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 4f;
            fill.intensity = 0.7f;
        }

        private int IndexOf(Transform bone)
        {
            return bones.IndexOf(bone);
        }

        private Vector3 WorldOf(Transform bone)
        {
            return modelRoot.InverseTransformPoint(bone.position);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.42f);
            }

            return material;
        }
    }
}
