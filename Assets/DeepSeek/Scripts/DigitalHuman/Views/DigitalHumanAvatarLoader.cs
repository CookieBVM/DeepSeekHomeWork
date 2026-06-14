using System.Collections.Generic;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    /// <summary>
    /// 负责加载外部 Avatar 预制体、搭建摄像机/灯光、绑定 Humanoid 骨骼、自动着色。
    /// 将搭建逻辑从 DigitalHumanAvatarView 中分离，保持 View 专注于动画控制。
    /// </summary>
    internal class DigitalHumanAvatarLoader

    {

        public Transform SceneRoot { get; private set; }
        public Transform ModelRoot { get; private set; }
        public Animator ExternalAnimator { get; private set; }
        public GameObject ExternalAvatarInstance { get; private set; }
        public bool UsingExternalAvatar { get; private set; }
        public RenderTexture RenderTexture { get; private set; }
        public Camera AvatarCamera { get; private set; }

        // ── Humanoid 骨骼 ──
        public Transform Hips, Spine, Chest, Neck, Head;
        public Transform LeftUpperArm, LeftLowerArm, LeftHand;
        public Transform RightUpperArm, RightLowerArm, RightHand;
        public Transform LeftUpperLeg, LeftLowerLeg, LeftFoot;
        public Transform RightUpperLeg, RightLowerLeg, RightFoot;
        public Transform Mouth;

        private readonly int textureSize;
        private readonly GameObject avatarPrefab;
        private string resourcesAvatarPath;
        private readonly Vector3 externalAvatarLocalPosition;
        private readonly Vector3 externalAvatarLocalEuler;
        private readonly float externalAvatarScale;

        public DigitalHumanAvatarLoader(
            int textureSize,
            GameObject avatarPrefab,
            string resourcesAvatarPath,
            Vector3 position,
            Vector3 euler,
            float scale)
        {
            this.textureSize = textureSize;
            this.avatarPrefab = avatarPrefab;
            this.resourcesAvatarPath = resourcesAvatarPath;
            externalAvatarLocalPosition = position;
            externalAvatarLocalEuler = euler;
            externalAvatarScale = scale;
        }

        private const string AvatarLayerName = "DigitalHumanAvatar";
        private static int? cachedAvatarLayer;

        public bool EnsureLoaded(Transform parent)
        {
            if (SceneRoot != null)
            {
                return UsingExternalAvatar;
            }

            SceneRoot = new GameObject("DigitalHumanAvatarScene").transform;
            SceneRoot.SetParent(parent, false);
            SceneRoot.localPosition = Vector3.zero;
            CreateCameraAndLight();

            if (TryLoadExternalAvatar())
            {
                return true;
            }

            Debug.LogError(
                "[DigitalHumanAvatarView] No external avatar prefab found! " +
                "Place a Mixamo/FBX prefab at Resources/DigitalHuman/Avatar.prefab " +
                "or assign one to the avatarPrefab field in the Inspector.");
            return false;
        }

        public void Cleanup()
        {
            if (ExternalAvatarInstance != null)
            {
                Object.Destroy(ExternalAvatarInstance);
                ExternalAvatarInstance = null;
            }

            ExternalAnimator = null;
            UsingExternalAvatar = false;
            ModelRoot = null;

            if (SceneRoot != null)
            {
                Object.Destroy(SceneRoot.gameObject);
                SceneRoot = null;
            }
        }

        public void SwitchToAvatar(string resourcesPath)
        {
            resourcesAvatarPath = resourcesPath;
            Cleanup();
        }

        // ──────────────────────────────────────────────
        //  内部实现
        // ──────────────────────────────────────────────

        private void CreateCameraAndLight()
        {
            RenderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "DigitalHumanAvatarTexture"
            };
            RenderTexture.Create();

            GameObject cameraObject = new GameObject("AvatarCamera");
            cameraObject.transform.SetParent(SceneRoot, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.93f, -3.05f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(
                new Vector3(0f, -0.04f, 1f), Vector3.up);
            AvatarCamera = cameraObject.AddComponent<Camera>();
            AvatarCamera.targetTexture = RenderTexture;
            AvatarCamera.clearFlags = CameraClearFlags.SolidColor;
            AvatarCamera.backgroundColor = new Color32(230, 240, 252, 255);
            AvatarCamera.fieldOfView = 28f;
            AvatarCamera.nearClipPlane = 0.05f;
            AvatarCamera.farClipPlane = 20f;

            GameObject keyLight = new GameObject("AvatarKeyLight");
            keyLight.transform.SetParent(SceneRoot, false);
            keyLight.transform.localPosition = new Vector3(-1.2f, 2.7f, -1.8f);
            keyLight.transform.localRotation = Quaternion.Euler(44f, 28f, 0f);
            Light directional = keyLight.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.18f;

            GameObject fillLight = new GameObject("AvatarFillLight");
            fillLight.transform.SetParent(SceneRoot, false);
            fillLight.transform.localPosition = new Vector3(1.35f, 1.45f, -1.2f);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 4f;
            fill.intensity = 0.7f;

            int layer = GetAvatarLayer();
            AvatarCamera.cullingMask = 1 << layer;
            SetLayerRecursive(SceneRoot.gameObject, layer);
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

            ExternalAvatarInstance = Object.Instantiate(prefab, SceneRoot, false);
            ExternalAvatarInstance.name = "RuntimeDigitalHumanAvatar";
            ModelRoot = ExternalAvatarInstance.transform;
            ModelRoot.localPosition = externalAvatarLocalPosition;
            ModelRoot.localRotation =
                Quaternion.Euler(externalAvatarLocalEuler) * Quaternion.Euler(0f, 180f, 0f);
            ModelRoot.localScale = Vector3.one * Mathf.Max(0.01f, externalAvatarScale);
            ExternalAnimator = ExternalAvatarInstance.GetComponentInChildren<Animator>();

            int layer = GetAvatarLayer();
            SetLayerRecursive(ExternalAvatarInstance, layer);

            BindHumanoidBones();
            AutoColorMaterials();

            Bounds bounds = CalculateModelBounds();
            if (bounds.extents.magnitude > 0.01f)
            {
                FrameCameraToModel(bounds);
            }
            else
            {
                Debug.LogWarning(
                    "[DigitalHumanAvatarView] Loaded external avatar has near-zero bounds. " +
                    "Camera will use default position.");
            }

            UsingExternalAvatar = true;
            return true;
        }

        private void BindHumanoidBones()
        {
            if (ExternalAnimator != null && ExternalAnimator.isHuman)
            {
                BindFromHumanoidAvatar();
                return;
            }

            BindByNameFallback();
        }

        private void BindFromHumanoidAvatar()
        {
            Hips = ExternalAnimator.GetBoneTransform(HumanBodyBones.Hips);
            Spine = ExternalAnimator.GetBoneTransform(HumanBodyBones.Spine);
            Chest = ExternalAnimator.GetBoneTransform(HumanBodyBones.Chest);
            Neck = ExternalAnimator.GetBoneTransform(HumanBodyBones.Neck);
            Head = ExternalAnimator.GetBoneTransform(HumanBodyBones.Head);
            LeftUpperArm = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            LeftLowerArm = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            LeftHand = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            RightUpperArm = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            RightLowerArm = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            RightHand = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            LeftUpperLeg = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            LeftLowerLeg = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            LeftFoot = ExternalAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            RightUpperLeg = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            RightLowerLeg = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            RightFoot = ExternalAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            Mouth = ExternalAnimator.GetBoneTransform(HumanBodyBones.Jaw);
            if (Mouth == null && Head != null)
            {
                Mouth = FindDeepChild(Head, "mouth", "jaw", "openjaw");
            }
        }

        private void BindByNameFallback()
        {
            Hips = FindDeepChild(ModelRoot, "hips", "pelvis", "root");
            Spine = FindDeepChild(ModelRoot, "spine");
            Chest = FindDeepChild(ModelRoot, "chest", "upperchest", "spine2");
            Neck = FindDeepChild(ModelRoot, "neck");
            Head = FindDeepChild(ModelRoot, "head");
            LeftUpperArm = FindDeepChild(ModelRoot, "leftupperarm", "left_arm", "l_upperarm");
            LeftLowerArm = FindDeepChild(ModelRoot, "leftlowerarm", "leftforearm", "l_forearm");
            RightUpperArm = FindDeepChild(ModelRoot, "rightupperarm", "right_arm", "r_upperarm");
            RightLowerArm = FindDeepChild(ModelRoot, "rightlowerarm", "rightforearm", "r_forearm");
            LeftUpperLeg = FindDeepChild(ModelRoot, "leftupperleg", "leftupleg", "l_thigh");
            RightUpperLeg = FindDeepChild(ModelRoot, "rightupperleg", "rightupleg", "r_thigh");
            Mouth = FindDeepChild(ModelRoot, "mouth", "jaw", "openjaw");
        }

        private void AutoColorMaterials()
        {
            if (ModelRoot == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                       ?? Shader.Find("Standard")
                       ?? Shader.Find("Legacy Shaders/Diffuse")
                       ?? Shader.Find("Diffuse");
            if (shader == null) return;

            System.Action<Renderer, Color> assign = (renderer, color) =>
            {
                Material mat = new Material(shader) { name = "AutoMat_" + renderer.gameObject.name, color = color };
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
                renderer.sharedMaterial = mat;
            };

            MeshRenderer[] meshes = ModelRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in meshes)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null) continue;
                assign(r, PickBodyPartColor(r.transform));
            }

            SkinnedMeshRenderer[] skinned = ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in skinned)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null) continue;
                assign(r, PickBodyPartColor(r.transform));
            }
        }

        private static Color PickBodyPartColor(Transform meshTransform)
        {
            Transform walker = meshTransform;
            for (int depth = 0; walker != null && depth < 10; depth++)
            {
                string name = walker.name.ToLowerInvariant();
                if (name.Contains("head") || name.Contains("neck")) return new Color32(255, 222, 194, 255);
                if (name.Contains("hand")) return new Color32(255, 222, 194, 255);
                if (name.Contains("upperarm") || name.Contains("lowerarm")) return new Color32(255, 222, 194, 255);
                if (name.Contains("arm")) return new Color32(38, 170, 211, 255);
                if (name.Contains("thigh") || name.Contains("upleg") || name.Contains("lowerleg")) return new Color32(82, 108, 152, 255);
                if (name.Contains("leg")) return new Color32(82, 108, 152, 255);
                if (name.Contains("foot")) return new Color32(42, 45, 54, 255);
                if (name.Contains("spine") || name.Contains("chest") || name.Contains("hips")) return new Color32(38, 170, 211, 255);
                if (name.Contains("mouth") || name.Contains("jaw")) return new Color32(200, 100, 100, 255);
                if (name.Contains("eye")) return Color.white;
                walker = walker.parent;
            }
            return new Color32(255, 222, 194, 255);
        }

        private Bounds CalculateModelBounds()
        {
            if (ModelRoot == null) return new Bounds();
            Renderer[] renderers = ModelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void FrameCameraToModel(Bounds modelBounds)
        {
            if (AvatarCamera == null) return;

            Vector3 center = modelBounds.center;
            float size = Mathf.Max(modelBounds.extents.magnitude, 0.4f);
            float distance = size / Mathf.Tan(AvatarCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            AvatarCamera.transform.position = center + new Vector3(0f, size * 0.15f, -distance);
            AvatarCamera.transform.LookAt(center + Vector3.up * size * 0.15f);
        }

        // ── 工具方法 ──

        private static Transform FindDeepChild(Transform root, params string[] keys)
        {
            if (root == null || keys == null) return null;

            string name = NormalizeBoneName(root.name);
            for (int i = 0; i < keys.Length; i++)
            {
                if (name.Contains(NormalizeBoneName(keys[i])))
                    return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), keys);
                if (found != null) return found;
            }

            return null;
        }

        private static string NormalizeBoneName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("_", string.Empty).Replace(" ", string.Empty)
                       .Replace(":", string.Empty).ToLowerInvariant();
        }
        private static int GetAvatarLayer()
        {
            if (cachedAvatarLayer.HasValue) return cachedAvatarLayer.Value;

            int layer = LayerMask.NameToLayer(AvatarLayerName);
            if (layer < 0)
            {
                layer = 30;
                Debug.LogWarning(
                    "[DigitalHumanAvatarLoader] Layer \"" + AvatarLayerName + "\" not found. " +
                    "Using layer " + layer + ". Add it in Project Settings -> Tags and Layers.");
            }

            cachedAvatarLayer = layer;
            return layer;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
