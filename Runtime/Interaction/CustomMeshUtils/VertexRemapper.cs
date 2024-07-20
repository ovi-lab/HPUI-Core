using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.CustomMeshUtils
{
    public static class VertexRemapper 
    {
        /// <summary>
        /// For a given rectangular mesh, returns an integer array with remapped vertices from bottom left to top right
        /// </summary>
        /// <param name="remapData">Scriptable object to store the remapped indices</param>
        /// <param name="mesh">The mesh to sort vertices for</param>
        /// <param name="smesh">The skinned mesh renderer to extract the mesh from</param>
        /// <returns></returns>
        public static void GetRectifiedIndices(VertexRemapData remapData, Mesh mesh)
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
            remapData.RemappedVertices = correctedIndices;
        }
        
        public static List<Vector3> RearrangeList(List<Vector3> originalList, int[] newOrder)
        {
            if (originalList.Count != newOrder.Length)
            {
                Debug.LogError("The length of the array does not match the length of the list.");
                return originalList;
            }

            List<Vector3> rearrangedList = new List<Vector3>(originalList.Count);

            foreach (int index in newOrder)
            {
                if (index < 0 || index >= originalList.Count)
                {
                    Debug.LogError("Index out of range.");
                    return originalList;
                }
                rearrangedList.Add(originalList[index]);
            }
            return rearrangedList;
        }
    }
}

