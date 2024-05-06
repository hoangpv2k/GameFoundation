﻿using CustomInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Elements
{
    internal class CustomFoldoutElement : CustomPropertyCollectionBaseElement
    {
        private readonly CustomProperty _property;

        public CustomFoldoutElement(CustomProperty property)
        {
            _property = property;

            DeclareGroups(property.ValueType);
        }

        public override bool Update()
        {
            var dirty = false;

            if (_property.IsExpanded)
            {
                dirty |= GenerateChildren();
            }
            else
            {
                dirty |= ClearChildren();
            }

            dirty |= base.Update();

            return dirty;
        }

        public override float GetHeight(float width)
        {
            var height = EditorGUIUtility.singleLineHeight;

            if (!_property.IsExpanded)
            {
                return height;
            }

            height += base.GetHeight(width);

            return height;
        }

        public override void OnGUI(Rect position)
        {
            var headerRect = new Rect(position)
            {
                height = EditorGUIUtility.singleLineHeight,
            };
            var contentRect = new Rect(position)
            {
                yMin = position.yMin + headerRect.height,
            };

            CustomEditorGUI.Foldout(headerRect, _property);

            if (!_property.IsExpanded)
            {
                return;
            }

            using (var indentedRectScope = CustomGuiHelper.PushIndentedRect(contentRect, 1))
            {
                base.OnGUI(indentedRectScope.IndentedRect);
            }
        }

        private bool GenerateChildren()
        {
            if (ChildrenCount != 0)
            {
                return false;
            }

            foreach (var childProperty in _property.ChildrenProperties)
            {
                AddProperty(childProperty);
            }

            return true;
        }

        private bool ClearChildren()
        {
            if (ChildrenCount == 0)
            {
                return false;
            }

            RemoveAllChildren();

            return true;
        }
    }
}