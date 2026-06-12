using System.Collections;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanSpeechService : MonoBehaviour
    {
        [SerializeField] private bool enablePlaceholderSpeech = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float minSpeechSeconds = 0.7f;
        [SerializeField] private float maxSpeechSeconds = 4f;

        public bool IsSpeaking { get; private set; }

        private Coroutine speechRoutine;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void OnEnable()
        {
            DigitalHumanEventBus.DigitalHumanResponded += HandleResponse;
        }

        private void OnDisable()
        {
            DigitalHumanEventBus.DigitalHumanResponded -= HandleResponse;
        }

        public void SpeakText(string text)
        {
            if (!enablePlaceholderSpeech || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (speechRoutine != null)
            {
                StopCoroutine(speechRoutine);
            }

            speechRoutine = StartCoroutine(SpeechRoutine(text));
        }

        private void HandleResponse(DigitalHumanResponse response)
        {
            SpeakText(response.Line);
        }

        private IEnumerator SpeechRoutine(string text)
        {
            IsSpeaking = true;
            float seconds = Mathf.Clamp(text.Length * 0.08f, minSpeechSeconds, maxSpeechSeconds);
            AudioClip clip = CreatePlaceholderSpeechClip(seconds);
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = 0.18f;
            audioSource.Play();
            yield return new WaitForSeconds(seconds);
            IsSpeaking = false;
        }

        private static AudioClip CreatePlaceholderSpeechClip(float seconds)
        {
            int sampleRate = 16000;
            int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float syllable = 0.5f + 0.5f * Mathf.Sin(time * 18f);
                float carrier = Mathf.Sin(2f * Mathf.PI * 220f * time);
                float envelope = Mathf.Clamp01(Mathf.Min(time / 0.08f, (seconds - time) / 0.15f));
                samples[i] = carrier * syllable * envelope * 0.12f;
            }

            AudioClip clip = AudioClip.Create("DigitalHumanPlaceholderSpeech", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
