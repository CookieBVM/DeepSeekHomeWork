using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    /// <summary>
    /// 数字人形象视图 - 通过RenderTexture将3D模型渲染到UI RawImage
    /// Ch46模型 + Animator驱动的动画系统
    /// </summary>
    public class DigitalHumanAvatarView : MonoBehaviour
    {
        [Header("渲染")]
        [SerializeField] private RawImage viewport;
        [SerializeField] private int textureSize = 768;

        [Header("外部模型")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private string resourcesAvatarPath = "DigitalHuman/Avatar";
        [SerializeField] private Vector3 externalAvatarLocalPosition = new Vector3(0f, -0.6f, 0.5f);
        [SerializeField] private Vector3 externalAvatarLocalEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private float externalAvatarScale = 1.2f;

        private RenderTexture renderTexture;
        private Camera avatarCamera;
        private Transform sceneRoot;
        private Transform modelRoot;

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
                try
                {
                    if (avatarCamera != null && avatarCamera.targetTexture == renderTexture)
                    {
                        avatarCamera.targetTexture = null;
                    }

                    if (viewport != null && viewport.texture == renderTexture)
                    {
                        viewport.texture = null;
                    }

                    renderTexture.Release();
                    DestroyImmediate(renderTexture);
                }
                catch (System.Exception)
                {
                }

                renderTexture = null;
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
            LoadAvatarFromResources();

            if (viewport != null && renderTexture != null)
            {
                viewport.texture = renderTexture;
            }
        }

        private void LoadAvatarFromResources()
        {
            GameObject prefab = avatarPrefab;
            if (prefab == null && !string.IsNullOrWhiteSpace(resourcesAvatarPath))
            {
                prefab = Resources.Load<GameObject>(resourcesAvatarPath);
            }

            if (prefab == null)
            {
                Debug.LogWarning("[DigitalHumanAvatarView] No avatar prefab found. Please assign one in the inspector.");
                return;
            }

            GameObject avatarInstance = Instantiate(prefab, sceneRoot, false);
            avatarInstance.name = "RuntimeDigitalHumanAvatar";
            modelRoot = avatarInstance.transform;

            modelRoot.localPosition = externalAvatarLocalPosition;
            modelRoot.localRotation = Quaternion.Euler(externalAvatarLocalEuler);
            modelRoot.localScale = Vector3.one * externalAvatarScale;

            // 获取Animator但不禁用 - 让模型自带动画（如有）正常工作
            Animator externalAnimator = avatarInstance.GetComponentInChildren<Animator>();
            if (externalAnimator != null)
            {
                externalAnimator.applyRootMotion = false;
                externalAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void CreateCameraAndLight()
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "DigitalHumanAvatarTexture"
            };
            renderTexture.Create();

            // 相机从正面拍摄模型，完整显示在viewport中
            GameObject cameraObject = new GameObject("AvatarCamera");
            cameraObject.transform.SetParent(sceneRoot, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.8f, 2.5f);
            cameraObject.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            avatarCamera = cameraObject.AddComponent<Camera>();
            avatarCamera.targetTexture = renderTexture;
            avatarCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarCamera.backgroundColor = new Color32(230, 240, 252, 255);
            avatarCamera.fieldOfView = 35f;
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

        public void BindViewport(RawImage targetViewport)
        {
            viewport = targetViewport;
            if (renderTexture != null)
            {
                viewport.texture = renderTexture;
            }
        }

        public void ApplyPose(DigitalHumanAvatarPose pose, DigitalHumanEmotion emotion)
        {
            // 姿态动画接口 - 保留供GameController调用
            // Animator驱动的动画不需要此处的姿态代码
        }

        public void SetAnimationSpeed(float speed)
        {
            // 动画速度接口 - 保留供GameController调用
        }

        public void PlayInteractiveGreeting()
        {
            // 交互问候接口 - 保留供GameController调用
        }
    }
}
