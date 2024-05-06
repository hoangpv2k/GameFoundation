using System;
using JetBrains.Annotations;
using UnityEngine;

namespace CustomTween {
    /// <summary>TweenSettings contains animation properties (duration, ease, delay, etc.). Can be serialized and tweaked from the Inspector.<br/>
    /// Use this struct when the 'start' and 'end' values of an animation are NOT known in advance and determined at runtime.<br/>
    /// When the 'start' and 'end' values ARE known, consider using <see cref="TweenSettings{T}"/> instead.</summary>
    /// <example>
    /// Tweak an animation settings from the Inspector, then pass the settings to the Tween method:
    /// <code>
    /// [SerializeField] TweenSettings animationSettings;
    /// public void AnimatePositionX(float targetPosX) {
    ///     Tween.PositionX(transform, targetPosX, animationSettings);
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public struct TweenSettings {
        public float duration;
        [Tooltip("The easing curve of an animation.\n\n" +
                 "Default is Ease." + nameof(Ease.OutQuad) + ". The Default ease can be modified via '" + nameof(CustomTweenConfig) + "." + nameof(CustomTweenConfig.defaultEase) + "' setting.\n\n" +
                 "Set to " + nameof(Ease) + "." + nameof(Ease.Custom) + " to control the easing with custom " + nameof(AnimationCurve) + ".")]
        public Ease ease;
        [Tooltip("A custom Animation Curve that will work as an easing curve.")]
        [CanBeNull] public AnimationCurve customEase;
        [Tooltip(Constants.cyclesTooltip)]
        public int cycles;
        [Tooltip("See the documentation of each cycle mode by hoovering the dropdown.")]
        public CycleMode cycleMode;
        [Tooltip(Constants.startDelayTooltip)]
        public float startDelay;
        [Tooltip(Constants.endDelayTooltip)]
        public float endDelay;
        [Tooltip(Constants.unscaledTimeTooltip)]
        public bool useUnscaledTime;
        public bool useFixedUpdate;
        [NonSerialized] internal ParametricEase parametricEase;
        [NonSerialized] internal float parametricEaseStrength;
        [NonSerialized] internal float parametricEasePeriod;

        TweenSettings(float duration, Ease ease, Easing? customEasing, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, bool useFixedUpdate = false) {
            this.duration = duration;
            var curve = customEasing?.curve;
            if (ease == Ease.Custom && customEasing?.parametricEase == ParametricEase.None) {
                if (curve == null || !ValidateCustomCurveKeyframes(curve)) {
                    Debug.LogError("Ease is Ease.Custom, but AnimationCurve is not configured correctly. Using Ease.Default instead.");
                    ease = Ease.Default;
                }
            }
            this.ease = ease;
            customEase = ease == Ease.Custom ? curve : null;
            this.cycles = cycles;
            this.cycleMode = cycleMode;
            this.startDelay = startDelay;
            this.endDelay = endDelay;
            this.useUnscaledTime = useUnscaledTime;
            parametricEase = customEasing?.parametricEase ?? ParametricEase.None;
            parametricEaseStrength = customEasing?.parametricEaseStrength ?? float.NaN;
            parametricEasePeriod = customEasing?.parametricEasePeriod ?? float.NaN;
            this.useFixedUpdate = useFixedUpdate;
        }

        #if PRIME_TWEEN_DOTWEEN_ADAPTER
        internal void SetEasing(Easing easing) {
            ease = easing.ease;
            parametricEase = easing.parametricEase;
            parametricEaseStrength = easing.parametricEaseStrength;
            parametricEasePeriod = easing.parametricEasePeriod;
        }
        #endif
        
        public TweenSettings(float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, bool useFixedUpdate = false)
            : this(duration, ease, null, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, useFixedUpdate) {
        }

        public TweenSettings(float duration, Easing easing, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, bool useFixedUpdate = false)
            : this(duration, easing.ease, easing, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, useFixedUpdate) {
        }
  
        internal static void setCyclesTo1If0(ref int cycles) {
            if (cycles == 0) {
                cycles = 1;
            }
        }

        internal void CopyFrom(ref TweenSettings other) {
            duration = other.duration;
            ease = other.ease;
            customEase = other.customEase;
            cycles = other.cycles;
            cycleMode = other.cycleMode;
            startDelay = other.startDelay;
            endDelay = other.endDelay;
            useUnscaledTime = other.useUnscaledTime;
            parametricEase = other.parametricEase;
            parametricEaseStrength = other.parametricEaseStrength;
            parametricEasePeriod = other.parametricEasePeriod;
            useFixedUpdate = other.useFixedUpdate;
        }

        internal void SetValidValues() {
            validateFiniteDuration(duration);
            validateFiniteDuration(startDelay);
            validateFiniteDuration(endDelay);
            setCyclesTo1If0(ref cycles);
            if (duration != 0f) {
                duration = Mathf.Max(0.001f, duration);
            }
            startDelay = Mathf.Max(0f, startDelay);
            endDelay = Mathf.Max(0f, endDelay);
            if (cycles == 1) {
                cycleMode = CycleMode.Restart;
            }
        }

        internal static void validateFiniteDuration(float f) {
            Assert.IsFalse(float.IsNaN(f), Constants.durationInvalidError);
            Assert.IsFalse(float.IsInfinity(f), Constants.durationInvalidError);
        }
        
        internal static bool ValidateCustomCurve([NotNull] AnimationCurve curve) {
            #if UNITY_ASSERTIONS && !PRIME_TWEEN_DISABLE_ASSERTIONS
            if (curve.length < 2) {
                Debug.LogError("Custom animation curve should have at least 2 keyframes, please edit the curve in Inspector.");
                return false;
            }
            return true;
            #else
            return true;
            #endif
        }

        internal static bool ValidateCustomCurveKeyframes([NotNull] AnimationCurve curve) {
            #if UNITY_ASSERTIONS && !PRIME_TWEEN_DISABLE_ASSERTIONS
            if (!ValidateCustomCurve(curve)) {
                return false;
            }
            var instance = CustomTweenManager.Instance;
            if (instance == null || instance.validateCustomCurves) {
                var error = getError();
                if (error != null) {
                    Debug.LogWarning($"Custom animation curve is not configured correctly which may have unexpected results: {error}. " +
                                     Constants.buildWarningCanBeDisabledMessage(nameof(CustomTweenConfig.validateCustomCurves)));
                }
                string getError() {
                    var start = curve[0];
                    if (!Mathf.Approximately(start.time, 0)) {
                        return "start time is not 0";
                    }
                    if (!Mathf.Approximately(start.value, 0) && !Mathf.Approximately(start.value, 1)) {
                        return "start value is not 0 or 1";
                    }
                    var end = curve[curve.length - 1];
                    if (!Mathf.Approximately(end.time, 1)) {
                        return "end time is not 1";
                    }
                    if (!Mathf.Approximately(end.value, 0) && !Mathf.Approximately(end.value, 1)) {
                        return "end value is not 0 or 1";
                    }
                    return null;
                }
            }
            return true;
            #else
            return true;
            #endif
        }
    }
    
    /// <summary>The standard animation easing types. Different easing curves produce a different animation 'feeling'.<br/>
    /// Play around with different ease types to choose one that suites you the best.
    /// You can also provide a custom AnimationCurve as an ease function or parametrize eases with the Easing.Overshoot/Elastic/BounceExact(...) methods.</summary>
    public enum Ease { Custom = -1, Default = 0, Linear = 1, 
        InSine, OutSine, InOutSine, 
        InQuad, OutQuad, InOutQuad, 
        InCubic, OutCubic, InOutCubic, 
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint, 
        InExpo, OutExpo, InOutExpo, 
        InCirc, OutCirc, InOutCirc, 
        InElastic, OutElastic, InOutElastic, 
        InBack, OutBack, InOutBack,
        InBounce, OutBounce, InOutBounce
    }
    
    /// <summary>Controls the behavior of subsequent cycles when a tween has more than one cycle.</summary>
    public enum CycleMode {
        [Tooltip("Restarts the tween from the beginning.")]
        Restart,
        [Tooltip("Swaps the 'startValue' and 'endValue' (easing is normal on the backward cycle).")]
        Yoyo,
        [Tooltip("At the end of a cycle increments 'startValue' and 'endValue' (startValue = endValue, endValue += endValue - startValue).\n\n" +
                 "For example, if tween moves position.x from 0 to 1, then after the first cycle, the tween will move the position.x from 1 to 2, and so on.")]
        Incremental,
        [Tooltip("Rewinds the tween as if time was reversed (easing is reversed on the backward cycle).")]
        Rewind
    }
}