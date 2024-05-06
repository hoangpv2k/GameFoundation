#if PRIME_TWEEN_SAFETY_CHECKS && UNITY_ASSERTIONS
#define SAFETY_CHECKS
#endif
#if PRIME_TWEEN_INSPECTOR_DEBUGGING && UNITY_EDITOR
#define ENABLE_SERIALIZATION
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace CustomTween {
    /// <summary>An ordered group of tweens and callbacks. Tweens in a sequence can run in parallel to one another with <see cref="Group"/> and sequentially with <see cref="Chain"/>.<br/>
    /// To make tweens in a Sequence overlap each other, use <see cref="TweenSettings.startDelay"/> and <see cref="TweenSettings.endDelay"/>.</summary>
    /// <example><code>
    /// Sequence.Create()
    ///     .Group(Tween.PositionX(transform, endValue: 10f, duration: 1.5f))
    ///     .Group(Tween.Scale(transform, endValue: 2f, duration: 0.5f)) // position and localScale tweens will run in parallel because they are 'grouped'
    ///     .Chain(Tween.Rotation(transform, endValue: new Vector3(0f, 0f, 45f), duration: 1f)) // rotation tween is 'chained' so it will start when both previous tweens are finished (after 1.5 seconds) 
    ///     .ChainCallback(() =&gt; Debug.Log("Sequence completed"));
    /// </code></example>
    #if ENABLE_SERIALIZATION
    [Serializable]
    #endif
    public 
        #if !ENABLE_SERIALIZATION && UNITY_2020_3_OR_NEWER
        readonly // duration setter produces error in Unity <= 2019.4.40: error CS1604: Cannot assign to 'this' because it is read-only
        #endif
        partial struct Sequence {
        const int emptySequenceTag = -43;
        internal 
            #if !ENABLE_SERIALIZATION && UNITY_2020_3_OR_NEWER
            readonly
            #endif
            Tween root;
        internal bool IsCreated => root.IsCreated;
        int id => root.id;

        /// Sequence is 'alive' when any of its tweens is 'alive'.
        public bool isAlive => root.isAlive;
        
        /// Elapsed time of the current cycle.
        public float elapsedTime {
            get => root.elapsedTime;
            set => root.elapsedTime = value;
        }

        /// The total number of cycles. Returns -1 to indicate infinite number cycles.
        public int cyclesTotal => root.cyclesTotal;
        public int cyclesDone => root.cyclesDone;
        /// The duration of one cycle.
        public float duration {
            get => root.duration;
            private set {
                Assert.IsTrue(isAlive);
                Assert.IsTrue(root.tween.isMainSequenceRoot());
                var rootTween = root.tween;
                Assert.AreEqual(0f, elapsedTimeTotal);
                Assert.IsTrue(value >= rootTween.cycleDuration);
                Assert.IsTrue(value >= rootTween.settings.duration);
                Assert.AreEqual(0f, rootTween.settings.startDelay);
                Assert.AreEqual(0f, rootTween.settings.endDelay);
                rootTween.settings.duration = value;
                rootTween.cycleDuration = value;
            }
        }

        /// Elapsed time of all cycles.
        public float elapsedTimeTotal {
            get => root.elapsedTimeTotal;
            set => root.elapsedTimeTotal = value;
        }

        /// <summary>The duration of all cycles. If cycles == -1, returns <see cref="float.PositiveInfinity"/>.</summary>
        public float durationTotal => root.durationTotal;
        
        /// Normalized progress of the current cycle expressed in 0..1 range.
        public float progress {
            get => root.progress;
            set => root.progress = value;
        }

        /// Normalized progress of all cycles expressed in 0..1 range.
        public float progressTotal {
            get => root.progressTotal;
            set => root.progressTotal = value;
        }

        bool tryManipulate() => root.tryManipulate();

        bool ValidateCanManipulateSequence() {
            if (!tryManipulate()) {
                return false;
            }
            if (root.elapsedTimeTotal != 0f) {
                Debug.LogError(Constants.sequenceAlreadyStarted);
                return false;
            }
            return true;
        }

        public static Sequence Create(int cycles = 1, CycleMode cycleMode = CycleMode.Restart, Ease sequenceEase = Ease.Linear, bool useUnscaledTime = false, bool useFixedUpdate = false) {
            #if UNITY_EDITOR
            if (Constants.warnNoInstance) {
                return default;
            }
            #endif
            var tween = CustomTweenManager.fetchTween();
            tween.propType = PropType.Float;
            tween.tweenType = TweenType.MainSequence;
            if (cycleMode == CycleMode.Incremental) {
                Debug.LogError($"Sequence doesn't support CycleMode.Incremental. Parameter {nameof(sequenceEase)} is applied to the sequence's 'timeline', and incrementing the 'timeline' doesn't make sense. For the same reason, {nameof(sequenceEase)} is clamped to [0:1] range.");
                cycleMode = CycleMode.Restart;
            }
            if (sequenceEase == Ease.Custom) {
                Debug.LogError("Sequence doesn't support Ease.Custom.");
                sequenceEase = Ease.Linear;
            }
            if (sequenceEase == Ease.Default) {
                sequenceEase = Ease.Linear;
            }
            var settings = new TweenSettings(0f, sequenceEase, cycles, cycleMode, 0f, 0f, useUnscaledTime, useFixedUpdate);
            tween.Setup(CustomTweenManager.dummyTarget, ref settings, _ => {}, null, false);
            tween.intParam = emptySequenceTag;
            var root = CustomTweenManager.addTween(tween);
            Assert.IsTrue(root.isAlive);
            return new Sequence(root);
        }

        public static Sequence Create(Tween firstTween) {
            #if UNITY_EDITOR
            if (Constants.warnNoInstance) {
                return default;
            }
            #endif
            return Create().Group(firstTween);
        }

        Sequence(Tween rootTween) {
            root = rootTween;
            setSequence(rootTween);
            Assert.IsTrue(isAlive);
            Assert.AreEqual(0f, duration);
            Assert.IsTrue(durationTotal == 0f || float.IsPositiveInfinity(durationTotal));
        }

        /// <summary>Groups <paramref name="tween"/> with the 'previous' animation (tween or sequence) in this Sequence.<br/>
        /// Grouped animations start at the same time and run in parallel.<br/>
        /// If the 'previous' operation is Chain(), then <paramref name="tween"/> will start at the same time with the 'previously' chained animation.</summary>
        public Sequence Group(Tween tween) {
            if (tryManipulate()) {
                Insert(getLastInSelfOrRoot().tween.waitDelay, tween);
            }
            return this;
        }

        void addLinkedReference(Tween tween) {
            Tween last;
            if (root.tween.next.IsCreated) {
                last = getLast();
                var lastInSelf = getLastInSelfOrRoot();
                Assert.AreNotEqual(root.id, lastInSelf.id);
                Assert.IsFalse(lastInSelf.tween.nextSibling.IsCreated);
                lastInSelf.tween.nextSibling = tween;
                Assert.IsFalse(tween.tween.prevSibling.IsCreated);
                tween.tween.prevSibling = lastInSelf;
            } else {
                last = root;
            }
            
            Assert.IsFalse(last.tween.next.IsCreated);
            Assert.IsFalse(tween.tween.prev.IsCreated);
            last.tween.next = tween;
            tween.tween.prev = last;
            root.tween.intParam = 0;
        }

        Tween getLast() {
            Tween result = default;
            foreach (var current in getAllTweens()) {
                result = current;
            }
            Assert.IsTrue(result.IsCreated);
            Assert.IsFalse(result.tween.next.IsCreated);
            return result;
        }
        
        /// <summary>Schedules <paramref name="tween"/> after all tweens and sequences in this Sequence.</summary>
        public Sequence Chain(Tween tween) {
            if (tryManipulate()) {
                Insert(duration, tween);
            }
            return this;
        }

        /// <summary>Places <paramref name="tween"/> inside this Sequence at time <paramref name="atTime"/>, leaving other animations unchanged.<br/>
        /// Sequence's duration will be increased if the inserted <paramref name="tween"/> doesn't fit inside the current duration.</summary>
        public Sequence Insert(float atTime, Tween tween) {
            if (!ValidateCanAdd(tween)) {
                return this;
            }
            if (tween.tween.sequence.IsCreated) {
                Debug.LogError($"{Constants.nestTweenTwiceError} Tween: {tween.tween.GetDescription()}");
                return this;
            }
            setSequence(tween);
            Insert_internal(atTime, tween);
            return this;
        }

        void Insert_internal(float atTime, Tween other) {
            Assert.AreEqual(0f, other.tween.waitDelay);
            other.tween.waitDelay = atTime;
            duration = Mathf.Max(duration, other.durationWithWaitDelay);
            addLinkedReference(other);
        }

        /// <summary>Schedules <see cref="callback"/> after all previously added tweens.</summary>
        /// <param name="warnIfTargetDestroyed">https://github.com/KyryloKuzyk/CustomTween/discussions/4</param>
        public Sequence ChainCallback([NotNull] Action callback, bool warnIfTargetDestroyed = true) {
            if (tryManipulate()) {
                InsertCallback(duration, callback, warnIfTargetDestroyed);
            }
            return this;
        }

        public Sequence InsertCallback(float atTime, Action callback, bool warnIfTargetDestroyed = true) {
            if (!tryManipulate()) {
                return this;
            }
            var maybeDelay = CustomTweenManager.delayWithoutDurationCheck(CustomTweenManager.dummyTarget, atTime, false);
            Assert.IsTrue(maybeDelay.HasValue);
            var delay = maybeDelay.Value;
            delay.tween.OnComplete(callback, warnIfTargetDestroyed);
            return Insert(0f, delay);
        }

        /// <summary>Schedules <see cref="callback"/> after all previously added tweens. Passing 'target' allows to write a non-allocating callback.</summary>
        /// <param name="warnIfTargetDestroyed">https://github.com/KyryloKuzyk/CustomTween/discussions/4</param>
        public Sequence ChainCallback<T>([NotNull] T target, [NotNull] Action<T> callback, bool warnIfTargetDestroyed = true) where T: class {
            if (tryManipulate()) {
                InsertCallback(duration, target, callback, warnIfTargetDestroyed);
            }
            return this;
        }

        public Sequence InsertCallback<T>(float atTime, [NotNull] T target, Action<T> callback, bool warnIfTargetDestroyed = true) where T: class {
            if (!tryManipulate()) {
                return this;
            }
            var maybeDelay = CustomTweenManager.delayWithoutDurationCheck(target, atTime, false);
            if (!maybeDelay.HasValue) {
                return this;
            }
            var delay = maybeDelay.Value;
            delay.tween.OnComplete(target, callback, warnIfTargetDestroyed);
            return Insert(0f, delay);
        }

        /// <summary>Schedules delay after all previously added tweens.</summary>
        public Sequence ChainDelay(float duration) {
            return Chain(Tween.Delay(duration));
        }

        Tween getLastInSelfOrRoot() {
            Assert.IsTrue(isAlive);
            var result = root;
            foreach (var current in getSelfChildren()) {
                result = current;
            }
            Assert.IsTrue(result.IsCreated);
            Assert.IsFalse(result.tween.nextSibling.IsCreated);
            return result;
        }

        void setSequence(Tween handle) {
            Assert.IsTrue(IsCreated);
            Assert.IsTrue(handle.isAlive); 
            var tween = handle.tween;
            Assert.IsFalse(tween.sequence.IsCreated);
            tween.sequence = this;
        }

        bool ValidateCanAdd(Tween other) {
            if (!ValidateCanManipulateSequence()) {
                return false;
            }
            if (!other.isAlive) {
                Debug.LogError(Constants.addDeadTweenToSequenceError);
                return false;
            }
            if (other.tween.settings.cycles == -1) {
                Debug.LogError(Constants.infiniteTweenInSequenceError);
                return false;
            }
            if (other.tween._isPaused) {
                warnIgnoredChildrenSetting(nameof(isPaused));
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (other.tween.timeScale != 1f) {
                warnIgnoredChildrenSetting(nameof(timeScale));
            }
            if (other.tween.settings.useUnscaledTime) {
                warnIgnoredChildrenSetting(nameof(TweenSettings.useUnscaledTime));
            }
            if (other.tween.settings.useFixedUpdate) {
                warnIgnoredChildrenSetting(nameof(TweenSettings.useFixedUpdate));
            }
            void warnIgnoredChildrenSetting(string settingName) {
                Debug.LogError($"'{settingName}' was ignored after adding tween/sequence to the Sequence. Parent Sequence controls isPaused/timeScale/useUnscaledTime/useFixedUpdate of all its children tweens and sequences.\n");
            }
            return true;
        }

        /// Stops all tweens in the Sequence, ignoring callbacks. 
        public void Stop() {
            if (isAlive && tryManipulate()) {
                Assert.IsTrue(root.tween.isMainSequenceRoot());
                releaseTweens();
                Assert.IsFalse(isAlive);
            }
        }

        /// <summary>Immediately completes the sequence.<br/>
        /// If the sequence has infinite cycles (cycles == -1), completes only the current cycle. To choose where the sequence should stop (at the 'start' or at the 'end') in the case of infinite cycles, use <see cref="SetRemainingCycles(bool stopAtEndValue)"/> before calling Complete().</summary>
        public void Complete() {
            if (isAlive && tryManipulate()) {
                if (cyclesTotal == -1 || root.tween.settings.cycleMode == CycleMode.Restart) {
                    SetRemainingCycles(1);
                } else {
                    int cyclesLeft = cyclesTotal - cyclesDone;
                    SetRemainingCycles(cyclesLeft % 2 == 1 ? 1 : 2);
                }
                root.isPaused = false;
                Assert.IsTrue(root.tween.isMainSequenceRoot());
                root.tween.updateSequence(float.MaxValue, false);
                Assert.IsFalse(isAlive);
            }
        }

        internal void emergencyStop() {
            Assert.IsTrue(isAlive);
            Assert.IsTrue(root.tween.isMainSequenceRoot());
            releaseTweens(t => t.warnOnCompleteIgnored(false));
        }

        internal void releaseTweens([CanBeNull] Action<ReusableTween> beforeKill = null) {
            var enumerator = getAllTweens();
            enumerator.MoveNext();
            var current = enumerator.Current;
            Assert.IsTrue(current.isAlive);
            while (true) {
                // ReSharper disable once RedundantCast
                Tween? next = enumerator.MoveNext() ? enumerator.Current : (Tween?)null;
                var tween = current.tween;
                Assert.IsTrue(tween._isAlive);
                beforeKill?.Invoke(tween);
                tween.kill();
                Assert.IsFalse(tween._isAlive);
                releaseTween(tween);
                if (!next.HasValue) {
                    break;
                }
                current = next.Value;
            }
            Assert.IsFalse(isAlive); // not IsCreated because this may be a local variable in the user's codebase
        }

        static void releaseTween([NotNull] ReusableTween tween) {
            // Debug.Log($"sequence {id} releaseTween {tween.id}");
            Assert.AreNotEqual(0, tween.sequence.root.id);
            tween.next = default;
            tween.prev = default;
            tween.prevSibling = default;
            tween.nextSibling = default;
            tween.sequence = default;
            if (tween.isSequenceRoot()) {
                tween.tweenType = TweenType.None;
                Assert.IsFalse(tween.isSequenceRoot());
            }
        }
        
        internal SequenceChildrenEnumerator getAllChildren() {
            var enumerator = getAllTweens();
            var movedNext = enumerator.MoveNext(); // skip self
            Assert.IsTrue(movedNext);
            Assert.AreEqual(root, enumerator.Current);
            return enumerator;
        }

        /// <summary>Stops the sequence when it reaches the 'end' or returns to the 'start' for the next time.<br/>
        /// For example, if you have an infinite sequence (cycles == -1) with CycleMode.Yoyo/Rewind, and you wish to stop it when it reaches the 'end', then set <see cref="stopAtEndValue"/> to true.
        /// To stop the animation at the 'beginning', set <see cref="stopAtEndValue"/> to false.</summary>
        public void SetRemainingCycles(bool stopAtEndValue) {
            root.SetRemainingCycles(stopAtEndValue);
        }

        /// <summary>Sets the number of remaining cycles.<br/>
        /// This method modifies the <see cref="cyclesTotal"/> so that the sequence will complete after the number of <see cref="cycles"/>.<br/>
        /// To set the initial number of cycles, use Sequence.Create(cycles: numCycles) instead.<br/><br/>
        /// Setting cycles to -1 will repeat the sequence indefinitely.<br/>
        /// </summary>
        public void SetRemainingCycles(int cycles) {
            root.SetRemainingCycles(cycles);
        }

        public bool isPaused {
            get => root.isPaused;
            set => root.isPaused = value;
        }

        internal SequenceDirectEnumerator getSelfChildren(bool isForward = true) => new SequenceDirectEnumerator(this, isForward);
        internal SequenceChildrenEnumerator getAllTweens() => new SequenceChildrenEnumerator(this);

        public override string ToString() => root.ToString();

        internal struct SequenceDirectEnumerator {
            readonly Sequence sequence;
            Tween current;
            readonly bool isEmpty;
            readonly bool isForward;
            bool isStarted;
  
            internal SequenceDirectEnumerator(Sequence s, bool isForward) {
                Assert.IsTrue(s.isAlive, s.id);
                sequence = s;
                this.isForward = isForward;
                isStarted = false;
                isEmpty = isSequenceEmpty(s);
                if (isEmpty) {
                    current = default;
                    return;
                }
                current = sequence.root.tween.next;
                Assert.IsTrue(current.IsCreated && current.id != sequence.root.tween.nextSibling.id);
                if (!isForward) {
                    while (true) {
                        var next = current.tween.nextSibling;
                        if (!next.IsCreated) {
                            break;
                        }
                        current = next;
                    }
                }
                Assert.IsTrue(current.IsCreated);
            }

            static bool isSequenceEmpty(Sequence s) {
                // tests: SequenceNestingDifferentSettings(), TestSequenceEnumeratorWithEmptySequences()
                return s.root.tween.intParam == emptySequenceTag;
            }
            
            public 
                #if UNITY_2020_2_OR_NEWER
                readonly
                #endif
                SequenceDirectEnumerator GetEnumerator() {
                Assert.IsTrue(sequence.isAlive);
                return this;
            }

            public 
                #if UNITY_2020_2_OR_NEWER
                readonly
                #endif
                Tween Current {
                get {
                    Assert.IsTrue(sequence.isAlive);
                    Assert.IsTrue(current.IsCreated);
                    Assert.IsNotNull(current.tween);
                    Assert.AreEqual(current.id, current.tween.id);
                    Assert.IsTrue(current.tween.sequence.IsCreated);
                    return current;
                }
            }

            public bool MoveNext() {
                if (isEmpty) {
                    return false;
                }
                Assert.IsTrue(current.isAlive);
                if (!isStarted) {
                    isStarted = true;
                    return true;
                }
                current = isForward ? current.tween.nextSibling : current.tween.prevSibling;
                return current.IsCreated;
            }
        }

        internal struct SequenceChildrenEnumerator {
            readonly Sequence sequence;
            Tween current;
            bool isStarted;

            internal SequenceChildrenEnumerator(Sequence s) {
                Assert.IsTrue(s.isAlive);
                Assert.IsTrue(s.root.tween.isMainSequenceRoot());
                sequence = s;
                current = default;
                isStarted = false;
            }

            public 
                #if UNITY_2020_2_OR_NEWER
                readonly
                #endif
                SequenceChildrenEnumerator GetEnumerator() {
                Assert.IsTrue(sequence.isAlive);
                return this;
            }

            public 
                #if UNITY_2020_2_OR_NEWER
                readonly
                #endif
                Tween Current {
                get {
                    Assert.IsTrue(current.IsCreated);
                    Assert.IsNotNull(current.tween);
                    Assert.AreEqual(current.id, current.tween.id);
                    Assert.IsTrue(current.tween.sequence.IsCreated);
                    return current;
                }
            }

            public bool MoveNext() {
                if (!isStarted) {
                    Assert.IsFalse(current.IsCreated);
                    current = sequence.root;
                    isStarted = true;
                    return true;
                }
                Assert.IsTrue(current.isAlive);
                current = current.tween.next;
                return current.IsCreated;
            }
        }

        /// <summary>Schedules <paramref name="sequence"/> after all tweens and sequences in this Sequence.</summary>
        public Sequence Chain(Sequence sequence) {
            if (tryManipulate()) {
                Insert(duration, sequence);
            }
            return this;
        }

        /// <summary>Groups <paramref name="sequence"/> with the 'previous' animation (tween or sequence) in this Sequence.<br/>
        /// Grouped animations start at the same time and run in parallel.<br/>
        /// If the 'previous' operation is Chain(), then <paramref name="sequence"/> will start at the same time with the 'previously' chained animation.</summary>
        public Sequence Group(Sequence sequence) {
            if (tryManipulate()) {
                Insert(getLastInSelfOrRoot().tween.waitDelay, sequence);
            }
            return this;
        }

        /// <summary>Places <paramref name="sequence"/> inside this Sequence at time <paramref name="atTime"/>, leaving other animations unchanged.<br/>
        /// Sequence's duration will be increased if the inserted <paramref name="sequence"/> doesn't fit inside the current duration.</summary>
        public Sequence Insert(float atTime, Sequence sequence) {
            if (!ValidateCanAdd(sequence.root)) {
                return this;
            }
            
            ref var otherTweenType = ref sequence.root.tween.tweenType;
            if (otherTweenType != TweenType.MainSequence) {
                Debug.LogError(Constants.nestSequenceTwiceError);
                return this;
            }
            otherTweenType = TweenType.NestedSequence;

            Insert_internal(atTime, sequence.root);
            validateSequenceEnumerator();
            return this;
        }

        /// <summary>Custom timeScale. To smoothly animate timeScale over time, use <see cref="Tween.TweenTimeScale"/> method.</summary>
        public float timeScale {
            get => root.timeScale;
            set => root.timeScale = value;
        }

        [System.Diagnostics.Conditional("SAFETY_CHECKS")]
        void validateSequenceEnumerator() {
            var buffer = new List<ReusableTween> {
                root.tween
            };
            foreach (var t in getAllTweens()) {
                // Debug.Log($"----- {t}");
                if (t.tween.isSequenceRoot()) {
                    foreach (var ch in t.tween.sequence.getSelfChildren()) {
                        // Debug.Log(ch);
                        buffer.Add(ch.tween);
                    }
                }
            }
            if (buffer.Count != buffer.Select(_ => _.id).Distinct().Count()) {
                Debug.LogError($"{root.id}, duplicates in validateSequenceEnumerator():\n{string.Join("\n", buffer)}");
            }
        }

        public Sequence OnComplete(Action onComplete, bool warnIfTargetDestroyed = true) {
            root.OnComplete(onComplete, warnIfTargetDestroyed);
            return this;
        }

        public Sequence OnComplete<T>(T target, Action<T> onComplete, bool warnIfTargetDestroyed = true) where T : class {
            root.OnComplete(target, onComplete, warnIfTargetDestroyed);
            return this;
        }
    }
}