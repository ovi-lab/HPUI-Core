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
        private int currentPhalangeIndex;

        protected void OnEnable()
        {
            t = target as GuidedDataCollector;
            if (t.OrderOfCalibration.Count > 0)
            {
                t.StepToTargetPhalange(t.OrderOfCalibration[0]);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (t.OrderOfCalibration.Count == 0)
            {
                EditorGUILayout.HelpBox("The guided data collector works best with a custom order of calibration. Without one, you will have to change the target phalange to be calibrated manually. Consider populating Order of Calibration.", MessageType.Warning);
            }

            GUI.enabled = Application.isPlaying && t.CollectingData;

            if (!t.PauseDataCollection)
            {
                if (GUILayout.Button("Pause Data Collection"))
                {
                    t.EndDataCollectionForTargetSegment();
                    // cycling strategy to automatically move to the next phalange.
                    // saves some headache when running a calibration protocol.
                    if (t.AutoMoveToNextPhalange)
                    {
                        if (t.OrderOfCalibration.Count > 0)
                        {
                            StepThroughCustomPhalanges(1);
                        }
                        else
                        {
                            StepThroughAllPhalanges(1);
                        }
                    }
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
            GUI.enabled = t.PauseDataCollection;

            if (GUILayout.Button("Go Prev Segment"))
            {
                if (t.OrderOfCalibration.Count > 0)
                {
                    StepThroughCustomPhalanges(-1);
                }
                else
                {
                    StepThroughAllPhalanges(-1);
                }
            }

            if (GUILayout.Button("Go Next Segment"))
            {
                if (t.OrderOfCalibration.Count > 0)
                {
                    StepThroughCustomPhalanges(1);
                }
                else
                {
                    StepThroughAllPhalanges(1);
                }
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
                if (t.OrderOfCalibration.Count > 0)
                {
                    foreach (HPUIInteractorConeRayAngleSegment segment in t.OrderOfCalibration)
                    {
                        if (!presentSegments.Contains(segment))
                        {
                            missingSegments += $"{segment.ToString()}, ";
                        }
                    }
                }
                else
                {
                    Array allSegments = Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment));
                    foreach (HPUIInteractorConeRayAngleSegment segment in allSegments)
                    {
                        if (!presentSegments.Contains(segment))
                        {
                            missingSegments += $"{segment.ToString()}, ";
                        }
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

        private void StepThroughCustomPhalanges(int amt = 1)
        {
            currentPhalangeIndex = (currentPhalangeIndex + amt) % t.OrderOfCalibration.Count;
            if (currentPhalangeIndex < 0)
                currentPhalangeIndex += t.OrderOfCalibration.Count;
            var targetSegment = t.OrderOfCalibration[currentPhalangeIndex];
            t.StepToTargetPhalange(targetSegment);
        }

        private void StepThroughAllPhalanges(int amt = 1)
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
