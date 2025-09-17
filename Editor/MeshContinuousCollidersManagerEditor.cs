using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Core.Interaction;

namespace ubco.ovilab.HPUI.Core.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MeshContinuousCollidersManager))]
    public class MeshContinuousCollidersManagerEditor : UnityEditor.Editor
    {
        private MeshContinuousCollidersManager t;
        private bool computedMeshXRes;
        private bool successfullyComputedMeshXRes;

        public override void OnInspectorGUI()
        {

            serializedObject.Update();


            SerializedProperty vertRemapData = serializedObject.FindProperty("vertexRemapData");
            t = (MeshContinuousCollidersManager)target;

            GUIContent buttonContent = new GUIContent("Compute Vertex Remapping", "Run this with the mesh upright outside of play mode");

            if (GUILayout.Button(buttonContent))
            {
                int[] remapData = RemapVertices();

                // we can compute mesh x resolution
                // by leveraging pythagoras
                // that when the distance between two vertices after remapping
                // is greater than the distance between the previous two pairs
                // we know we're on the next row, so that's our last vertex
                // on the x-axis
                // .
                // | \  here the hypotenuse will always be greater than the x width, even for x = 2
                // .__.
                // this fails if for some reason mesh x res is 1
                // or if your differences are lesser than a nanometer
                // or if your mesh is not a rectangular grid
                // at which point, you're on your own

                Mesh tempMesh = new Mesh();
                t.Mesh.BakeMesh(tempMesh);
                for (int i = 1; i < remapData.Length - 1; i++)
                {
                    Vector3 prevVertex = tempMesh.vertices[remapData[i - 1]];
                    Vector3 currVertex = tempMesh.vertices[remapData[i]];
                    Vector3 nextVertex = tempMesh.vertices[remapData[i + 1]];
                    float prevToCurrDist = Vector3.Distance(prevVertex, currVertex);
                    float currToNextDist = Vector3.Distance(currVertex, nextVertex);
                    if (Mathf.Abs(prevToCurrDist - currToNextDist)>0.0000001f)
                    {
                        t.MeshXResolution = i + 1;
                        successfullyComputedMeshXRes = true;
                        break;
                    }
                }

                computedMeshXRes = true;

                vertRemapData.arraySize = remapData.Length;

                for (int i = 0; i < remapData.Length; i++)
                {
                    vertRemapData.GetArrayElementAtIndex(i).intValue = remapData[i];
                }

            }

            serializedObject.ApplyModifiedProperties();

            if(computedMeshXRes)
            {
                if (successfullyComputedMeshXRes)
                {
                    string message = $"Computed Mesh X Resolution as {t.MeshXResolution}";
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                }
                else
                {
                    string message = "Failed to compute Mesh X Resolution! Please Set Manually!";
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
            }

            DrawDefaultInspector();
        }

        private int[] RemapVertices()
        {
            SkinnedMeshRenderer targetMesh = t.Mesh;
            Mesh tempMesh = new Mesh();
            if(targetMesh==null)
            {
                Debug.LogError("Please assign mesh first!");
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
