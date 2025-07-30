using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System;
using System.Collections.Generic;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GuidedDataCollector), true)]
    public class GuidedDataCollectorEditor : UnityEditor.Editor
    {
        private int currentPhalangeIndex = 0;

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
            currentPhalangeIndex = 0;
            StepToTargetEnum();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUI.enabled = Application.isPlaying && t.CollectingData;

            if (!t.PauseDataCollection)
            {
                if (GUILayout.Button("Pause Data Collection"))
                {
                    t.EndDataCollectionForTargetSegment();
                    // cycling strategy to automatically move to the next phalange.
                    // saves some headache when running a calibration protocol.
                    StepToTargetEnum();
                }
            }
            else
            {
                if (GUILayout.Button("Collect data for next segment"))
                {

                    t.StartDataCollectionForNextTargetSegment();
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go Prev Segment"))
            {
                StepEnum(-1);
            }

            if (GUILayout.Button("Go Next Segment"))
            {
                StepEnum(1);
            }
            GUILayout.EndHorizontal();
            if (Application.isPlaying && t.CollectingData)
            {
                var presentSegments = new HashSet<HPUIInteractorConeRayAngleSegment>();
                foreach (var record in t.DataRecords)
                {
                    presentSegments.Add(record.segment);
                }

                var missingSegments = "";
                Array allSegments = Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment));
                foreach (HPUIInteractorConeRayAngleSegment segment in allSegments)
                {
                    if (!presentSegments.Contains(segment))
                    {
                        missingSegments += $"{segment.ToString()}, ";
                    }
                }
                missingSegments = missingSegments.Trim(new[] { ' ', ',' });
                if (missingSegments.Length != 0)
                {
                    EditorGUILayout.HelpBox($"Missing Data for Segments: {missingSegments}", MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();

        }

        private void StepToTargetEnum(bool iterateToNext = true)
        {
            if (currentPhalangeIndex >= t.OrderOfCalibration.Count)
            {
                currentPhalangeIndex = 0;
            }
            t.TargetSegment = t.OrderOfCalibration[currentPhalangeIndex];
            if(iterateToNext) currentPhalangeIndex++;
        }

        private void StepEnum(int amt = 1)
        {
            int phalangeCount = Enum.GetNames(typeof(HPUIInteractorConeRayAngleSegment)).Length;
            int currentPhalangeIndex = Array.IndexOf(Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)), t.TargetSegment);
            if (amt > 0)
            {
                if (currentPhalangeIndex < phalangeCount - 1)
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)currentPhalangeIndex + amt;
                }
                else
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)0;
                }
            }
            else
            {
                if (currentPhalangeIndex == 0)
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)phalangeCount - 1;
                }
                else
                {
                    t.TargetSegment = (HPUIInteractorConeRayAngleSegment)currentPhalangeIndex + amt;
                }
            }
        }
    }
}
