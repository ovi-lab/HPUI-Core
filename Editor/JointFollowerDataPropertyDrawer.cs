using UnityEditor;
using ubco.ovilab.HPUI.Core.Tracking;
using UnityEngine;

namespace ubco.ovilab.HPUI.Core.Editor
{
    [CustomPropertyDrawer(typeof(JointFollowerData))]
    public class JointFollowerDataPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 9 + EditorGUIUtility.standardVerticalSpacing * 10;
        }

        // FIXME: This seems to ignore the foldout the DatumPropertyDrawer has
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float itemHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            EditorGUI.PrefixLabel(position, label);

            EditorGUI.indentLevel++;

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            rect.y += itemHeight;
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
