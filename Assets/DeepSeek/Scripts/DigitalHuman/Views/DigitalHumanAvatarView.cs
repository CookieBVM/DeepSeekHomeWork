using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanAvatarView : MonoBehaviour
    {
        [Header("渲染")]
        [SerializeField] private RawImage viewport;
        [SerializeField] private int textureSize = 768;

        [Header("外部模型")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private string resourcesAvatarPath = "DigitalHuman/Avatar";
        [SerializeField] private Vector3 externalAvatarLocalPosition = new Vector3(0f, -0.6f, 200f);
        [SerializeField] private Vector3 externalAvatarLocalEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private float externalAvatarScale = 1.2f;

        private RenderTexture renderTexture;
        private Camera avatarCamera;
        private Transform sceneRoot;
        private Transform modelRoot;
        private Animator avatarAnimator;
        private Coroutine returnToIdleRoutine;
        private static readonly int IdleStateHash = Animator.StringToHash("Idle");

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
            if (sceneRoot != null) return;

            sceneRoot = new GameObject("DigitalHumanAvatarScene").transform;
            sceneRoot.SetParent(transform, false);
            sceneRoot.localPosition = new Vector3(-400f, 0f, 0f);

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
                Debug.LogWarning("[DigitalHumanAvatarView] No avatar prefab found.");
                return;
            }

            GameObject avatarInstance = Instantiate(prefab, sceneRoot, false);
            avatarInstance.name = "RuntimeDigitalHumanAvatar";
            modelRoot = avatarInstance.transform;

            modelRoot.localPosition = externalAvatarLocalPosition;
            modelRoot.localRotation = Quaternion.Euler(externalAvatarLocalEuler);
            modelRoot.localScale = Vector3.one * externalAvatarScale;

            avatarAnimator = avatarInstance.GetComponentInChildren<Animator>();
            if (avatarAnimator != null)
            {
                avatarAnimator.applyRootMotion = false;
                avatarAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                if (avatarAnimator.runtimeAnimatorController == null)
                {
                    var controller = Resources.Load<RuntimeAnimatorController>(
                        "DigitalHuman/AvatarController");
                    if (controller != null)
                    {
                        avatarAnimator.runtimeAnimatorController = controller;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[DigitalHumanAvatarView] AvatarController not found. " +
                            "Run DeepSeek > Setup Mixamo Animations in Unity Editor.");
                    }
                }
            }
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
            cameraObject.transform.localPosition = new Vector3(0f, 102f, 11f);
            cameraObject.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            avatarCamera = cameraObject.AddComponent<Camera>();
            avatarCamera.targetTexture = renderTexture;
            avatarCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarCamera.backgroundColor = new Color32(190, 200, 218, 255);
            avatarCamera.fieldOfView = 60f;
            avatarCamera.nearClipPlane = 0.05f;
            avatarCamera.farClipPlane = 400f;

            GameObject keyLight = new GameObject("AvatarKeyLight");
            keyLight.transform.SetParent(sceneRoot, false);
            keyLight.transform.localPosition = new Vector3(-2f, 2.5f, 0f);
            keyLight.transform.localRotation = Quaternion.Euler(45f, 75f, 0f);
            Light light = keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.55f;

            GameObject fillLight = new GameObject("AvatarFillLight");
            fillLight.transform.SetParent(sceneRoot, false);
            fillLight.transform.localPosition = new Vector3(1.8f, 1f, 0.5f);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 2.5f;
            fill.intensity = 0.25f;
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
            if (avatarAnimator == null) return;

            string triggerName = MapPoseToTrigger(pose);
            if (string.IsNullOrEmpty(triggerName))
            {
                avatarAnimator.Play(IdleStateHash, 0, 0f);
                return;
            }

            avatarAnimator.ResetTrigger(triggerName);
            avatarAnimator.SetTrigger(triggerName);

            if (returnToIdleRoutine != null)
            {
                StopCoroutine(returnToIdleRoutine);
            }

            returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterClip(triggerName));
        }

        public void SetAnimationSpeed(float speed)
        {
            if (avatarAnimator != null)
            {
                avatarAnimator.speed = speed;
            }
        }

        public void PlayInteractiveGreeting()
        {
            if (avatarAnimator != null)
            {
                avatarAnimator.SetTrigger("Greeting");

                if (returnToIdleRoutine != null)
                {
                    StopCoroutine(returnToIdleRoutine);
                }

                returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterClip("Greeting"));
            }
        }

        private IEnumerator ReturnToIdleAfterClip(string triggerName)
        {
            yield return null;

            var stateInfo = avatarAnimator.GetCurrentAnimatorStateInfo(0);
            float clipLength = stateInfo.length;

            if (triggerName == "Greeting")
            {
                clipLength *= 2.5f;
            }

            if (clipLength > 0.05f)
            {
                yield return new WaitForSeconds(clipLength);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            avatarAnimator.Play(IdleStateHash, 0, 0f);
            returnToIdleRoutine = null;
        }

        private static string MapPoseToTrigger(DigitalHumanAvatarPose pose)
        {
            // Idle: no trigger, handled directly
            if (pose == DigitalHumanAvatarPose.Idle)
                return null;

            return pose.ToString();
        }
    }
}