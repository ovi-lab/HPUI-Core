using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.Components;
using System;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GuidedConeRayEstimatorComponent), true)]
    public class GuidedConeRayEstimatorComponentEditor : UnityEditor.Editor
    {
        private enum State { Wait, Started, Processing, Processed }
        private class StateInformation
        {
            public State state = State.Wait;
            public HPUIInteractorConeRayAngles generatedAsset, savedAsset;
            public StateInformation(State state = State.Wait, HPUIInteractorConeRayAngles generatedAsset = null, HPUIInteractorConeRayAngles savedAsset = null)
            {
                this.state = state;
                this.generatedAsset = generatedAsset;
                this.savedAsset = savedAsset;
            }
        }

        private SerializedObject generatedConeRayAnglesObj;
        private string collectDataButtonString = "Start Data Collection";
        private bool isCollectingData = false, showXRHandtrackingEventsMissingMessage;
        private GuidedConeRayEstimatorComponent t;
        private bool hasSomeDataBeenCollected = false;
        private StateInformation stateInfo;

        protected void OnEnable()
        {
            t = target as GuidedConeRayEstimatorComponent;
            stateInfo = new StateInformation();
        }

        protected void CheckIfShowXRHandtrackingEventsMissingMessage()
        {
            showXRHandtrackingEventsMissingMessage = t.SetDetectionLogicOnEstimation && t.XRHandTrackingEventsForConeDetection == null && t.Interactor.GetComponent<XRHandTrackingEvents>() == null;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CheckIfShowXRHandtrackingEventsMissingMessage();

            if (showXRHandtrackingEventsMissingMessage)
            {
                EditorGUILayout.HelpBox("While SetDetectionLogicOnEstimation is set, XRHandTrackingEventsForConeDetection is null and Interactor doesn't have an XRHandTrackingEvents component.", MessageType.Error);
            }

            EditorGUILayout.Space();
            GUI.enabled = !showXRHandtrackingEventsMissingMessage && Application.isPlaying;

            if (GUILayout.Button(collectDataButtonString))
            {
                if (!isCollectingData) // start data collection
                {
                    t.StartDataCollectionForPhalange();
                    collectDataButtonString = "Stop Data Collection";
                    stateInfo.state = State.Started;
                }
                else // stop data collection
                {
                    t.EndDataCollectionForPhalange();
                    collectDataButtonString = "Start Data Collection";

                    // cycling strategy to automatically move to the next phalange.
                    // saves some headache when running a calibration protocol.
                    int phalangeCount = Enum.GetNames(typeof(HPUIInteractorConeRayAngleSegment)).Length;
                    int currentPhalangeIndex = Array.IndexOf(Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)), t.TargetSegment);
                    if (currentPhalangeIndex < phalangeCount - 1) t.TargetSegment = (HPUIInteractorConeRayAngleSegment)currentPhalangeIndex + 1;
                    else t.TargetSegment = (HPUIInteractorConeRayAngleSegment)0;

                    hasSomeDataBeenCollected = true;
                }
                isCollectingData = !isCollectingData;
            }

            EditorGUILayout.Space();
            GUI.enabled = hasSomeDataBeenCollected;
            if (GUILayout.Button("Process Data"))
            {
                t.FinishAndEstimate((angles) =>
                {

                    stateInfo.state = State.Processing;
                    t.FinishAndEstimate((angles) =>
                    {
                        stateInfo.state = State.Processed;
                        this.stateInfo.generatedAsset = angles;
                    });
                });
            }

            EditorGUILayout.Space();
            if (stateInfo.state == State.Processing)
            {
                EditorGUILayout.LabelField("Processing new cone ray angles...");
            }

            if (stateInfo.state == State.Processed)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Save Data"))
                {

                    if (stateInfo.generatedAsset != null)
                    {
                        generatedConeRayAnglesObj = new SerializedObject(stateInfo.generatedAsset);
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
}
