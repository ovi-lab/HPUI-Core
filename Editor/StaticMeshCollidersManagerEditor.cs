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
            serializedObject.Update();

            SerializedProperty vertRemapData = serializedObject.FindProperty("vertexRemapData");
            t = (StaticMeshCollidersManager)target;

            GUIContent buttonContent = new GUIContent("Compute Vertex Remapping", "Run this with the hand upright and a blank Vertex Remap Data asset assigned outside of play mode");

            if (GUILayout.Button(buttonContent))
            {
                int[] remapData = t.RemapVertices(); 

                vertRemapData.arraySize = remapData.Length;

                for (int i = 0; i < remapData.Length; i++)
                {
                    vertRemapData.GetArrayElementAtIndex(i).intValue = remapData[i];
                }
            }

            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }

    }
}