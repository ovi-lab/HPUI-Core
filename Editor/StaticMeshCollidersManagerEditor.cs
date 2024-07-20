using System;
using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.StaticMesh;

namespace UnityLibrary
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StaticMeshCollidersManager))]
    public class StaticMeshCollidersManagerEditor : Editor
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