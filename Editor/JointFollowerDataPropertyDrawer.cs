using UnityEditor;
using ubco.ovilab.HPUI.Tracking;
using UnityEngine;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomPropertyDrawer(typeof(JointFollowerData))]
    public class JointFollowerDataPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 8 + EditorGUIUtility.standardVerticalSpacing * 9;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float itemHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.indentLevel++;

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("handedness"));
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("jointID"));
            SerializedProperty useSecondJointIDProp = property.FindPropertyRelative("useSecondJointID");
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, useSecondJointIDProp);
            bool guiEnabled = GUI.enabled;
            GUI.enabled = useSecondJointIDProp.boolValue;
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("secondJointID"));
            GUI.enabled = guiEnabled;
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("defaultJointRadius"));
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("offsetAngle"));
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("offsetAsRatioToRadius"));
            rect.y += itemHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("longitudinalOffset"));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
