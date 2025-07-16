using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomPropertyDrawer(typeof(StatisticalConeRaySegmentComputation))]
    public class StatisticalConeRaySegmentComputationDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 4;

            // Show percentile only if Estimate is Percentile
            SerializedProperty estimateProp = property.FindPropertyRelative("estimate");
            if ((StatisticalConeRaySegmentComputation.Estimate)estimateProp.enumValueIndex
                 == StatisticalConeRaySegmentComputation.Estimate.Percentile)
            {
                lines++; // show percentile
            }
            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines-1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty estimateProp = property.FindPropertyRelative("estimate");
            SerializedProperty minRayInteractionsThresholdProp = property.FindPropertyRelative("minRayInteractionsThreshold");
            SerializedProperty percentileProp = property.FindPropertyRelative("percentile");
            SerializedProperty multiplierProp = property.FindPropertyRelative("multiplier");

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(lineRect, estimateProp);

            if ((StatisticalConeRaySegmentComputation.Estimate)estimateProp.enumValueIndex
                == StatisticalConeRaySegmentComputation.Estimate.Percentile)
            {
                lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(lineRect, percentileProp);
            }

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(lineRect, minRayInteractionsThresholdProp);

            lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(lineRect, multiplierProp);

            EditorGUI.EndProperty();
        }
    }
}
