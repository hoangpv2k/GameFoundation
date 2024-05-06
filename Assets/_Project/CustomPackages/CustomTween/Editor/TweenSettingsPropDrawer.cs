using JetBrains.Annotations;
using CustomTween;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUI;
using static UnityEditor.EditorGUIUtility;

[CustomPropertyDrawer(typeof(TweenSettings))]
internal class TweenSettingsPropDrawer : PropertyDrawer {
    public override float GetPropertyHeight([NotNull] SerializedProperty property, GUIContent label) {
        if (!property.isExpanded) {
            return singleLineHeight;
        }
        return getPropHeight(property);
    }

    internal static float getPropHeight([NotNull] SerializedProperty property) {
        var count = 1;
        count++; // duration
        count++; // ease
        var easeIndex = property.FindPropertyRelative(nameof(TweenSettings.ease)).intValue;
        if (easeIndex == (int)Ease.Custom) {
            count++; // customEase
        }
        count++; // cycles
        var cycles = property.FindPropertyRelative(nameof(TweenSettings.cycles)).intValue;
        if (cycles != 0 && cycles != 1) {
            count++; // cycleMode
        }
        count++; // startDelay
        count++; // endDelay
        count++; // useUnscaledTime
        count++; // useFixedUpdate
        var result = singleLineHeight * count + standardVerticalSpacing * (count - 1);
        result += standardVerticalSpacing * 2; // extra spacing
        return result;
    }

    public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label) {
        var rect = new Rect(position) { height = singleLineHeight };
        PropertyField(rect, property, label);
        if (!property.isExpanded) {
            return;
        }
        moveToNextLine(ref rect);
        indentLevel++;
        { // duration
            property.NextVisible(true);
            DrawDuration(rect, property);
            moveToNextLine(ref rect);
        }
        drawEaseTillNext(property, ref rect);
        indentLevel--;
    }

    internal static void DrawDuration(Rect rect, [NotNull] SerializedProperty property) {
        if (GUI.enabled) {
            if (property.floatValue == 0f) {
                property.floatValue = 1f;
            } else if (property.floatValue < 0) {
                property.floatValue = 0.01f;
            }
        }
        PropertyField(rect, property);
    }

    internal static void drawEaseTillNext([NotNull] SerializedProperty property, ref Rect rect) {
        { // ease
            property.NextVisible(true);
            PropertyField(rect, property);
            moveToNextLine(ref rect);
            // customEase
            bool isCustom = property.intValue == (int) Ease.Custom;
            property.NextVisible(true);
            if (isCustom) {
                PropertyField(rect, property);
                moveToNextLine(ref rect);
            }
        }
        rect.y += standardVerticalSpacing * 2;
        { // cycles
            var cycles = drawCycles(rect, property);
            moveToNextLine(ref rect);
            {
                // cycleMode
                property.NextVisible(true);
                if (cycles != 0 && cycles != 1) {
                    PropertyField(rect, property);
                    moveToNextLine(ref rect);
                }
            }
        }
        drawStartDelayTillEnd(ref rect, property);
    }

    internal static void drawStartDelayTillEnd(ref Rect rect, [NotNull] SerializedProperty property) {
        { // startDelay, endDelay
            for (int _ = 0; _ < 2; _++) {
                property.NextVisible(true);
                if (property.floatValue < 0f) {
                    property.floatValue = 0f;
                }
                PropertyField(rect, property);
                moveToNextLine(ref rect);
            }
        }
        { // useUnscaledTime
            property.NextVisible(true);
            PropertyField(rect, property);
            moveToNextLine(ref rect);
        }
        { // useFixedUpdate
            property.NextVisible(true);
            PropertyField(rect, property);
            moveToNextLine(ref rect);
        }
    }

    internal static int drawCycles(Rect rect, [NotNull] SerializedProperty property) {
        property.NextVisible(false);
        if (property.intValue == 0) {
            property.intValue = 1;
        } else if (property.intValue < -1) {
            property.intValue = -1;
        }
        PropertyField(rect, property);
        return property.intValue;
    }

    static void moveToNextLine(ref Rect rect) {
        rect.y += singleLineHeight + standardVerticalSpacing;
    }
}