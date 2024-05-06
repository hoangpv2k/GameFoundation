#if PRIME_TWEEN_SAFETY_CHECKS && UNITY_ASSERTIONS
#define SAFETY_CHECKS
#endif
using System;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CustomTween {
    [Serializable]
    internal class ReusableTween {
        #if UNITY_EDITOR
        [SerializeField, HideInInspector] internal string debugDescription;
        [SerializeField, CanBeNull, UsedImplicitly] internal UnityEngine.Object unityTarget;
        #endif
        internal int id = -1;
        /// Holds a reference to tween's target. If the target is UnityEngine.Object, the tween will gracefully stop when the target is destroyed. That is, destroying object with running tweens is perfectly ok.
        /// Keep in mind: when animating plain C# objects (not derived from UnityEngine.Object), the plugin will hold a strong reference to the object for the entire tween duration.
        ///     If plain C# target holds a reference to UnityEngine.Object and animates its properties, then it's user's responsibility to ensure that UnityEngine.Object still exists.
        [CanBeNull] internal object target;
        [SerializeField] internal bool _isPaused;
        internal bool _isAlive;
        [SerializeField] internal float elapsedTimeTotal;
        [SerializeField] internal float easedInterpolationFactor;
        internal float cycleDuration;
        internal PropType propType;
        internal TweenType tweenType;
        [SerializeField] internal ValueContainer startValue;
        [SerializeField] internal ValueContainer endValue;
        internal ValueContainer diff;
        internal bool isAdditive;
        internal ValueContainer prevVal;
        [SerializeField] internal TweenSettings settings;
        [SerializeField] int cyclesDone;
        const int iniCyclesDone = -1;

        internal object customOnValueChange;
        internal int intParam;
        Action<ReusableTween> onValueChange;

        [CanBeNull] Action<ReusableTween> onComplete;
        [CanBeNull] object onCompleteCallback;
        [CanBeNull] object onCompleteTarget;

        internal float waitDelay;
        internal Sequence sequence;
        internal Tween prev;
        internal Tween next;
        internal Tween prevSibling;
        internal Tween nextSibling;

        internal Func<ReusableTween, ValueContainer> getter;
        internal bool startFromCurrent;

        bool stoppedEmergently;
        internal readonly TweenCoroutineEnumerator coroutineEnumerator = new TweenCoroutineEnumerator();
        internal float timeScale = 1f;
        bool warnIgnoredOnCompleteIfTargetDestroyed = true;
        internal ShakeData shakeData;
        State state;
        bool warnEndValueEqualsCurrent;

        internal bool updateAndCheckIfRunning(float dt) {
            if (!_isAlive) {
                return sequence.IsCreated; // don't release a tween until sequence.releaseTweens()
            }
            if (!_isPaused) {
                SetElapsedTimeTotal(elapsedTimeTotal + dt * timeScale);
            }
            return _isAlive;
        }

        internal bool isUpdating;
        internal void SetElapsedTimeTotal(float newElapsedTimeTotal, bool earlyExitSequenceIfPaused = true) {
            if (isUpdating) {
                Debug.LogError(Constants.recursiveCallError);
                return;
            }
            isUpdating = true;
            if (!sequence.IsCreated) {
                setElapsedTimeTotal(newElapsedTimeTotal, out var cyclesDiff);
                if (!stoppedEmergently && _isAlive && isDone(cyclesDiff)) {
                    if (!_isPaused) {
                        kill();
                    }
                    ReportOnComplete();
                }
            } else {
                Assert.IsTrue(sequence.isAlive, id);
                if (isMainSequenceRoot()) {
                    Assert.IsTrue(sequence.root.id == id, id);
                    updateSequence(newElapsedTimeTotal, false, earlyExitSequenceIfPaused);
                }
            }
            isUpdating = false;
        }

        internal void updateSequence(float _elapsedTimeTotal, bool isRestart, bool earlyExitSequenceIfPaused = true) {
            Assert.IsTrue(isSequenceRoot());
            float prevEasedT = easedInterpolationFactor;
            setElapsedTimeTotal(_elapsedTimeTotal, out int cyclesDiff); // update sequence root

            bool isRestartToBeginning = isRestart && cyclesDiff < 0;
            Assert.IsTrue(!isRestartToBeginning || cyclesDone == 0 || cyclesDone == iniCyclesDone);
            if (cyclesDiff != 0 && !isRestartToBeginning) {
                // print($"           sequence cyclesDiff: {cyclesDiff}");
                if (isRestart) {
                    Assert.IsTrue(cyclesDiff > 0 && cyclesDone == settings.cycles);
                    cyclesDiff = 1;
                }
                int cyclesDiffAbs = Mathf.Abs(cyclesDiff);
                int newCyclesDone = cyclesDone;
                cyclesDone -= cyclesDiff;
                int cyclesDelta = cyclesDiff > 0 ? 1 : -1;
                var interpolationFactor = cyclesDelta > 0 ? 1f : 0f;
                for (int i = 0; i < cyclesDiffAbs; i++) {
                    Assert.IsTrue(!isRestart || i == 0);
                    if (cyclesDone == settings.cycles || cyclesDone == iniCyclesDone) {
                        // do nothing when moving backward from the last cycle or forward from the -1 cycle
                        cyclesDone += cyclesDelta;
                        continue;
                    }

                    var easedT = calcEasedT(interpolationFactor, cyclesDone);
                    var isForwardCycle = easedT > 0.5f;
                    const float negativeElapsedTime = -1000f;
                    if (!forceChildrenToPos()) {
                        return;
                    }
                    bool forceChildrenToPos() {
                        // complete the previous cycles by forcing all children tweens to 0f or 1f
                        // print($" (i:{i}) force to pos: {isForwardCycle}");
                        var simulatedSequenceElapsedTime = isForwardCycle ? float.MaxValue : negativeElapsedTime;
                        foreach (var t in getSequenceSelfChildren(isForwardCycle)) {
                            var tween = t.tween;
                            tween.updateSequenceChild(simulatedSequenceElapsedTime, isRestart);
                            if (isEarlyExitAfterChildUpdate()) {
                                return false;
                            }
                        }
                        return true;
                    }

                    cyclesDone += cyclesDelta;
                    var sequenceCycleMode = settings.cycleMode;
                    if (sequenceCycleMode == CycleMode.Restart && cyclesDone != settings.cycles && cyclesDone != iniCyclesDone) { // '&& cyclesDone != 0' check is wrong because we should do the restart when moving from 1 to 0 cyclesDone
                        if (!restartChildren()) {
                            return;
                        }
                        bool restartChildren() {
                            // print($"restart to pos: {!isForwardCycle}");
                            var simulatedSequenceElapsedTime = !isForwardCycle ? float.MaxValue : negativeElapsedTime;
                            prevEasedT = simulatedSequenceElapsedTime;
                            foreach (var t in getSequenceSelfChildren(!isForwardCycle)) {
                                var tween = t.tween;
                                tween.updateSequenceChild(simulatedSequenceElapsedTime, true);
                                if (isEarlyExitAfterChildUpdate()) {
                                    return false;
                                }
                                Assert.IsTrue(isForwardCycle || tween.cyclesDone == tween.settings.cycles, id);
                                Assert.IsTrue(!isForwardCycle || tween.cyclesDone <= 0, id);
                                Assert.IsTrue(isForwardCycle || tween.state == State.After, id);
                                Assert.IsTrue(!isForwardCycle || tween.state == State.Before, id);
                            }
                            return true;
                        }
                    }
                }
                Assert.IsTrue(newCyclesDone == cyclesDone, id);
                if (isDone(cyclesDiff)) {
                    if (isMainSequenceRoot() && !_isPaused) {
                        sequence.releaseTweens();
                    }
                    ReportOnComplete();
                    return;
                }
            }

            easedInterpolationFactor = Mathf.Clamp01(easedInterpolationFactor);
            bool isForward = easedInterpolationFactor > prevEasedT;
            float sequenceElapsedTime = easedInterpolationFactor * cycleDuration;
            foreach (var t in getSequenceSelfChildren(isForward)) {
                t.tween.updateSequenceChild(sequenceElapsedTime, isRestart);
                if (isEarlyExitAfterChildUpdate()) {
                    return;
                }
            }

            bool isEarlyExitAfterChildUpdate() {
                if (!sequence.isAlive) {
                    return true;
                }
                return earlyExitSequenceIfPaused && sequence.root.tween._isPaused; // access isPaused via root tween to bypass the cantManipulateNested check
            }
        }

        Sequence.SequenceDirectEnumerator getSequenceSelfChildren(bool isForward) {
            Assert.IsTrue(sequence.isAlive, id);
            return sequence.getSelfChildren(isForward);
        }

        bool isDone(int cyclesDiff) {
            Assert.IsTrue(settings.cycles == -1 || cyclesDone <= settings.cycles);
            if (timeScale >= 0f) {
                return cyclesDiff > 0 && cyclesDone == settings.cycles;
            }
            return cyclesDiff < 0 && cyclesDone == iniCyclesDone;
        }

        void updateSequenceChild(float encompassingElapsedTime, bool isRestart) {
            if (isSequenceRoot()) {
                updateSequence(encompassingElapsedTime, isRestart);
            } else {
                setElapsedTimeTotal(encompassingElapsedTime, out var cyclesDiff);
                if (!stoppedEmergently && _isAlive && isDone(cyclesDiff)) {
                    ReportOnComplete();
                }
            }
        }

        internal bool isMainSequenceRoot() => tweenType == TweenType.MainSequence;
        internal bool isSequenceRoot() => tweenType == TweenType.MainSequence || tweenType == TweenType.NestedSequence;

        void setElapsedTimeTotal(float _elapsedTimeTotal, out int cyclesDiff) {
            elapsedTimeTotal = _elapsedTimeTotal;
            float t = calcTFromElapsedTimeTotal(_elapsedTimeTotal, out cyclesDiff, out var newState);
            cyclesDone += cyclesDiff;
            if (newState == State.Running || state != newState) {
                if (isUnityTargetDestroyed()) {
                    EmergencyStop(true);
                    return;
                }
                var easedT = calcEasedT(t, cyclesDone);
                // print($"state: {state}/{newState}, cycles: {cyclesDone}/{settings.cycles} (diff: {cyclesDiff}), elapsedTimeTotal: {elapsedTimeTotal}, interpolation: {t}/{easedT}");
                state = newState;
                ReportOnValueChange(easedT);
            }
        }

        float calcTFromElapsedTimeTotal(float _elapsedTimeTotal, out int cyclesDiff, out State newState) {
            // key timeline points: 0 | startDelay | duration | 1 | endDelay | onComplete
            var cyclesTotal = settings.cycles;
            if (_elapsedTimeTotal == float.MaxValue) {
                Assert.AreNotEqual(-1, cyclesTotal);
                var cyclesLeft = cyclesTotal - cyclesDone;
                Assert.IsTrue(cyclesLeft >= 0);
                cyclesDiff = cyclesLeft;
                newState = State.After;
                return 1f;
            }
            _elapsedTimeTotal -= waitDelay; // waitDelay is applied before calculating cycles
            if (_elapsedTimeTotal < 0f) {
                cyclesDiff = iniCyclesDone - cyclesDone;
                newState = State.Before;
                return 0f;
            }
            Assert.IsTrue(_elapsedTimeTotal >= 0f);
            Assert.AreNotEqual(float.MaxValue, _elapsedTimeTotal);
            var duration = settings.duration;
            if (duration == 0f) {
                if (cyclesTotal == -1) {
                    cyclesDiff = cyclesDone == iniCyclesDone ? 2 : 1;
                    newState = State.Running;
                    return 1f;
                }
                Assert.AreNotEqual(-1, cyclesTotal);
                if (_elapsedTimeTotal == 0f) {
                    cyclesDiff = iniCyclesDone - cyclesDone;
                    newState = State.Before;
                    return 0f;
                }
                var cyclesLeft = cyclesTotal - cyclesDone;
                Assert.IsTrue(cyclesLeft >= 0);
                cyclesDiff = cyclesLeft;
                newState = State.After;
                return 1f;
            }
            Assert.AreNotEqual(0f, cycleDuration);
            var newCyclesDone = (int) (_elapsedTimeTotal / cycleDuration);
            if (cyclesTotal != -1 && newCyclesDone > cyclesTotal) {
                newCyclesDone = cyclesTotal;
            }
            cyclesDiff = newCyclesDone - cyclesDone;
            if (cyclesTotal != -1 && cyclesDone + cyclesDiff == cyclesTotal) {
                newState = State.After;
                return 1f;
            }
            var elapsedTimeInCycle = _elapsedTimeTotal - cycleDuration * newCyclesDone - settings.startDelay;
            if (elapsedTimeInCycle < 0f) {
                newState = State.Before;
                return 0f;
            }
            Assert.IsTrue(elapsedTimeInCycle >= 0f);
            Assert.AreNotEqual(0f, duration);
            var result = elapsedTimeInCycle / duration;
            if (result > 1f) {
                newState = State.After;
                return 1f;
            }
            newState = State.Running;
            Assert.IsTrue(result >= 0f);
            return result;
        }

        /*void print(object msg) {
            Debug.Log($"[{Time.frameCount}]  id {id}  {msg}");
        }*/

        internal void Reset() {
            Assert.IsFalse(isUpdating);
            Assert.IsFalse(_isAlive);
            Assert.IsFalse(sequence.IsCreated);
            Assert.IsFalse(prev.IsCreated);
            Assert.IsFalse(next.IsCreated);
            Assert.IsFalse(prevSibling.IsCreated);
            Assert.IsFalse(nextSibling.IsCreated);
            Assert.IsFalse(IsInSequence());
            if (shakeData.isAlive) {
                shakeData.Reset(this);
            }
            #if UNITY_EDITOR
            debugDescription = null;
            unityTarget = null;
            #endif
            id = -1;
            target = null;
            propType = PropType.None;
            settings.customEase = null;
            customOnValueChange = null;
            onValueChange = null;
            onComplete = null;
            onCompleteCallback = null;
            onCompleteTarget = null;
            getter = null;
            stoppedEmergently = false;
            waitDelay = 0f;
            coroutineEnumerator.resetEnumerator();
            tweenType = TweenType.None;
            timeScale = 1f;
            warnIgnoredOnCompleteIfTargetDestroyed = true;
            clearOnUpdate();
        }

        /// <param name="warnIfTargetDestroyed">https://github.com/KyryloKuzyk/CustomTween/discussions/4</param>
        internal void OnComplete([NotNull] Action _onComplete, bool warnIfTargetDestroyed) {
            Assert.IsNotNull(_onComplete);
            validateOnCompleteAssignment();
            warnIgnoredOnCompleteIfTargetDestroyed = warnIfTargetDestroyed;
            onCompleteCallback = _onComplete;
            onComplete = tween => {
                var callback = tween.onCompleteCallback as Action;
                Assert.IsNotNull(callback);
                try {
                    callback();
                } catch (Exception e) {
                    tween.handleOnCompleteException(e);
                }
            };
        }

        internal void OnComplete<T>([CanBeNull] T _target, [NotNull] Action<T> _onComplete, bool warnIfTargetDestroyed) where T : class {
            if (_target == null || isDestroyedUnityObject(_target)) {
                Debug.LogError($"{nameof(_target)} is null or has been destroyed. {Constants.onCompleteCallbackIgnored}");
                return;
            }
            Assert.IsNotNull(_onComplete);
            validateOnCompleteAssignment();
            warnIgnoredOnCompleteIfTargetDestroyed = warnIfTargetDestroyed;
            onCompleteTarget = _target;
            onCompleteCallback = _onComplete;
            onComplete = tween => {
                var callback = tween.onCompleteCallback as Action<T>;
                Assert.IsNotNull(callback);
                var _onCompleteTarget = tween.onCompleteTarget as T;
                if (isDestroyedUnityObject(_onCompleteTarget)) {
                    tween.warnOnCompleteIgnored(true);
                    return;
                }
                try {
                    callback(_onCompleteTarget);
                } catch (Exception e) {
                    tween.handleOnCompleteException(e);
                }
            };
        }

        void handleOnCompleteException(Exception e) {
            // Design decision: if a tween is inside a Sequence and user's tween.OnComplete() throws an exception, the Sequence should continue
            Assert.LogError($"Tween's onComplete callback raised exception, tween: {GetDescription()}, exception:\n{e}\n", id, target as UnityEngine.Object);
        }

        internal static bool isDestroyedUnityObject<T>(T obj) where T: class => obj is UnityEngine.Object unityObject && unityObject == null;

        void validateOnCompleteAssignment() {
            const string msg = "Tween already has an onComplete callback. Adding more callbacks is not allowed.\n" +
                               "Workaround: wrap a tween in a Sequence by calling Sequence.Create(tween) and use multiple ChainCallback().\n";
            Assert.IsNull(onCompleteTarget, msg);
            Assert.IsNull(onCompleteCallback, msg);
            Assert.IsNull(onComplete, msg);
        }

        /// _getter is null for custom tweens
        internal void Setup([CanBeNull] object _target, ref TweenSettings _settings, [NotNull] Action<ReusableTween> _onValueChange, [CanBeNull] Func<ReusableTween, ValueContainer> _getter, bool _startFromCurrent) {
            Assert.IsTrue(_settings.cycles >= -1);
            Assert.IsNotNull(_onValueChange);
            Assert.IsNull(getter);
            Assert.AreNotEqual(PropType.None, propType);
            #if UNITY_EDITOR
            if (Constants.noInstance) {
                return;
            }
            #endif
            if (_settings.ease == Ease.Default) {
                _settings.ease = CustomTweenManager.Instance.defaultEase;
            } else if (_settings.ease == Ease.Custom && _settings.parametricEase == ParametricEase.None) {
                if (_settings.customEase == null || !TweenSettings.ValidateCustomCurveKeyframes(_settings.customEase)) {
                    Debug.LogError($"Ease type is Ease.Custom, but {nameof(TweenSettings.customEase)} is not configured correctly.");
                    _settings.ease = CustomTweenManager.Instance.defaultEase;
                }
            }
            state = State.Before;
            target = _target;
            setUnityTarget(_target);
            elapsedTimeTotal = 0f;
            easedInterpolationFactor = float.MinValue;
            _isPaused = false;
            revive();

            cyclesDone = iniCyclesDone;
            _settings.SetValidValues();
            settings.CopyFrom(ref _settings);
            recalculateTotalDuration();
            Assert.IsTrue(cycleDuration >= 0);
            onValueChange = _onValueChange;
            Assert.IsFalse(_startFromCurrent && _getter == null);
            startFromCurrent = _startFromCurrent;
            getter = _getter;
            if (!_startFromCurrent) {
                cacheDiff();
            }
            if (propType == PropType.Quaternion) {
                prevVal.QuaternionVal = Quaternion.identity;
            } else {
                prevVal.Reset();
            }
            warnEndValueEqualsCurrent = CustomTweenManager.Instance.warnEndValueEqualsCurrent;
        }

        internal void setUnityTarget(object _target) {
            #if UNITY_EDITOR
            unityTarget = _target as UnityEngine.Object;
            #endif
        }

        /// Tween.Custom and Tween.ShakeCustom try-catch the <see cref="onValueChange"/> and calls <see cref="ReusableTween.EmergencyStop"/> if an exception occurs.
        /// <see cref="ReusableTween.EmergencyStop"/> sets <see cref="stoppedEmergently"/> to true.
        void ReportOnValueChange(float _easedInterpolationFactor) {
            // Debug.Log($"id {id}, ReportOnValueChange {_easedInterpolationFactor}");
            Assert.IsFalse(isUnityTargetDestroyed());
            if (startFromCurrent) {
                startFromCurrent = false;
                if (!ShakeData.TryTakeStartValueFromOtherShake(this)) {
                    startValue = getter(this);
                }
                if (startValue.Vector4Val == endValue.Vector4Val && warnEndValueEqualsCurrent && !shakeData.isAlive) {
                    Assert.LogWarning($"Tween's 'endValue' equals to the current animated value: {startValue.Vector4Val}, tween: {GetDescription()}.\n" +
                                      $"{Constants.buildWarningCanBeDisabledMessage(nameof(CustomTweenConfig.warnEndValueEqualsCurrent))}\n", id);
                }
                cacheDiff();
            }
            easedInterpolationFactor = _easedInterpolationFactor;
            onValueChange(this);
            if (stoppedEmergently || !_isAlive) {
                return;
            }
            onUpdate?.Invoke(this);
        }

        void ReportOnComplete() {
            // Debug.Log($"[{Time.frameCount}] id {id} ReportOnComplete() {easedInterpolationFactor}");
            Assert.IsFalse(startFromCurrent);
            Assert.IsTrue(timeScale < 0 || cyclesDone == settings.cycles);
            Assert.IsTrue(timeScale >= 0 || cyclesDone == iniCyclesDone);
            onComplete?.Invoke(this);
        }

        internal bool isUnityTargetDestroyed() {
            // must use target here instead of unityTarget
            // unityTarget has the SerializeField attribute, so if ReferenceEquals(unityTarget, null), then Unity will populate the field with non-null UnityEngine.Object when a new scene is loaded in the Editor
            // https://github.com/KyryloKuzyk/CustomTween/issues/32
            return isDestroyedUnityObject(target);
        }

        internal bool HasOnComplete => onComplete != null;

        [NotNull]
        internal string GetDescription() {
            string result = "";
            if (!_isAlive) {
                result += " - ";
            }
            if (target != CustomTweenManager.dummyTarget) {
                result += $"{(target is UnityEngine.Object unityObject && unityObject != null ? unityObject.name : target?.GetType().Name)} / ";
            }
            var duration = settings.duration;
            if (tweenType == TweenType.Delay) {
                if (duration == 0f && onComplete != null) {
                    result += "Callback";
                } else {
                    result += $"Delay / duration {duration}";
                }
            } else {
                if (tweenType == TweenType.MainSequence) {
                    result += $"Sequence {id}";
                } else if (tweenType == TweenType.NestedSequence) {
                    result += $"Sequence {id} (nested)";
                } else {
                    result += $"{(tweenType != TweenType.None ? tweenType.ToString() : propType.ToString())}" ;
                }
                result += " / duration ";
                /*if (waitDelay != 0f) {
                    result += $"{waitDelay}+";
                }*/
                result += $"{duration}";
            }
            result += $" / id {id}";
            if (sequence.IsCreated && tweenType != TweenType.MainSequence) {
                result += $" / sequence {sequence.root.id}";
            }
            return result;
        }

        internal float calcDurationWithWaitDependencies() {
            var cycles = settings.cycles;
            Assert.AreNotEqual(-1, cycles, "It's impossible to calculate the duration of an infinite tween (cycles == -1).");
            Assert.AreNotEqual(0, cycles);
            return waitDelay + cycleDuration * cycles;
        }

        internal void recalculateTotalDuration() {
            cycleDuration = settings.startDelay + settings.duration + settings.endDelay;
        }

        internal float FloatVal => startValue.x + diff.x * easedInterpolationFactor;
        internal Vector2 Vector2Val {
            get {
                var easedT = easedInterpolationFactor;
                return new Vector2(
                    startValue.x + diff.x * easedT,
                    startValue.y + diff.y * easedT);
            }
        }
        internal Vector3 Vector3Val {
            get {
                var easedT = easedInterpolationFactor;
                return new Vector3(
                    startValue.x + diff.x * easedT,
                    startValue.y + diff.y * easedT,
                    startValue.z + diff.z * easedT);
            }
        }
        internal Vector4 Vector4Val {
            get {
                var easedT = easedInterpolationFactor;
                return new Vector4(
                    startValue.x + diff.x * easedT,
                    startValue.y + diff.y * easedT,
                    startValue.z + diff.z * easedT,
                    startValue.w + diff.w * easedT);
            }
        }
        internal Color ColorVal {
            get {
                var easedT = easedInterpolationFactor;
                return new Color(
                    startValue.x + diff.x * easedT,
                    startValue.y + diff.y * easedT,
                    startValue.z + diff.z * easedT,
                    startValue.w + diff.w * easedT);
            }
        }
        internal Rect RectVal {
            get {
                var easedT = easedInterpolationFactor;
                return new Rect(
                    startValue.x + diff.x * easedT,
                    startValue.y + diff.y * easedT,
                    startValue.z + diff.z * easedT,
                    startValue.w + diff.w * easedT);
            }
        }
        internal Quaternion QuaternionVal => Quaternion.SlerpUnclamped(startValue.QuaternionVal, endValue.QuaternionVal, easedInterpolationFactor);

        float calcEasedT(float t, int cyclesDone) {
            var cycleMode = settings.cycleMode;
            var cyclesTotal = settings.cycles;
            if (cyclesDone == cyclesTotal) {
                // Debug.Log("cyclesDone == cyclesTotal");
                Assert.AreNotEqual(-1, cyclesTotal);
                switch (cycleMode) {
                    case CycleMode.Restart:
                        return evaluate(1f);
                    case CycleMode.Yoyo:
                    case CycleMode.Rewind:
                        return evaluate(cyclesTotal % 2);
                    case CycleMode.Incremental:
                        return cyclesTotal;
                    default:
                        throw new Exception();
                }
            }
            if (cycleMode == CycleMode.Restart) {
                return evaluate(t);
            }
            if (cycleMode == CycleMode.Incremental) {
                return evaluate(t) + cyclesDone;
            }
            var isForwardCycle = cyclesDone % 2 == 0;
            if (isForwardCycle) {
                return evaluate(t);
            }
            if (cycleMode == CycleMode.Yoyo) {
                return 1 - evaluate(t);
            }
            if (cycleMode == CycleMode.Rewind) {
                return evaluate(1 - t);
            }
            throw new Exception();
        }

        float evaluate(float t) {
            if (settings.ease == Ease.Custom) {
                if (settings.parametricEase != ParametricEase.None) {
                    return Easing.Evaluate(t, this);
                }
                return settings.customEase.Evaluate(t);
            }
            return StandardEasing.Evaluate(t, settings.ease);
        }

        internal void cacheDiff() {
            Assert.IsFalse(startFromCurrent);
            Assert.AreNotEqual(PropType.None, propType);
            if (propType == PropType.Quaternion) {
                startValue.QuaternionVal.Normalize();
                endValue.QuaternionVal.Normalize();
            } else {
                diff.x = endValue.x - startValue.x;
                diff.y = endValue.y - startValue.y;
                diff.z = endValue.z - startValue.z;
                diff.w = endValue.w - startValue.w;
            }
        }

        internal void ForceComplete() {
            Assert.IsFalse(sequence.IsCreated);
            kill(); // protects from recursive call
            if (isUnityTargetDestroyed()) {
                warnOnCompleteIgnored(true);
                return;
            }
            var cyclesTotal = settings.cycles;
            if (cyclesTotal == -1) {
                // same as SetRemainingCycles(1)
                cyclesTotal = getCyclesDone() + 1;
                settings.cycles = cyclesTotal;
            }
            cyclesDone = cyclesTotal;
            ReportOnValueChange(calcEasedT(1f, cyclesTotal));
            if (stoppedEmergently) {
                return;
            }
            ReportOnComplete();
            Assert.IsFalse(_isAlive);
        }

        internal void warnOnCompleteIgnored(bool isTargetDestroyed) {
            if (HasOnComplete && warnIgnoredOnCompleteIfTargetDestroyed) {
                onComplete = null;
                var msg = $"{Constants.onCompleteCallbackIgnored} Tween: {GetDescription()}.\n";
                if (isTargetDestroyed) {
                    msg += "\nIf you use tween.OnComplete(), Tween.Delay(), or sequence.ChainDelay() only for cosmetic purposes, you can turn off this error by passing 'warnIfTargetDestroyed: false' to the method.\n" +
                           "More info: https://github.com/KyryloKuzyk/CustomTween/discussions/4\n";
                }
                Assert.LogError(msg, id, target as UnityEngine.Object);
            }
        }

        internal void EmergencyStop(bool isTargetDestroyed = false) {
            if (sequence.IsCreated) {
                var mainSequence = sequence;
                while (true) {
                    var _prev = mainSequence.root.tween.prev;
                    if (!_prev.IsCreated) {
                        break;
                    }
                    var parent = _prev.tween.sequence;
                    if (!parent.IsCreated) {
                        break;
                    }
                    mainSequence = parent;
                }
                Assert.IsTrue(mainSequence.root.tween.isMainSequenceRoot());
                mainSequence.emergencyStop();
            } else if (_isAlive) {
                // EmergencyStop() can be called after ForceComplete() and a caught exception in Tween.Custom()
                kill();
            }
            stoppedEmergently = true;
            warnOnCompleteIgnored(isTargetDestroyed);
            Assert.IsFalse(_isAlive);
            Assert.IsFalse(sequence.isAlive);
        }

        internal void kill() {
            // Debug.Log($"[{Time.frameCount}] kill {GetDescription()}");
            Assert.IsTrue(_isAlive);
            _isAlive = false;
            #if UNITY_EDITOR
            debugDescription = null;
            #endif
        }

        void revive() {
            // Debug.Log($"[{Time.frameCount}] revive {GetDescription()}");
            Assert.IsFalse(_isAlive);
            _isAlive = true;
            #if UNITY_EDITOR
            debugDescription = null;
            #endif
        }

        internal bool IsInSequence() {
            var result = sequence.IsCreated;
            Assert.IsTrue(result || !nextSibling.IsCreated);
            return result;
        }

        internal bool canManipulate() => !IsInSequence() || isMainSequenceRoot();

        internal bool trySetPause(bool isPaused) {
            if (_isPaused == isPaused) {
                return false;
            }
            _isPaused = isPaused;
            return true;
        }

        [CanBeNull] object onUpdateTarget;
        object onUpdateCallback;
        Action<ReusableTween> onUpdate;

        internal void SetOnUpdate<T>(T _target, [NotNull] Action<T, Tween> _onUpdate) where T : class {
            Assert.IsNull(onUpdate, "Only one OnUpdate() is allowed for one tween.");
            Assert.IsNotNull(_onUpdate, nameof(_onUpdate) + " is null!");
            onUpdateTarget = _target;
            onUpdateCallback = _onUpdate;
            onUpdate = reusableTween => reusableTween.invokeOnUpdate<T>();
        }

        void invokeOnUpdate<T>() where T : class {
            var callback = onUpdateCallback as Action<T, Tween>;
            Assert.IsNotNull(callback);
            var _onUpdateTarget = onUpdateTarget as T;
            if (isDestroyedUnityObject(_onUpdateTarget)) {
                Assert.LogError($"OnUpdate() will not be called again because OnUpdate()'s target has been destroyed, tween: {GetDescription()}", id, target as UnityEngine.Object);
                clearOnUpdate();
                return;
            }
            try {
                callback(_onUpdateTarget, new Tween(this));
            } catch (Exception e) {
                Assert.LogError($"OnUpdate() will not be called again because it thrown exception, tween: {GetDescription()}, exception:\n{e}", id, target as UnityEngine.Object);
                clearOnUpdate();
            }
        }

        void clearOnUpdate() {
            onUpdateTarget = null;
            onUpdateCallback = null;
            onUpdate = null;
        }

        public override string ToString() {
            return GetDescription();
        }

        enum State {
            Before, Running, After
        }

        internal float getElapsedTimeTotal() {
            var result = elapsedTimeTotal;
            var durationTotal = getDurationTotal();
            if (result == float.MaxValue) {
                return durationTotal;
            }
            return Mathf.Clamp(result, 0f, durationTotal);
        }

        internal float getDurationTotal() {
            var cyclesTotal = settings.cycles;
            if (cyclesTotal == -1) {
                return float.PositiveInfinity;
            }
            Assert.AreNotEqual(0, cyclesTotal);
            return cycleDuration * cyclesTotal;
        }

        internal int getCyclesDone() {
            int result = cyclesDone;
            if (result == iniCyclesDone) {
                return 0;
            }
            Assert.IsTrue(result >= 0);
            return result;
        }
    }
}
