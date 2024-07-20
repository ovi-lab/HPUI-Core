using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.CustomMeshUtils;
using ubco.ovilab.HPUI.Editor;
using UnityEditor;
using UnityEngine;

namespace UnityLibrary
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUICustomMesh))]
    public class HPUICustomMeshEditor : HPUIBaseInteractableEditor
    {
        private HPUICustomMesh t;
        private SerializedProperty customMesh;
        private SerializedProperty meshXRes;
        
        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>()
        {
            "continuousSurfaceCreatedEvent",
            "boundsCollider", // NOTE: this is not relevant to the HPUICustomMesh.
            "ui"
        }).ToList();
        
        protected override void OnEnable()
        {
            base.OnEnable();
            t = (HPUICustomMesh)target;
            customMesh = serializedObject.FindProperty("customHPUIMesh");
            meshXRes = serializedObject.FindProperty("meshXRes");
        }

        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Mesh Configurations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(customMesh);
            EditorGUILayout.PropertyField(meshXRes);
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Set Colliders Matrix"))
            {
                t.CreateCollidersMatrix();
            }
            base.OnInspectorGUI();
        }
    }
}