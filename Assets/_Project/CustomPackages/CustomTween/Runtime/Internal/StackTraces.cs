#if PRIME_TWEEN_SAFETY_CHECKS && UNITY_ASSERTIONS && UNITY_2019_4_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace CustomTween {
    internal static class StackTraces {
        static readonly List<int> idToHash = new List<int>(1000);
        static readonly Dictionary<int, List<byte[]>> hashToTraces = new Dictionary<int, List<byte[]>>(100);
        
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/6230ef8f9bed142ddf6a5e338d6e0faf3368d313/Runtime/Export/Scripting/StackTrace.cs#L31-L47
        internal static unsafe void Record(int id) {
            if (id == 1) {
                idToHash.Clear();
                idToHash.Add(0);
            }
            const int maxLength = 16 * 1024;
            byte* buf = stackalloc byte[maxLength];
            int len = Debug.ExtractStackTraceNoAlloc(buf, maxLength, Application.dataPath);
            Assert.IsTrue(len > 0);
            int hash = ComputeHash(buf, len);
            Assert.AreEqual(id, idToHash.Count);
            idToHash.Add(hash);
            if (hashToTraces.TryGetValue(hash, out var traces)) {
                if (!Contains(traces, buf, len)) {
                    traces.Add(BufToArray());
                }
            } else {
                hashToTraces.Add(hash, new List<byte[]> { BufToArray() });
            }
            
            byte[] BufToArray() {
                var result = new byte[len];
                for (int i = 0; i < len; i++) {
                    result[i] = buf[i];
                }
                return result;
            }
        }

        static unsafe bool Contains([NotNull] List<byte[]> arrays, byte* data, int length) {
            foreach (var arr in arrays) {
                if (arr.Length == length) {
                    if (SequenceEqual()) {
                        return true;
                    }

                    bool SequenceEqual() {
                        for (int i = 0; i < length; i++) {
                            if (arr[i] != data[i]) {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        
        /// https://stackoverflow.com/a/468084
        static unsafe int ComputeHash(byte* data, int length) {
            unchecked {
                const int p = 16777619;
                int hash = (int)2166136261;
                for (int i = 0; i < length; i++) {
                    hash = (hash ^ data[i]) * p;
                }
                return hash;
            }
        }

        [NotNull]
        internal static string Get(int id) {
            Assert.IsTrue(id < idToHash.Count);
            var isSuccess = hashToTraces.TryGetValue(idToHash[id], out var traces);
            Assert.IsTrue(isSuccess);
            Assert.IsNotNull(traces);
            return string.Join("\n\n", traces.Select(bytes => {
                string str = Encoding.UTF8.GetString(bytes);
                Assert.IsFalse(string.IsNullOrEmpty(str));
                int i = 0;
                while (true) {
                    var prev = i;
                    i = str.IndexOf('\n', i);
                    if (i == -1) {
                        return str;
                    }
                    i++;
                    int j = str.IndexOf("CustomTween.", i, StringComparison.Ordinal);
                    if (j != i) {
                        return str.Substring(prev);
                    }
                }
            }));
        }

        internal static void logDebugInfo() {
            Debug.Log($"idToHash.Count: {idToHash.Count}, hashToTraces.Count: {hashToTraces.Count}");
        }
    }
}
#endif