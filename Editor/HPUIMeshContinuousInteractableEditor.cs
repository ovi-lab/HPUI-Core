using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEditor;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIMeshContinuousInteractable))]
    public class HPUIMeshContinuousInteractableEditor : HPUIBaseInteractableEditor
    {
        private HPUIMeshContinuousInteractable t;
        private SerializedProperty staticMesh;
        private SerializedProperty meshXResolution;
        
        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>()
        {
            "staticMesh",
            "meshXResolution",
        }).ToList();
        
        protected override void OnEnable()
        {
            base.OnEnable();
            t = (HPUIMeshContinuousInteractable)target;
            staticMesh = serializedObject.FindProperty("staticHPUIMesh");
            meshXResolution = serializedObject.FindProperty("meshXResolution");
        }

        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Static Mesh Configurations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(staticMesh);
            EditorGUILayout.PropertyField(meshXResolution);
        }
    }
}
