using System.Collections.Generic;
using JetBrains.Annotations;
using CustomTween;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomTweenManager))]
internal class CustomTweenManagerInspector : Editor {
    SerializedProperty tweensProp;
    SerializedProperty fixedUpdateTweensProp;
    GUIContent aliveTweenGuiContent;
    GUIContent fixedUpdateTweenGuiContent;
    StringCache tweensCountCache;
    StringCache maxSimultaneousTweensCountCache;
    StringCache currentPoolCapacityCache;

    void OnEnable() {
        tweensProp = serializedObject.FindProperty(nameof(CustomTweenManager.tweens));
        fixedUpdateTweensProp = serializedObject.FindProperty(nameof(CustomTweenManager.fixedUpdateTweens));
        Assert.IsNotNull(tweensProp);
        Assert.IsNotNull(fixedUpdateTweensProp);
        aliveTweenGuiContent = new GUIContent("Tweens");
        fixedUpdateTweenGuiContent = new GUIContent("Fixed update tweens");
    }

    public override void OnInspectorGUI() {
        using (new EditorGUI.DisabledScope(true)) {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoBehaviour), false);
        }
        
        var manager = target as CustomTweenManager;
        Assert.IsNotNull(manager);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Alive tweens", EditorStyles.label);
        GUILayout.Label(tweensCountCache.GetCachedString(manager.tweensCount), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label( Constants.maxAliveTweens, EditorStyles.label);
        GUILayout.Label(maxSimultaneousTweensCountCache.GetCachedString(manager.maxSimultaneousTweensCount), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tweens capacity", EditorStyles.label);
        GUILayout.Label(currentPoolCapacityCache.GetCachedString(manager.currentPoolCapacity), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Use " + Constants.setTweensCapacityMethod + " to set tweens capacity.\n" +
                                "To prevent memory allocations during runtime, choose the value that is greater than the maximum number of simultaneous tweens in your game.", MessageType.None);

        drawList(tweensProp, manager.tweens, aliveTweenGuiContent);
        drawList(fixedUpdateTweensProp, manager.fixedUpdateTweens, fixedUpdateTweenGuiContent);
        void drawList(SerializedProperty tweensProp, List<ReusableTween> list, GUIContent guiContent) {
            if (tweensProp.isExpanded) {
                foreach (var tween in list) {
                    if (tween != null && string.IsNullOrEmpty(tween.debugDescription)) {
                        tween.debugDescription = tween.GetDescription();
                    }
                }
            }
            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.PropertyField(tweensProp, guiContent);
            }
        }
    }

    struct StringCache {
        int currentValue;
        string str;

        [NotNull]
        internal string GetCachedString(int value) {
            if (currentValue != value || str == null) {
                currentValue = value;
                str = value.ToString();
            }
            return str;
        }
    }
}