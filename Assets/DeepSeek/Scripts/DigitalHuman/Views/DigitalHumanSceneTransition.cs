using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanSceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeSeconds = 3f;

        private Coroutine fadeRoutine;
        private bool isFading;

        public bool IsFading => isFading;

        private void Awake()
        {
            EnsureBuilt();
        }

        public void Configure(float seconds)
        {
            fadeSeconds = Mathf.Max(0.1f, seconds);
        }

        public IEnumerator FadeOutIn()
        {
            EnsureBuilt();
            yield return FadeOut();
            yield return FadeIn();
        }

        public IEnumerator FadeOut()
        {
            EnsureBuilt();
            yield return FadeTo(1f, fadeSeconds * 0.5f);
        }

        public IEnumerator FadeIn()
        {
            EnsureBuilt();
            yield return FadeTo(0f, fadeSeconds * 0.5f);
        }

        public void LoadSceneWithFade(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            yield return FadeTo(1f, fadeSeconds * 0.5f);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            yield return FadeTo(0f, fadeSeconds * 0.5f);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            EnsureBuilt();
            isFading = true;

            // 【修复】在淡入淡出动画进行中，始终阻止射线检测
            fadeGroup.blocksRaycasts = true;

            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;

            // 【修复】只有当淡入完成（alpha接近0）时才关闭射线阻止
            // 淡出完成（alpha=1）时仍然阻止，防止切换过程中点击
            // 使用 targetAlpha > 0.01f 而不是 > 0.01 来更精确判断
            fadeGroup.blocksRaycasts = targetAlpha > 0.01f;

            isFading = false;
        }

        private void EnsureBuilt()
        {
            if (fadeGroup != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("DigitalHumanTransitionCanvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;  // 【增强】确保在最上层
                canvas.overrideSorting = true;  // 【增强】强制覆盖排序
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject fade = new GameObject("SceneFade");
            fade.transform.SetParent(canvas.transform, false);
            RectTransform rect = fade.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = fade.AddComponent<Image>();
            image.color = new Color32(255, 255, 255, 255);
            fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }
}
