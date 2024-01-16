using UnityEditor;
using ubco.ovilab.HPUI.Tracking;
using UnityEngine;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomPropertyDrawer(typeof(JointFollowerData))]
    public class JointFollowerDataPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(property.FindPropertyRelative("handedness"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("jointID"));
            SerializedProperty useSecondJointIDProp = property.FindPropertyRelative("useSecondJointID");
            EditorGUILayout.PropertyField(useSecondJointIDProp);
            bool guiEnabled = GUI.enabled;
            GUI.enabled = useSecondJointIDProp.boolValue;
            EditorGUILayout.PropertyField(property.FindPropertyRelative("secondJointID"));
            GUI.enabled = guiEnabled;
            EditorGUILayout.PropertyField(property.FindPropertyRelative("defaultJointRadius"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("offsetAngle"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("offsetAsRatioToRadius"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("longitudinalOffset"));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
