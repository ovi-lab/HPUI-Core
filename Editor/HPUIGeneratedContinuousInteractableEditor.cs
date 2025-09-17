using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Core.Interaction;
using System.Collections.Generic;
using System.Linq;

namespace ubco.ovilab.HPUI.Core.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIGeneratedContinuousInteractable), true)]
    public class HPUIGeneratedContinuousInteractableEditor: HPUIBaseInteractableEditor
    {
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Assets/HPUIContinousUI.prefab";
        private HPUIGeneratedContinuousInteractable t;
        private SerializedProperty uiProp;

        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>()
        {
            "continuousSurfaceCreatedEvent",
            "boundsCollider", // NOTE: this is not relevant to the HPUIGeneratedContinuousInteractable.
        }).ToList();


        protected SerializedProperty sigmaFactorProperty,
            x_sizeProperty, y_sizeProperty, y_divisionsProperty,
            offsetProperty, numberOfBonesPerVertexProperty,
            keypointsDataProperty, defaultMaterialProperty,
            filterProperty;

        private bool advancedFoldout;

        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIGeneratedContinuousInteractable;

            sigmaFactorProperty = serializedObject.FindProperty("sigmaFactor");
            x_sizeProperty = serializedObject.FindProperty("x_size");
            y_sizeProperty = serializedObject.FindProperty("y_size");
            y_divisionsProperty = serializedObject.FindProperty("y_divisions");
            offsetProperty = serializedObject.FindProperty("offset");
            numberOfBonesPerVertexProperty = serializedObject.FindProperty("numberOfBonesPerVertex");
            keypointsDataProperty = serializedObject.FindProperty("keypointsData");
            defaultMaterialProperty = serializedObject.FindProperty("defaultMaterial");
            filterProperty = serializedObject.FindProperty("filter");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button(new GUIContent("Manual recompute", "Assuming the the corresponding hand is held straight, compute and generate the surface")))
            {
                foreach (Object t in targets)
                {
                    (t as HPUIGeneratedContinuousInteractable)?.ManualRecompute();
                }
            }
            GUI.enabled = true;
        }

        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.LabelField("Continuous surface configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(x_sizeProperty);
            EditorGUILayout.PropertyField(y_sizeProperty);
            EditorGUILayout.PropertyField(y_divisionsProperty);
            EditorGUILayout.PropertyField(offsetProperty);
            EditorGUILayout.PropertyField(numberOfBonesPerVertexProperty);
            EditorGUILayout.PropertyField(defaultMaterialProperty);
            EditorGUILayout.PropertyField(filterProperty);
            EditorGUILayout.PropertyField(keypointsDataProperty);

            advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced Settings (mesh generation)");

            if (advancedFoldout)
            {
                EditorGUILayout.PropertyField(sigmaFactorProperty);
            }
            EditorGUILayout.Space();
        }

        /// <inheritdoc />
        protected override List<string> GetDerivedSerializedPropertyNames()
        {
            List<string> props = base.GetDerivedSerializedPropertyNames();
            props.Add("x_size");
            props.Add("y_size");
            props.Add("y_divisions");
            props.Add("offset");
            props.Add("numberOfBonesPerVertex");
            props.Add("defaultMaterial");
            props.Add("filter");
            props.Add("keypointsData");
            props.Add("sigmaFactor");
            return props;
        }
    }
}
