using System;
using UnityEditor;
using UnityEngine;
using ubco.ovi.HPUI;

namespace ubco.ovi.HPUI.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
    public class ConditionalFieldAttributeDrawer : PropertyDrawer
    {
        private bool toShow = true;
        private bool initialized;
        private PropertyDrawer customPropertyDrawer;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (toShow)
            {
                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalFieldAttribute labelAttribute = attribute as ConditionalFieldAttribute;
            position.height = EditorGUIUtility.singleLineHeight;
            toShow = EditorGUI.Toggle(position, labelAttribute.label, toShow);
            if (toShow)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                label.text = " ";
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
