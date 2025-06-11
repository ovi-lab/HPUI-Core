using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(OnGestureDataCollector), true)]
    public class OnGestureDataCollectorEditor : UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[] { "interactableToSegmentMapping",
                                                                                  "xrHandTrackingEventsForConeDetection",
                                                                                  "ignoreMissingSegments"};
        private OnGestureDataCollector t;
        private SerializedProperty mappingProp,
            ignoreMissingSegmentsProp;

        private List<HPUIInteractorConeRayAngleSegment> allSegments;

        protected void OnEnable()
        {
            t = target as OnGestureDataCollector;
            mappingProp = serializedObject.FindProperty("interactableToSegmentMapping");
            ignoreMissingSegmentsProp = serializedObject.FindProperty("ignoreMissingSegments");
            allSegments = Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).OfType<HPUIInteractorConeRayAngleSegment>().ToList();
        }

        public override void OnInspectorGUI()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            EditorGUI.BeginChangeCheck();
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!excludedSerializedNames.Contains(iterator.name))
                {
                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
            }

            EditorGUILayout.PropertyField(ignoreMissingSegmentsProp);
            IEnumerable<HPUIInteractorConeRayAngleSegment> availableSegments = t.InteractableToSegmentMapping.Select(el => el.segment);
            IEnumerable<HPUIInteractorConeRayAngleSegment> missingSegments = allSegments.Where(el => !availableSegments.Contains(el));

            if (missingSegments.Count() != 0)
            {
                if (ignoreMissingSegmentsProp.boolValue)
                {
                    EditorGUILayout.HelpBox($"Segments ignored ({missingSegments.Count()}): {string.Join(", ", missingSegments)}", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Segments missing ({missingSegments.Count()}): {string.Join(", ", missingSegments)}", MessageType.Error);
                }
            }

            EditorGUILayout.PropertyField(mappingProp);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(OnGestureDataCollector.ConeRayAnglesEstimationPair), true)]
    public class ConeRayAnglesEstimationPairPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty interactableProp = property.FindPropertyRelative("interactable");
            SerializedProperty segmentProp = property.FindPropertyRelative("segment");

            float width = EditorGUIUtility.currentViewWidth;
            float segmentHeight = EditorGUIUtility.singleLineHeight;

            // Calculate rects
            Rect interactableRect = new Rect(position.x, position.y, position.width * 0.49f, segmentHeight);
            Rect taregetRect = new Rect(position.x + position.width * 0.51f, position.y, position.width * 0.49f, segmentHeight);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(interactableRect, interactableProp, GUIContent.none);
            EditorGUI.PropertyField(taregetRect, segmentProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
