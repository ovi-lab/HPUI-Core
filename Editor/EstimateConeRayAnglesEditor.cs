using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.Components;
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
        private class StateInformation
        {
            public State state = State.Wait;
            public HPUIInteractorConeRayAngles generatedAsset, savedAsset;
        }

        private static readonly string[] excludedSerializedNames = new string[]{ "generatedConeRayAngles", "interactableToSegmentMapping" };
        private const string DONT_ASK_EDITORPREF_KEY = "ubco.ovilab.HPUI.Components.ConeEsimation.DontAskWhenRestarting";
        private static Dictionary<EstimateConeRayAngles, StateInformation> stateInfoStore = new();
        private EstimateConeRayAngles t;
        private SerializedObject generatedConeRayAnglesObj;
        private SerializedProperty mappingProp;

        private bool estimatedResultsFoldout = false,
            dontAskBeforeDiscard;
        private List<HPUIInteractorConeRayAngleSegment> availableSegments = new(),
            allSegments;
        private StateInformation stateInfo;

        protected void OnEnable()
        {
            t = target as EstimateConeRayAngles;
            mappingProp = serializedObject.FindProperty("interactableToSegmentMapping");
            allSegments = Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).OfType<HPUIInteractorConeRayAngleSegment>().ToList();
            dontAskBeforeDiscard = EditorPrefs.GetBool(DONT_ASK_EDITORPREF_KEY, false);
            if (!stateInfoStore.TryGetValue(t, out stateInfo))
            {
                stateInfo = new StateInformation();
                stateInfoStore.Add(t, stateInfo);
            }
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
                EditorGUILayout.LabelField("Editor inspector only functions (play mode)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("If not saved, the data will be discarded when exiting playmode, regardless of the `Always Ask Before Restarting` toggle.", MessageType.Info);

                using (EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope())
                {
                    // Doing the double negation because I am too lazy to maintain two flags just for this!
                    dontAskBeforeDiscard = !EditorGUILayout.Toggle(new GUIContent("Always Ask Before Restarting",
                                                                                  "Always ask before restarting data collection to avoid discarding data."),
                                                                   !dontAskBeforeDiscard);
                    if (check.changed)
                    {
                        EditorPrefs.SetBool(DONT_ASK_EDITORPREF_KEY, dontAskBeforeDiscard);
                    }
                }

                GUI.enabled = EditorApplication.isPlaying;
                if ((stateInfo.state == State.Wait || stateInfo.state == State.Processed) &&
                    GUILayout.Button(new GUIContent((stateInfo.state == State.Processed ? "Restart": "Start") + " data collection", "Sets up the intertactables to collect data necessary for estimation.")))
                {
                    if (stateInfo.state == State.Wait || EditorUtility.DisplayDialog("Restart data collection",
                                                                           "Restarting data collection will discard previous data. Continue?",
                                                                           "Yes",
                                                                           "No",
                                                                           DialogOptOutDecisionType.ForThisMachine, DONT_ASK_EDITORPREF_KEY))
                    {
                        stateInfo.savedAsset = null;
                        stateInfo.generatedAsset = null;
                        stateInfo.state = State.Started;
                        t.StartDataCollection();
                    }
                    dontAskBeforeDiscard = EditorPrefs.GetBool(DONT_ASK_EDITORPREF_KEY, false);
                }

                if (stateInfo.state == State.Started && GUILayout.Button(new GUIContent("Finish data collection and estimate", "Finish data collection and start estimation of cones.")))
                {
                    stateInfo.state = State.Processing;
                    t.FinishAndEstimate((angles) =>
                    {
                        stateInfo.state = State.Processed;
                        this.stateInfo.generatedAsset = angles;
                    });
                }

                if (stateInfo.state == State.Processing)
                {
                    EditorGUILayout.LabelField("Processing new cone ray angles...");
                }
                GUI.enabled = true;

                if (stateInfo.generatedAsset != null)
                {
                    generatedConeRayAnglesObj = new SerializedObject(stateInfo.generatedAsset);
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

                    if (stateInfo.savedAsset != null)
                    {
                        EditorGUILayout.ObjectField("Saved asset", stateInfo.savedAsset, typeof(HPUIInteractorConeRayAngles), false);
                    }

                    GUI.enabled = guiState;

                    if (stateInfo.savedAsset != null && GUILayout.Button(new GUIContent("Save", "Save the asset.")))
                    {
                        string saveName = EditorUtility.SaveFilePanelInProject("Save new cone angles asset", "NewHPUIInteractorConeRayAngles.asset", "asset", "Save location for the generated cone angle asset.");
                        AssetDatabase.CreateAsset(stateInfo.generatedAsset, saveName);
                        AssetDatabase.SaveAssets();
                        stateInfo.savedAsset = stateInfo.generatedAsset;
                    }
                }
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
