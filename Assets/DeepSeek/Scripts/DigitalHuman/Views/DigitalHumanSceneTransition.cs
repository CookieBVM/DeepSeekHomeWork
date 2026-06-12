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
            fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
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
                canvas.sortingOrder = 1000;
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
