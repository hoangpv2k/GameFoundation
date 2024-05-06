// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedParameter.Global
using System;
using System.ComponentModel;
using JetBrains.Annotations;
using UnityEngine;

namespace CustomTween {
    public partial class CustomTweenConfig {
        // ReSharper disable once ValueParameterNotUsed
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("this setting is replaced with the 'warnIfTargetDestroyed' parameter that you can pass to the tween.OnComplete(), Tween.Delay() and Sequence.ChainDelay() methods.")]
        public static bool warnDestroyedTweenHasOnComplete { set {} }
    }
    
    static class Messages {
        /// Obsolete on 2023-08-29
        internal const string obsoleteIsAliveMessage = "please use 'isAlive' (lower case 'i') instead.";
        /// Obsolete on 2023-08-29
        internal const string obsoleteIsPausedMessage = "please use 'isPaused' (lower case 'i') instead.";
        internal const string obsoleteTweenSetCycles = "SetCycles() was renamed to SetRemainingCycles().";
    }
    
    public partial struct Tween {
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteTweenSetCycles)]
        public void SetCycles(bool stopAtEndValue) => SetRemainingCycles(stopAtEndValue);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteTweenSetCycles)]
        public void SetCycles(int cycles) => SetRemainingCycles(cycles);

        /// Obsolete on 2023-11-24
        const string minMaxExpected = "numMinExpected/numMaxExpected parameters are no longer supported.";
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int StopAll([CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => StopAll(onTarget);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int CompleteAll([CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => CompleteAll(onTarget);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int SetPausedAll(bool isPaused, [CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => SetPausedAll(isPaused, onTarget);

        
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteIsAliveMessage)]
        public bool IsAlive => isAlive;
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteIsPausedMessage)]
        public bool IsPaused => isPaused;

        /// Obsolete on 2023-08-30 
        const string localScaleRenamed = "please use 'Scale' instead of 'LocalScale'.";
        
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<Vector3>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<Vector3>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 startValue, Vector3 endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<Vector3>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 startValue, Vector3 endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<Vector3>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<Vector3>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 startValue, Vector3 endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<Vector3>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, TweenSettings<Vector3> settings) {
            return animate(target, ref settings, _tween => {
                var _target = _tween.target as Transform;
                var val = _tween.Vector3Val;
                _target.localScale = val;
            }, t => (t.target as Transform).localScale.ToContainer());
        }

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleX(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleX(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single startValue, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleX(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleX(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleX(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleX(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, TweenSettings<float> settings) {
            return animate(target, ref settings, _tween => {
                var _target = _tween.target as Transform;
                var val = _tween.FloatVal;
                _target.localScale = _target.localScale.WithComponent(0, val);
            }, t => (t.target as Transform).localScale.x.ToContainer());
        }

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleY(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleY(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single startValue, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleY(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleY(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleY(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleY(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, TweenSettings<float> settings) {
            return animate(target, ref settings, _tween => {
                var _target = _tween.target as Transform;
                var val = _tween.FloatVal;
                _target.localScale = _target.localScale.WithComponent(1, val);
            }, t => (t.target as Transform).localScale.y.ToContainer());
        }

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleZ(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleZ(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single startValue, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleZ(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScaleZ(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleZ(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleZ(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, TweenSettings<float> settings) {
            return animate(target, ref settings, _tween => {
                var _target = _tween.target as Transform;
                var val = _tween.FloatVal;
                _target.localScale = _target.localScale.WithComponent(2, val);
            }, t => (t.target as Transform).localScale.z.ToContainer());
        }
        
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single startValue, Single endValue, float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) 
            => LocalScale(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, TweenSettings<float> uniformScaleSettings) => Scale(target, uniformScaleSettings);
        
        
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween ShakeLocalScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = CustomTweenConfig.defaultUseUnscaledTimeForShakes) 
            => ShakeScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween ShakeLocalScale([NotNull] Transform target, ShakeSettings settings) => ShakeScale(target, settings);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween PunchLocalScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = CustomTweenConfig.defaultUseUnscaledTimeForShakes) 
            => PunchScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween PunchLocalScale([NotNull] Transform target, ShakeSettings settings) => ShakeScale(target, settings.WithPunch());
    }
    
    public partial struct Sequence {
        /// Obsolete on 2023-11-28
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("please use Sequence.Create(cycles: numCycles) instead. Or use SetRemainingCycles() to modify the number of remaining cycles when the Sequence is already running.")]
        public Sequence SetCycles(int cycles) {
            SetRemainingCycles(cycles);
            return this;
        }
        
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteIsAliveMessage)]
        public bool IsAlive => isAlive;
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(Messages.obsoleteIsPausedMessage)]
        public bool IsPaused => isPaused;
    }
}