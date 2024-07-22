using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StaticMeshCollidersManager))]
    public class StaticMeshCollidersManagerEditor : UnityEditor.Editor
    {
        private StaticMeshCollidersManager t;

        public override void OnInspectorGUI()
        {
            t = (StaticMeshCollidersManager)target;
            GUIContent buttonContent = new GUIContent("Compute Vertex Remapping", "Run this with the hand upright and a blank Vertex Remap Data asset assigned outside of play mode");
            if(GUILayout.Button(buttonContent))
            {
                t.RemapVertices();
            }

            DrawDefaultInspector();
        }
    }
}