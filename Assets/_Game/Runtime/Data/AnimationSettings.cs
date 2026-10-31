using UnityEngine;

namespace Game.Runtime.Data
{
    // Every duration, speed and strength of the game feel (§10.9). Can be tweaked in Play mode; no hard-coded values in code.
    [CreateAssetMenu(menuName = "Game/Animation Settings")]
    public sealed class AnimationSettings : ScriptableObject
    {
        [Header("Passenger")]
        [Tooltip("Cells per second")]
        [SerializeField] private float walkSpeed = 4.0f;
        [SerializeField] private float stepHeight = 0.15f;
        [SerializeField] private float stepTime = 0.12f;
        [SerializeField] private Vector3 tapSquash = new Vector3(0.2f, -0.2f, 0.2f);
        [SerializeField] private float tapSquashTime = 0.2f;
        [SerializeField] private float blockedShakeStrength = 0.1f;
        [SerializeField] private float blockedShakeTime = 0.3f;
        [SerializeField] private float boardHopHeight = 0.5f;
        [SerializeField] private float boardHopTime = 0.2f;
        [SerializeField] private float boardFromSlotTime = 0.25f;
        [SerializeField] private float introPopTime = 0.25f;
        [SerializeField] private float introStagger = 0.02f;
        [SerializeField] private float idleBreathScale = 0.03f;
        [SerializeField] private float idleBreathTime = 1.2f;

        [Header("Bus")]
        [SerializeField] private float busArriveTime = 0.5f;
        [SerializeField] private float busDepartTime = 0.4f;
        [SerializeField] private float indicatorSpacing = 0.3f;

        [Header("UI")]
        [SerializeField] private float popupTime = 0.25f;
        [SerializeField] private float nodePulseScale = 1.1f;
        [SerializeField] private float nodePulseTime = 0.6f;
        [Tooltip("HUD timer pulse in the last 10 seconds")]
        [SerializeField] private float timerPulseScale = 1.15f;
        [SerializeField] private float timerPulseTime = 0.5f;
        [Tooltip("A pressed button shrinks to this scale")]
        [SerializeField] private float buttonPressScale = 0.9f;
        [SerializeField] private float buttonPressTime = 0.08f;

        public float WalkSpeed => walkSpeed;
        public float StepHeight => stepHeight;
        public float StepTime => stepTime;
        public Vector3 TapSquash => tapSquash;
        public float TapSquashTime => tapSquashTime;
        public float BlockedShakeStrength => blockedShakeStrength;
        public float BlockedShakeTime => blockedShakeTime;
        public float BoardHopHeight => boardHopHeight;
        public float BoardHopTime => boardHopTime;
        public float BoardFromSlotTime => boardFromSlotTime;
        public float IntroPopTime => introPopTime;
        public float IntroStagger => introStagger;
        public float IdleBreathScale => idleBreathScale;
        public float IdleBreathTime => idleBreathTime;
        public float BusArriveTime => busArriveTime;
        public float BusDepartTime => busDepartTime;
        public float IndicatorSpacing => indicatorSpacing;
        public float PopupTime => popupTime;
        public float NodePulseScale => nodePulseScale;
        public float NodePulseTime => nodePulseTime;
        public float TimerPulseScale => timerPulseScale;
        public float TimerPulseTime => timerPulseTime;
        public float ButtonPressScale => buttonPressScale;
        public float ButtonPressTime => buttonPressTime;
    }
}
