using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ubco.ovilab.HPUI.CustomMeshUtils
{
    /// <summary>
    /// Component to manage the colliders for a given rectangular custom mesh, based on <see cref="Interaction.DeformableSurfaceCollidersManager"/>
    /// </summary>
    public class CustomMeshCollidersManager : MonoBehaviour
    {
        [SerializeField] private VertexRemapData vertexRemapData;

        // [SerializeField] private GameObject rectifiedVertexDS;
        // [SerializeField, Range(0, 120)] private int id;
        
        private NativeArray<Vector3> verticesNative, normalsNative;
        private List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        private NativeArray<int> remappedVertices;
        private Mesh tempMesh;
        private bool generatedColliders;

        public void SetupColliders(SkinnedMeshRenderer keyboardMesh)
        {
            tempMesh = new Mesh(); 
            keyboardMesh.BakeMesh(tempMesh, true);
            if (vertexRemapData.RemappedVertices.Length <= 0)
            {
                VertexRemapper.GetRectifiedIndices(vertexRemapData, tempMesh);
            }
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);
            if (!remappedVertices.IsCreated)
            {
                remappedVertices = new NativeArray<int>(vertexRemapData.RemappedVertices, Allocator.Persistent);
            }
            else
            {
                remappedVertices.CopyFrom(vertexRemapData.RemappedVertices);
            }
            
            generatedColliders = true;
        }

        private void Update()
        {
            if (!generatedColliders) return;
            
            
            // rectifiedVertexDS.transform.localPosition = vertices[remappedVertices[id]];
        }
    }
}