using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.StaticMesh;
using ubco.ovilab.HPUI.Editor;
using UnityEditor;
using UnityEngine;

namespace UnityLibrary
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIStaticContinuousInteractable))]
    public class HPUIStaticContinuousInteractableEditor : HPUIBaseInteractableEditor
    {
        private HPUIStaticContinuousInteractable t;
        private SerializedProperty staticMesh;
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
            t = (HPUIStaticContinuousInteractable)target;
            staticMesh = serializedObject.FindProperty("staticHPUIMesh");
            meshXRes = serializedObject.FindProperty("meshXRes");
        }

        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Mesh Configurations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(staticMesh);
            EditorGUILayout.PropertyField(meshXRes);
        }
    }
}