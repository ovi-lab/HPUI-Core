using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(DeformableSurfaceKeypoint), true)]
    public class DeformableSurfaceKeypointPropertyDrawer: PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetTargetAndTypeProps(property, out SerializedProperty typeProp, out SerializedProperty targetProp);

            float height = EditorGUI.GetPropertyHeight(targetProp, true);
            return height + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            GetTargetAndTypeProps(property, out SerializedProperty typeProp, out SerializedProperty targetProp);

            // Draw label
            label.text += ":";
            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent + 1;

            float width = EditorGUIUtility.currentViewWidth;
            float targetHeight = EditorGUI.GetPropertyHeight(targetProp, true);

            // Calculate rects
            float heightOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect typeRect = new Rect(position.x, heightOffset, position.width * 0.33f, position.height);
            Rect taregetRect = new Rect(position.x + position.width * 0.33f, heightOffset, position.width * 0.67f, targetHeight);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            EditorGUI.PropertyField(taregetRect, targetProp, GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        private void GetTargetAndTypeProps(SerializedProperty property, out SerializedProperty typeProp, out SerializedProperty targetProp)
        {
            typeProp = property.FindPropertyRelative("keypointType");

            switch((DeformableSurfaceKeypoint.KeypointsOptions)typeProp.enumValueFlag)
            {
                case(DeformableSurfaceKeypoint.KeypointsOptions.JointID):
                    targetProp = property.FindPropertyRelative("jointID");
                    break;
                case(DeformableSurfaceKeypoint.KeypointsOptions.JointFollowerData):
                    targetProp = property.FindPropertyRelative("jointFollowerData");
                    break;
                case(DeformableSurfaceKeypoint.KeypointsOptions.Transform):
                    targetProp = property.FindPropertyRelative("jointTransform");
                    break;
                default:
                    throw new System.Exception();
            }

        }
    }
}
