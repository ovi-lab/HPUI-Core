using System;
using System.Collections.Generic;
using System.Linq;
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

            GUIContent buttonContent = new GUIContent("Compute Vertex Remapping", "Run this with the hand upright outside of play mode");

            if (GUILayout.Button(buttonContent))
            {
                int[] remapData = RemapVertices();

                vertRemapData.arraySize = remapData.Length;

                for (int i = 0; i < remapData.Length; i++)
                {
                    vertRemapData.GetArrayElementAtIndex(i).intValue = remapData[i];
                }
            }

            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }

        private int[] RemapVertices()
        {
            SkinnedMeshRenderer targetMesh = t.GetComponent<HPUIMeshContinuousInteractable>().StaticHPUIMesh;
            Mesh tempMesh = new Mesh();
            if(targetMesh==null)
            {
                Debug.LogError("Please assign static mesh to the HPUI Static Continuous Interactable Component first!");
            }
            targetMesh.BakeMesh(tempMesh, true);
            return GetRectifiedIndices(tempMesh, serializedObject.FindProperty("flipOrderForRecompute").boolValue);
        }

        private int[] GetRectifiedIndices(Mesh mesh, bool flipOrder)
        {
            int vertexCount = mesh.vertexCount;
            Vector3[] vertices = mesh.vertices;
            int[] correctedIndices = new int[vertexCount];
            List<(Vector3 vertex, int index)> indexedVertices = new List<(Vector3 vertex, int index)>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                indexedVertices.Add((vertices[i], i));
            }
            indexedVertices.Sort((a, b) =>
            {
                if (Math.Abs(a.vertex.y - b.vertex.y) > 0.00001)
                    return a.vertex.y.CompareTo(b.vertex.y);
                return b.vertex.x.CompareTo(a.vertex.x);
            });

            for (int i = 0; i < vertexCount; i++)
            {
                correctedIndices[i] = indexedVertices[i].index;
            }
            int[] remapData = flipOrder ? correctedIndices.Reverse().ToArray() : correctedIndices;
            return remapData;
        }
    }
}
