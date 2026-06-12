using System;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanInputHandler : MonoBehaviour
    {
        public event Action<DigitalHumanModule> ModuleRequested;
        public event Action<DigitalHumanDifficulty> DifficultyRequested;
        public event Action<string> OptionSubmitted;
        public event Action<string> TextSubmitted;
        public event Action<string> VoiceTextSubmitted;
        public event Action<string, DigitalHumanParticipant> ColoringAreaSubmitted;
        public event Action ImitationPauseRequested;
        public event Action ImitationConfirmRequested;

        public void RequestModule(DigitalHumanModule module)
        {
            ModuleRequested?.Invoke(module);
        }

        public void RequestDifficulty(DigitalHumanDifficulty difficulty)
        {
            DifficultyRequested?.Invoke(difficulty);
        }

        public void SubmitOption(string optionId)
        {
            OptionSubmitted?.Invoke(optionId);
        }

        public void SubmitText(string text)
        {
            TextSubmitted?.Invoke(text);
        }

        public void SubmitVoiceRecognizedText(string recognizedText)
        {
            VoiceTextSubmitted?.Invoke(recognizedText);
        }

        public void SubmitColoringArea(string areaId, DigitalHumanParticipant participant)
        {
            ColoringAreaSubmitted?.Invoke(areaId, participant);
        }

        public void ToggleImitationPause()
        {
            ImitationPauseRequested?.Invoke();
        }

        public void ConfirmImitation()
        {
            ImitationConfirmRequested?.Invoke();
        }
    }
}
