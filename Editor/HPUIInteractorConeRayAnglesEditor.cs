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
        private bool overwriteThisAsset;
        private XRHandJointID selectedPhalange = XRHandJointID.IndexDistal;

        // storing all baseline thresholds
        private Dictionary<XRHandJointID, List<float>> baselineThresholds = new Dictionary<XRHandJointID, List<float>>();

        private const string DONT_ASK_FOR_OVERRIDE_EDITOR_PREF_KEY = "ubco.ovilab.HPUI.Interaction.ConeThresholdsEditor.DontAskForOverride";
        private bool userSelection;
        private string newAssetFileSaveName;

        private bool advancedSettingsFoldout;

        private const string OFFSET_TOOLTIP = "The uniform offset to apply to the ray thresholds for a given phalange or all phalanges. Note that it always applies it from the base values, which are derived upon the first time the asset is selected in this Unity session. So setting it to 2 and then 1 won't increment it by 3, the second operation will only increment it by 1 from the base amount. Consequently, setting it to 0 will reset it to whatever the original values were.";

        protected void OnEnable()
        {
            t = target as HPUIInteractorConeRayAngles;
            if (!t) return;
            foreach ((XRHandJointID phalange, List<HPUIInteractorRayAngle> rays) in t.ActiveFingerAngles)
            {
                if (!baselineThresholds.ContainsKey(phalange) && rays.Count > 0)
                {
                    baselineThresholds[phalange] = rays.Select(ray => ray.RaySelectionThreshold).ToList();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            advancedSettingsFoldout = EditorGUILayout.Foldout(advancedSettingsFoldout, "Advanced Operations", toggleOnLabelClick:true);
            if (advancedSettingsFoldout)
            {
                EditorGUILayout.LabelField("Apply Ray Selection Threshold Offset", EditorStyles.boldLabel);
                offsetValue = EditorGUILayout.FloatField(new GUIContent("Offset", OFFSET_TOOLTIP), offsetValue);
                applyToAll = EditorGUILayout.Toggle(new GUIContent("Apply to All", "apply offset to all phalanges at once"), applyToAll);
                overwriteThisAsset = EditorGUILayout.Toggle("Overwrite This Asset", overwriteThisAsset);

                EditorGUI.BeginDisabledGroup(applyToAll);
                selectedPhalange = (XRHandJointID)EditorGUILayout.EnumPopup("Phalange", selectedPhalange);
                EditorGUI.EndDisabledGroup();

                if (overwriteThisAsset)
                {
                    EditorGUILayout.HelpBox("You are about to overwrite this asset!", MessageType.Warning); // warning
                    GUI.backgroundColor = Color.red; // more warning
                }
                if (GUILayout.Button(overwriteThisAsset ? "Overwrite this Asset and Apply Offset" : "Create Copy and Apply Offset"))
                {
                    if (overwriteThisAsset)
                    {
                        userSelection = EditorUtility.DisplayDialog("Overwrite Cone Ray Angles Asset",
                            "Are you sure you want to overwrite this asset?" +
                            "\nYou can undo or set offsets to 0 to regain original values." +
                            "\nOnce you overwrite and close the editor the values will be permanently modified!",
                            "Yes", "No", DialogOptOutDecisionType.ForThisSession,
                            DONT_ASK_FOR_OVERRIDE_EDITOR_PREF_KEY); // https://www.youtube.com/watch?v=xrg-RgF5F8o Warning
                        if (userSelection)
                        {
                            ApplyModifications();
                        }
                    }
                    else
                    {
                        newAssetFileSaveName = EditorUtility.SaveFilePanelInProject("Save new cone angles asset",
                            $"{t.name}_copy_all_{offsetValue}", "asset",
                            "Save location for the modified cone angle asset.");
                        if (!string.IsNullOrWhiteSpace(newAssetFileSaveName))
                        {
                            ApplyModifications(newAssetFileSaveName);
                        }
                    }
                }
            }
        }

        private void ApplyModifications()
        {
            Undo.RecordObject(t, "Apply RaySelectionThreshold Offset");
            if (applyToAll)
            {
                foreach ((XRHandJointID phalange, List<HPUIInteractorRayAngle> rayAngle) in t.ActiveFingerAngles)
                {
                    ApplyOffsetToAsset(phalange, rayAngle);
                }

            }
            else // apply to selected phalange
            {
                if (t.ActiveFingerAngles.TryGetValue(selectedPhalange, out List<HPUIInteractorRayAngle> rayAngles))
                {
                    ApplyOffsetToAsset(selectedPhalange, rayAngles);
                }
                else
                {
                    throw new Exception("Selected phalange not found in ActiveFingerAngles.");
                }
            }
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ApplyModifications(string newAssetSavePath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(newAssetSavePath), "Invalid Save Path!");
            HPUIInteractorConeRayAngles newAsset = Instantiate(t);
            if (applyToAll)
            {
                foreach ((XRHandJointID phalange, List<HPUIInteractorRayAngle> rayAngle) in newAsset.ActiveFingerAngles)
                {
                    ApplyOffsetToAsset(phalange, rayAngle);
                }
            }
            else // apply to selected phalange
            {
                newAsset.ActiveFingerAngles.TryGetValue(selectedPhalange, out List<HPUIInteractorRayAngle> newRayAngles);
                Debug.Assert(newRayAngles != null, "New asset copy doesn't have the same data as the old asset??");
                ApplyOffsetToAsset(selectedPhalange, newRayAngles);
            }
            AssetDatabase.CreateAsset(newAsset, newAssetSavePath);
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// applies offsets based on original threshold value
        /// </summary>
        private void ApplyOffsetToAsset(XRHandJointID phalange, List<HPUIInteractorRayAngle> rayAngles)
        {
            List<float> baselineThresholdsForPhalange = baselineThresholds.GetValueOrDefault(phalange, null);

            for (int i = 0; i < rayAngles.Count; i++)
            {
                HPUIInteractorRayAngle rayAngle = rayAngles[i];
                float baseThreshold = baselineThresholdsForPhalange[i];
                rayAngle.RaySelectionThreshold = baseThreshold + offsetValue;
            }
        }
    }
}
