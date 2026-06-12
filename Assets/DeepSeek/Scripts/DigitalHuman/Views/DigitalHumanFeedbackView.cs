using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanFeedbackView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup stickerGroup;
        [SerializeField] private Text stickerText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip encouragementClip;
        [SerializeField] private float fallbackFrequency = 523.25f;

        private Coroutine stickerRoutine;

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            DigitalHumanEventBus.RewardRequested += ShowReward;
            DigitalHumanEventBus.TaskCompleted += HandleTaskCompleted;
        }

        private void OnDisable()
        {
            DigitalHumanEventBus.RewardRequested -= ShowReward;
            DigitalHumanEventBus.TaskCompleted -= HandleTaskCompleted;
        }

        public void ShowReward(string message, float visibleSeconds)
        {
            EnsureBuilt();
            if (stickerRoutine != null)
            {
                StopCoroutine(stickerRoutine);
            }

            stickerRoutine = StartCoroutine(StickerRoutine(message, visibleSeconds));
        }

        private void HandleTaskCompleted(DigitalHumanModule module, string taskId)
        {
            PlayEncouragement(10f);
        }

        private IEnumerator StickerRoutine(string message, float visibleSeconds)
        {
            stickerText.text = string.IsNullOrWhiteSpace(message) ? "做得真好！" : message;
            stickerGroup.alpha = 0f;
            stickerGroup.transform.localScale = Vector3.one * 0.6f;

            float inSeconds = 0.18f;
            for (float t = 0f; t < inSeconds; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / inSeconds);
                stickerGroup.alpha = p;
                stickerGroup.transform.localScale = Vector3.Lerp(Vector3.one * 0.6f, Vector3.one * 1.05f, p);
                yield return null;
            }

            stickerGroup.alpha = 1f;
            stickerGroup.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(Mathf.Max(0.3f, visibleSeconds));

            for (float t = 0f; t < inSeconds; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / inSeconds);
                stickerGroup.alpha = 1f - p;
                yield return null;
            }

            stickerGroup.alpha = 0f;
        }

        private void PlayEncouragement(float seconds)
        {
            EnsureBuilt();
            AudioClip clip = encouragementClip != null ? encouragementClip : CreateSoftTone(seconds);
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = clip;
            audioSource.volume = 0.35f;
            audioSource.Play();
            StartCoroutine(StopAudioAfter(seconds));
        }

        private IEnumerator StopAudioAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void EnsureBuilt()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("DigitalHumanFeedbackCanvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 900;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (stickerGroup != null)
            {
                return;
            }

            GameObject sticker = new GameObject("RewardSticker");
            sticker.transform.SetParent(canvas.transform, false);
            RectTransform rect = sticker.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.36f, 0.64f);
            rect.anchorMax = new Vector2(0.64f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = sticker.AddComponent<Image>();
            image.color = new Color32(83, 181, 111, 235);
            stickerGroup = sticker.AddComponent<CanvasGroup>();
            stickerGroup.alpha = 0f;

            GameObject textObject = new GameObject("StickerText");
            textObject.transform.SetParent(sticker.transform, false);
            stickerText = textObject.AddComponent<Text>();
            stickerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stickerText.text = "做得真好！";
            stickerText.fontSize = 38;
            stickerText.color = Color.white;
            stickerText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = stickerText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
        }

        private AudioClip CreateSoftTone(float seconds)
        {
            int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * Mathf.Max(1f, seconds));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Clamp01(Mathf.Min(time / 0.6f, (seconds - time) / 1.2f));
                float toneA = Mathf.Sin(2f * Mathf.PI * fallbackFrequency * time);
                float toneB = Mathf.Sin(2f * Mathf.PI * fallbackFrequency * 1.25f * time);
                samples[i] = (toneA * 0.18f + toneB * 0.08f) * envelope;
            }

            AudioClip clip = AudioClip.Create("DigitalHumanSoftEncouragement", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
