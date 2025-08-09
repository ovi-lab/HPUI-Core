using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(HPUIConeRayCastDetectionLogic.ClosestJointAndSideEstimator), true)]
    public class HPUIConeRayCastDetectionLogic_ClosestJointAndSideEstimatorDrawer: PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty xrHandTrackingEventsProp = property.FindPropertyRelative("xrHandTrackingEvents");
            SerializedProperty xrOriginTransformProp = property.FindPropertyRelative("xrOriginTransform");

            float targetHeight = EditorGUIUtility.singleLineHeight;

            // Calculate rects
            Rect labelRect = new Rect(position.x, position.y, position.width, targetHeight);
            Rect xrHandTrackingEventsRect = new Rect(position.x + 10, position.y + EditorGUIUtility.standardVerticalSpacing + targetHeight, position.width - 10, targetHeight);
            Rect xrOriginTransformRect = new Rect(position.x + 10, position.y + EditorGUIUtility.standardVerticalSpacing * 2 + targetHeight * 2, position.width - 10, targetHeight);

            if (xrHandTrackingEventsProp == null || xrOriginTransformProp == null)
            {
                SubclassSelectorDrawer.SetManagedReference(property, typeof(HPUIConeRayCastDetectionLogic.ClosestJointAndSideEstimator));
            }
            else
            {
                // Draw fields
                EditorGUI.LabelField(labelRect, "ClosestJointAndSideEstimator fields:");
                EditorGUI.PropertyField(xrHandTrackingEventsRect, xrHandTrackingEventsProp);
                EditorGUI.PropertyField(xrOriginTransformRect, xrOriginTransformProp);
            }
            
            EditorGUI.EndProperty();
        }
    }
}
