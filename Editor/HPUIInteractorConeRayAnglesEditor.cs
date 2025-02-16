using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIInteractorConeRayAngles), true)]
    public class HPUIInteractorConeAnglesEditor: UnityEditor.Editor
    {
        private HPUIInteractorConeRayAngles t;

        private float offsetValue;
        private bool applyToAll;
        private XRHandJointID selectedPhalange = XRHandJointID.IndexDistal;

        // storing all baseline thresholds for first ray per phalange
        // assuming that offsets are applied uniformly per phalange
        private Dictionary<XRHandJointID, float> baselineThresholds = new Dictionary<XRHandJointID, float>();
        // storing previous applied offset
        private Dictionary<XRHandJointID, float> cachedOffsets = new Dictionary<XRHandJointID, float>();

        protected void OnEnable()
        {
            t = target as HPUIInteractorConeRayAngles;
            foreach (var kvp in t.ActiveFingerAngles)
            {
                XRHandJointID phalange = kvp.Key;
                List<HPUIInteractorRayAngle> rayAngles = kvp.Value;
                if (!baselineThresholds.ContainsKey(phalange))
                {
                    try
                    {
                        baselineThresholds[phalange] = rayAngles.Select(ray => ray.RaySelectionThreshold).ToList()[0];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // this is little proximal, that needs to be fixed at some point in time
                    }
                }
                if (cachedOffsets.ContainsKey(phalange)) continue;
                float initialOffset = rayAngles.Count > 0 ? rayAngles[0].RaySelectionThreshold - baselineThresholds[phalange] : 0f;
                cachedOffsets[phalange] = initialOffset;
            }
            Undo.undoRedoPerformed += OnUndoRedoEvent;
        }

        protected void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoEvent;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Apply Ray Selection Threshold Offset", EditorStyles.boldLabel);
            offsetValue = EditorGUILayout.FloatField("Offset", offsetValue);
            applyToAll = EditorGUILayout.Toggle("Apply to All", applyToAll);

            EditorGUI.BeginDisabledGroup(applyToAll);
            selectedPhalange = (XRHandJointID)EditorGUILayout.EnumPopup("Phalange", selectedPhalange);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Apply Offset"))
            {
                Undo.RecordObject(t, "Apply RaySelectionThreshold Offset");

                if (applyToAll)
                {
                    foreach (KeyValuePair<XRHandJointID, List<HPUIInteractorRayAngle>> kvp in t.ActiveFingerAngles)
                    {
                        ApplyOffsetToList(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    if (t.ActiveFingerAngles.TryGetValue(selectedPhalange, out List<HPUIInteractorRayAngle> list))
                    {
                        ApplyOffsetToList(selectedPhalange, list);
                    }
                    else
                    {
                        Debug.LogWarning("Selected phalange not found in ActiveFingerAngles.");
                    }
                }
                EditorUtility.SetDirty(t);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        /// <summary>
        /// applies offsets based on original threshold value
        /// </summary>
        private void ApplyOffsetToList(XRHandJointID phalange, List<HPUIInteractorRayAngle> rayAngles)
        {
            float previousOffset = cachedOffsets.GetValueOrDefault(phalange, 0f);

            foreach (HPUIInteractorRayAngle rayAngle in rayAngles)
            {
                rayAngle.RaySelectionThreshold = rayAngle.RaySelectionThreshold - previousOffset + offsetValue;
            }

            cachedOffsets[phalange] = offsetValue;
        }

        /// <summary>
        /// Called on Undo/Redo. For each phalange, recompute the post undo/redo applied offset from the current thresholds.
        /// </summary>
        private void OnUndoRedoEvent()
        {
            foreach (var kvp in t.ActiveFingerAngles)
            {
                XRHandJointID phalange = kvp.Key;
                var rayAngles = kvp.Value;
                if (rayAngles.Count > 0 && baselineThresholds.TryGetValue(phalange, out float threshold))
                {
                    // Assuming all ray angles were modified uniformly, update the cached offset based on the first element.
                    float newOffset = rayAngles[0].RaySelectionThreshold - threshold;
                    cachedOffsets[phalange] = newOffset;
                }
            }
            Repaint();

        }
    }
}
