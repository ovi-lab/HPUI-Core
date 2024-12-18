using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EstimateConeRayAngles), true)]
    public class EstimateConeRayAnglesEditor: UnityEditor.Editor
    {
        private enum State { Wait, Started, Processing, Processed }

        private static readonly string[] excludedSerializedNames = new string[]{ "generatedConeRayAngles", "interactableToSegmentMapping" };
        private EstimateConeRayAngles t;
        private State state = State.Wait;
        private SerializedObject generatedConeRayAnglesObj;
        private SerializedProperty mappingProp;

        private HPUIInteractorConeRayAngles angles;
        private string saveName = "Assets/NewHPUIInteractorConeRayAngles.asset";
        private bool estimatedResultsFoldout = false;
        private List<HPUIInteractorConeRayAngleSegment> availableSegments = new(),
            allSegments;

        protected void OnEnable()
        {
            t = target as EstimateConeRayAngles;
            mappingProp = serializedObject.FindProperty("interactableToSegmentMapping");
            allSegments = Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).OfType<HPUIInteractorConeRayAngleSegment>().ToList();
        }

        public override void OnInspectorGUI()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
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

            EditorGUILayout.Space();
            IEnumerable<HPUIInteractorConeRayAngleSegment> availableSegments = t.InteractableToSegmentMapping.Select(el => el.segment);
            IEnumerable<HPUIInteractorConeRayAngleSegment> missingSegments = allSegments.Where(el => !availableSegments.Contains(el));

            if (missingSegments.Count() != 0)
            {
                EditorGUILayout.HelpBox($"Segments missing ({missingSegments.Count()}): {string.Join(", ", missingSegments)}", MessageType.Error);
            }

            EditorGUILayout.PropertyField(mappingProp);

            EditorGUILayout.Space();
            if (missingSegments.Count() == 0)
            {
                EditorGUILayout.LabelField("Editor only functions (play mode)", EditorStyles.boldLabel);

                GUI.enabled = EditorApplication.isPlaying;
                if ((state == State.Wait || state == State.Processed) &&
                    GUILayout.Button(new GUIContent((state == State.Processed ? "Restart": "Start") + " data collection", "Sets up the intertactables to collect data necessary for estimation.")))
                {
                    if (state == State.Wait || EditorUtility.DisplayDialog("Restart data collection", "Restarting data collection will discard previous data. Continue?", "Yes", "No"))
                    {
                        angles = null;
                        state = State.Started;
                        t.StartDataCollection();
                    }
                }

                if (state == State.Started && GUILayout.Button(new GUIContent("Finish data collection and estimate", "Finish data collection and start estimation of cones.")))
                {
                    state = State.Processing;
                    t.FinishAndEstimate((angles) =>
                    {
                        state = State.Processed;
                        this.angles = angles;
                    });
                }

                if (state == State.Processing)
                {
                    EditorGUILayout.LabelField("Processing new cone ray angles...");
                }

                if (angles != null)
                {
                    generatedConeRayAnglesObj = new SerializedObject(angles);
                    bool guiState = GUI.enabled;
                    GUI.enabled = false;

                    estimatedResultsFoldout = EditorGUILayout.Foldout(estimatedResultsFoldout, "Estimated data (preview)");
                    if (estimatedResultsFoldout)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("IndexDistalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("IndexIntermediateAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("IndexProximalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("MiddleDistalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("MiddleIntermediateAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("MiddleProximalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("RingDistalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("RingIntermediateAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("RingProximalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("LittleDistalAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("LittleIntermediateAngles"));
                            EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("LittleProximalAngles"));
                        }
                    }
                    GUI.enabled = guiState;

                    if (!saveName.StartsWith("Assets/"))
                    {
                        EditorGUILayout.HelpBox("Save name not in Assets folder", MessageType.Warning);
                    }

                    saveName = EditorGUILayout.TextField("Save name", saveName);

                    if (GUILayout.Button(new GUIContent("Save", "Save the asset in the above location.")))
                    {
                        AssetDatabase.CreateAsset(angles, saveName);
                        AssetDatabase.SaveAssets();
                    }
                }
                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ConeRayAnglesEstimationPair), true)]
    public class ConeRayAnglesEstimationPairPropertyDrawer: PropertyDrawer
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
