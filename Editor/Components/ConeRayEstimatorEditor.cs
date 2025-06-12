using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ConeRayEstimator), true)]
    public class ConeRayEstimatorEditor : UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[] {
            "xrHandTrackingEventsForConeDetection",
            "generatedAsset" // This is handled in DrawGenerationAndSaveOptions
        };
        private const string DONT_ASK_EDITORPREF_KEY = "ubco.ovilab.HPUI.Components.ConeEsimation.DontAskWhenRestarting";
        private static Dictionary<ConeRayEstimator, HPUIInteractorConeRayAngles> savedAssets = new();
        private ConeRayEstimator t;
        private SerializedObject generatedConeRayAnglesObj;
        private SerializedProperty xrHandTrackingEventsForConeDetectionProp,
            generatedAssetProp;

        private bool estimatedResultsFoldout = false,
            dontAskBeforeDiscard,
            showXRHandtrackingEventsMissingMessage;

        protected void OnEnable()
        {
            t = target as ConeRayEstimator;
            xrHandTrackingEventsForConeDetectionProp = serializedObject.FindProperty("xrHandTrackingEventsForConeDetection");
            generatedAssetProp = serializedObject.FindProperty("generatedAsset");
            dontAskBeforeDiscard = EditorPrefs.GetBool(DONT_ASK_EDITORPREF_KEY, false);
            CheckIfShowXRHandtrackingEventsMissingMessage();
        }

        protected void CheckIfShowXRHandtrackingEventsMissingMessage()
        {
            showXRHandtrackingEventsMissingMessage = (t.SetDetectionLogicOnEstimation &&
                                                      t.XRHandTrackingEventsForConeDetection == null &&
                                                      (t.DataCollector == null ||
                                                       t.DataCollector.Interactor == null ||
                                                       t.DataCollector.Interactor.GetComponent<XRHandTrackingEvents>() == null));
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            DrawRemainingInspector();
            EditorGUILayout.Space();
            DrawGenerationAndSaveOptions();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw fields not in <see cref="excludedSerializedNames"/> and the
        /// XRHandTrackingEvents property field.
        /// This method is to be called in <see cref="OnInspectorGUI"/>.
        /// </summary>
        protected void DrawRemainingInspector()
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

            EditorGUILayout.PropertyField(xrHandTrackingEventsForConeDetectionProp, new GUIContent("XRHandTrackingEvents For Cone Detection"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                CheckIfShowXRHandtrackingEventsMissingMessage();
            }

            if (showXRHandtrackingEventsMissingMessage)
            {
                EditorGUILayout.HelpBox("While SetDetectionLogicOnEstimation is set, XRHandTrackingEventsForConeDetection is null and Interactor doesn't have an XRHandTrackingEvents component.", MessageType.Error);
            }
        }

        /// <summary>
        /// Draw UI related to generating and saving assets in editor.
        /// This method is to be called in <see cref="OnInspectorGUI"/>.
        /// </summary>
        protected void DrawGenerationAndSaveOptions()
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

            if (t.ConeRaySegmentComputation == null)
            {
                EditorGUILayout.HelpBox("ConeRaySegmentComputation not configured", MessageType.Error);
            }

            if (t.DataCollector == null)
            {
                EditorGUILayout.HelpBox("DataCollector not configured", MessageType.Error);
            }
            else if (t.DataCollector.Interactor == null)
            {
                EditorGUILayout.HelpBox("Interactor in DataCollector not configured", MessageType.Error);

            }

            GUI.enabled = EditorApplication.isPlaying &&
                t.ConeRaySegmentComputation != null &&
                t.DataCollector != null &&
                t.DataCollector.Interactor != null;
            if ((t.CurrentState == ConeRayEstimator.State.Ready) &&
                GUILayout.Button(new GUIContent((t.GeneratedAsset ? "Restart" : "Start") + " data collection", "Sets up the intertactables to collect data necessary for estimation.")))
                {
                    if (EditorUtility.DisplayDialog("Restart data collection",
                                                    "Restarting data collection will discard previous data. Continue?",
                                                    "Yes",
                                                    "No",
                                                    DialogOptOutDecisionType.ForThisMachine, DONT_ASK_EDITORPREF_KEY))
                    {
                        if (savedAssets.ContainsKey(t))
                        {
                            savedAssets.Remove(t);
                        }
                        generatedAssetProp.objectReferenceValue = null;
                        t.StartDataCollection();
                    }
                    dontAskBeforeDiscard = EditorPrefs.GetBool(DONT_ASK_EDITORPREF_KEY, false);
                }

            if (t.CurrentState == ConeRayEstimator.State.CollectingData && GUILayout.Button(new GUIContent("Finish data collection and estimate", "Finish data collection and start estimation of cones.")))
            {
                t.EndAndEstimate();
            }

            if (t.CurrentState == ConeRayEstimator.State.EstimatingConeRays)
            {
                EditorGUILayout.LabelField("Processing new cone ray angles...");
            }
            GUI.enabled = true;

            if (generatedAssetProp.objectReferenceValue != null)
            {
                bool guiState = GUI.enabled;
                GUI.enabled = false;

                estimatedResultsFoldout = EditorGUILayout.Foldout(estimatedResultsFoldout, "Estimated data (preview)");
                if (estimatedResultsFoldout)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        generatedConeRayAnglesObj = new SerializedObject(generatedAssetProp.objectReferenceValue);
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

                bool hasSavedAsset = savedAssets.TryGetValue(t, out HPUIInteractorConeRayAngles savedAsset);

                if (hasSavedAsset)
                {
                    EditorGUILayout.ObjectField("Saved asset", savedAsset, typeof(HPUIInteractorConeRayAngles), false);
                }

                GUI.enabled = guiState;

                if (t.GeneratedAsset != null && GUILayout.Button(new GUIContent("Save", "Save the asset.")))
                {
                    string saveName = EditorUtility.SaveFilePanelInProject("Save new cone angles asset", "NewHPUIInteractorConeRayAngles.asset", "asset", "Save location for the generated cone angle asset.");
                    try
                    {
                        AssetDatabase.CreateAsset(t.GeneratedAsset, saveName);
                        AssetDatabase.SaveAssets();
                    }
                    catch(Exception e)
                    {
                        Debug.LogError($"{e}");
                    }
                    if (hasSavedAsset)
                    {
                        savedAssets[t] = t.GeneratedAsset;
                    }
                    else
                    {
                        savedAssets.Add(t, t.GeneratedAsset);
                    }
                }
            }
        }
    }
}
