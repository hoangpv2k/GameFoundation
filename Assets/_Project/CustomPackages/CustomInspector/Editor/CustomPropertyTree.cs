﻿using System;
using CustomInspector.Elements;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace CustomInspector
{
    public abstract class CustomPropertyTree : IDisposable
    {
        private CustomPropertyElement _rootPropertyElement;
        private Rect _cachedOuterRect = new Rect(0, 0, 0, 0);

        public CustomPropertyDefinition RootPropertyDefinition { get; protected set; }
        public CustomProperty RootProperty { get; protected set; }

        public Type TargetObjectType { get; protected set; }
        public int TargetsCount { get; protected set; }
        public bool TargetIsPersistent { get; protected set; }

        public bool ValidationRequired { get; private set; } = true;
        public bool RepaintRequired { get; private set; } = true;

        public int RepaintFrame { get; private set; } = 0;

        public virtual void Dispose()
        {
            if (_rootPropertyElement != null && _rootPropertyElement.IsAttached)
            {
                _rootPropertyElement.DetachInternal();
            }
        }

        public virtual void Update(bool forceUpdate = false)
        {
            RepaintFrame++;
        }

        public virtual bool ApplyChanges()
        {
            return false;
        }

        public void RunValidationIfRequired()
        {
            if (!ValidationRequired)
            {
                return;
            }

            RunValidation();
        }

        public void RunValidation()
        {
            ValidationRequired = false;

            Profiler.BeginSample("CustomInspector.RunValidation");
            RootProperty.RunValidation();
            Profiler.EndSample();

            RequestRepaint();
        }

        public virtual void Draw()
        {
            RepaintRequired = false;

            if (_rootPropertyElement == null)
            {
                _rootPropertyElement = new CustomPropertyElement(RootProperty, new CustomPropertyElement.Props
                {
                    forceInline = !RootProperty.TryGetMemberInfo(out _),
                });
                _rootPropertyElement.AttachInternal();
            }

            Profiler.BeginSample("CustomInspector.UpdateRootPropertyElement");
            _rootPropertyElement.Update();
            Profiler.EndSample();

            var rectOuter = GUILayoutUtility.GetRect(0, 9999, 0, 0);
            _cachedOuterRect = Event.current.type == EventType.Layout ? _cachedOuterRect : rectOuter;

            var rect = new Rect(_cachedOuterRect);
            rect = EditorGUI.IndentedRect(rect);
            rect.height = _rootPropertyElement.GetHeight(rect.width);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            GUILayoutUtility.GetRect(_cachedOuterRect.width, rect.height);

            Profiler.BeginSample("CustomInspector.DrawRootPropertyElement");
            _rootPropertyElement.OnGUI(rect);
            Profiler.EndSample();

            EditorGUI.indentLevel = oldIndent;
        }

        public void EnumerateValidationResults(Action<CustomProperty, CustomValidationResult> call)
        {
            RootProperty.EnumerateValidationResults(call);
        }

        public void RequestRepaint()
        {
            RepaintRequired = true;
        }

        public void RequestValidation()
        {
            ValidationRequired = true;

            RequestRepaint();
        }

        public abstract void ForceCreateUndoGroup();
    }
}