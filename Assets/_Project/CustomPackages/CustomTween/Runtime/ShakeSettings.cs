using System;
using JetBrains.Annotations;
using UnityEngine;

namespace CustomTween {
    /// <summary>
    /// ShakeSettings contains all properties needed for a shake or punch (frequency, strength per axis, duration, etc.). Can be serialized and tweaked from the Inspector.<br/>
    /// Shake methods are: Tween.ShakeLocalPosition(), Tween.ShakeLocalRotation(), Tween.ShakeScale(), and Tween.ShakeCustom().<br/><br/>
    /// Punch is a special case of a shake that a has a punch 'direction'. The punched value will oscillate back and forth in the direction of a punch.<br/> 
    /// Punch methods are: Tween.PunchLocalPosition(), Tween.PunchLocalRotation(), Tween.PunchScale(), and Tween.PunchCustom().<br/>
    /// </summary>
    [Serializable]
    public struct ShakeSettings {
        internal const float defaultFrequency = 10;
        
        [Tooltip("Strength is applied per-axis in local space coordinates.\n\n" +
                 "Shakes: the strongest strength component will be used as the main frequency axis. Shakes on secondary axes happen randomly instead of following the frequency.\n\n" +
                 "Punches: strength determines punch direction.\n\n" +
                 "Strength is measured in units (position/scale) or Euler angles (rotation).")]
        public Vector3 strength;
        public float duration;
        [Tooltip("Number of shakes per second.")]
        public float frequency;
        [Tooltip("With enabled falloff shake starts at the highest strength and fades to the end.")]
        public bool enableFalloff;
        [Tooltip("Falloff ease is inverted to achieve the effect of shake 'fading' over time. Typically, eases go from 0 to 1, but falloff ease goes from 1 to 0.\n\n" +
                 "Default is Ease.Linear.\n\n" +
                 "Set to " + nameof(Ease) + "." + nameof(Ease.Custom) + " to have manual control over shake's 'strength' over time.")]
        public Ease falloffEase;
        [Tooltip("Shake's 'strength' over time.")]
        [CanBeNull] public AnimationCurve strengthOverTime;
        [Tooltip("Represents how asymmetrical the shake is.\n\n" +
                 "'0' means the shake is symmetrical around the initial value.\n\n" +
                 "'1' means the shake is asymmetrical and will happen between the initial position and the value of the '" + nameof(strength) + "' vector.\n\n" +
                 "When used with punches, can be treated as the resistance to 'recoil': '0' is full recoil, '1' is no recoil.")]
        [Range(0f, 1f)] public float asymmetry;
        /// <see cref="CustomTweenManager.defaultShakeEase"/>
        [Tooltip("Ease between adjacent shake points.\n\n" +
                 "Default is Ease.OutQuad.")]
        public Ease easeBetweenShakes;
        [Tooltip(Constants.cyclesTooltip)]
        public int cycles;
        [Tooltip(Constants.startDelayTooltip)]
        public float startDelay;
        [Tooltip(Constants.endDelayTooltip)]
        public float endDelay;
        [Tooltip(Constants.unscaledTimeTooltip)]
        public bool useUnscaledTime;
        public bool useFixedUpdate;
        internal bool isPunch { get; private set; }

        ShakeSettings(Vector3 strength, float duration, float frequency, Ease? falloffEase, [CanBeNull] AnimationCurve strengthOverTime, Ease easeBetweenShakes, float asymmetryFactor, int cycles, float startDelay, float endDelay, bool useUnscaledTime, bool useFixedUpdate) {
            this.frequency = frequency;
            this.strength = strength;
            this.duration = duration;
            if (falloffEase == Ease.Custom) {
                if (strengthOverTime == null || !TweenSettings.ValidateCustomCurve(strengthOverTime)) {
                    Debug.LogError($"Shake falloff is Ease.Custom, but {nameof(this.strengthOverTime)} is not configured correctly. Using Ease.Linear instead.");
                    falloffEase = Ease.Linear;
                }
            }
            this.falloffEase = falloffEase ?? default;
            this.strengthOverTime = falloffEase == Ease.Custom ? strengthOverTime : null;
            enableFalloff = falloffEase != null;
            this.easeBetweenShakes = easeBetweenShakes;
            this.cycles = cycles;
            this.startDelay = startDelay;
            this.endDelay = endDelay;
            this.useUnscaledTime = useUnscaledTime;
            asymmetry = asymmetryFactor;
            isPunch = false;
            this.useFixedUpdate = useFixedUpdate;
        }

        public ShakeSettings(Vector3 strength, float duration = 0.5f, float frequency = defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = CustomTweenConfig.defaultUseUnscaledTimeForShakes, bool useFixedUpdate = false)
            // ReSharper disable once RedundantCast
            : this(strength, duration, frequency, enableFalloff ? Ease.Default : (Ease?)null, null, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime, useFixedUpdate) {}

        public ShakeSettings(Vector3 strength, float duration, float frequency, AnimationCurve strengthOverTime, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = CustomTweenConfig.defaultUseUnscaledTimeForShakes, bool useFixedUpdate = false)
            : this(strength, duration, frequency, Ease.Custom, strengthOverTime, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime, useFixedUpdate) { }

        internal TweenSettings tweenSettings => new TweenSettings(duration, Ease.Linear, cycles, CycleMode.Restart, startDelay, endDelay, useUnscaledTime, useFixedUpdate);

        internal 
            #if UNITY_2020_2_OR_NEWER
            readonly
            #endif
            ShakeSettings WithPunch() {
            var result = this;
            result.isPunch = true;
            return result;
        }
    }
}