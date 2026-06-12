using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    [CreateAssetMenu(menuName = "DeepSeek/Digital Human Theme", fileName = "DigitalHumanTheme")]
    public class DigitalHumanThemeConfig : ScriptableObject
    {
        [Header("Accessible UI")]
        public Color32 backgroundColor = new Color32(255, 247, 236, 225);
        public Color32 panelColor = new Color32(255, 255, 255, 255);
        public Color32 primaryColor = new Color32(248, 177, 91, 255);
        public Color32 secondaryColor = new Color32(124, 178, 255, 255);
        public Color32 textColor = new Color32(47, 52, 64, 255);
        public Color32 successColor = new Color32(83, 181, 111, 255);
        public Color32 softWarningColor = new Color32(255, 210, 110, 255);

        [Header("Typography")]
        public int titleFontSize = 40;
        public int bodyFontSize = 30;
        public int optionFontSize = 34;

        [Header("Timing")]
        public float sceneFadeSeconds = 3f;
        public float rewardStickerSeconds = 1.6f;
        public float encouragementMusicSeconds = 10f;

        public static DigitalHumanThemeConfig CreateRuntime(DigitalHumanModule module)
        {
            var config = CreateInstance<DigitalHumanThemeConfig>();
            switch (module)
            {
                case DigitalHumanModule.ParentChildColoring:
                    config.backgroundColor = new Color32(244, 255, 248, 225);
                    config.primaryColor = new Color32(97, 197, 148, 255);
                    config.secondaryColor = new Color32(255, 188, 120, 255);
                    break;
                case DigitalHumanModule.ActionImitation:
                    config.backgroundColor = new Color32(245, 250, 255, 225);
                    config.primaryColor = new Color32(118, 167, 255, 255);
                    config.secondaryColor = new Color32(255, 204, 112, 255);
                    break;
                default:
                    config.backgroundColor = new Color32(255, 247, 236, 225);
                    config.primaryColor = new Color32(248, 177, 91, 255);
                    config.secondaryColor = new Color32(255, 219, 150, 255);
                    break;
            }

            return config;
        }
    }
}
