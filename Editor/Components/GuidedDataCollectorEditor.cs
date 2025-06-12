using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GuidedDataCollector), true)]
    public class GuidedDataCollectorEditor : UnityEditor.Editor
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
        private GuidedDataCollector t;

        protected void OnEnable()
        {
            t = target as GuidedDataCollector;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUI.enabled = Application.isPlaying && t.CollectingData;

            if (GUILayout.Button("Collect data for next segment"))
            {
                t.EndDataCollectionForTargetSegment();
                // cycling strategy to automatically move to the next phalange.
                // saves some headache when running a calibration protocol.
                int phalangeCount = Enum.GetNames(typeof(HPUIInteractorConeRayAngleSegment)).Length;
                int currentPhalangeIndex = Array.IndexOf(Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)), t.TargetSegment);
                if (currentPhalangeIndex < phalangeCount - 1)
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)currentPhalangeIndex + 1;
                }
                else
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)0;
                }
            }

            serializedObject.ApplyModifiedProperties();

        }
    }
}
