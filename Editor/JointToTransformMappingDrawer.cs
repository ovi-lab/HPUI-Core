using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Tracking;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(JointToTransformMapping), true)]
    public class JointToTransformMappingPropertyDrawer: PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty typeProp = property.FindPropertyRelative("xrHandJointID");
            SerializedProperty targetProp = property.FindPropertyRelative("jointTransform");

            float width = EditorGUIUtility.currentViewWidth;
            float targetHeight = EditorGUIUtility.singleLineHeight;

            // Calculate rects
            Rect typeRect = new Rect(position.x, position.y, position.width * 0.33f, targetHeight);
            Rect taregetRect = new Rect(position.x + position.width * 0.35f, position.y, position.width * 0.65f, targetHeight);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            EditorGUI.PropertyField(taregetRect, targetProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
