using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanAvatarView : MonoBehaviour
    {
        [SerializeField] private RawImage viewport;

        private void Awake()
        {
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
        }

        public void BindViewport(RawImage viewport)
        {
            this.viewport = viewport;
        }

        public void ApplyPose(DigitalHumanAvatarPose pose, DigitalHumanEmotion emotion)
        {
        }

        public void SetAnimationSpeed(float speed)
        {
        }

        public void PlayInteractiveGreeting()
        {
        }
    }
}