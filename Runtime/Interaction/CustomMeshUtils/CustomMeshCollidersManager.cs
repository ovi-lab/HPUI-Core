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
        
        private NativeArray<Vector3> vertices_native, normals_native;
        private List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        private NativeArray<int> remapped_vertices_data;
        private Mesh tempMesh;
        private bool generatedColliders;

        public void SetupColliders(SkinnedMeshRenderer keyboardMesh, HPUICustomMesh hpuiCustomMesh)
        {
            tempMesh = new Mesh(); 
            keyboardMesh.BakeMesh(tempMesh, true);
            if (vertexRemapData == null)
            {
                throw new ArgumentException("Missing Vertex Remap Data Asset! Create a new one or provide an existing one!");
            }
            if (vertexRemapData.RemappedVertices.Length <= 0)
            {
                VertexRemapper.GetRectifiedIndices(vertexRemapData, tempMesh);
            }
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);
            if (!remapped_vertices_data.IsCreated)
            {
                remapped_vertices_data = new NativeArray<int>(vertexRemapData.RemappedVertices, Allocator.Persistent);
            }
            else
            {
                remapped_vertices_data.CopyFrom(vertexRemapData.RemappedVertices);
            }

            float xWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[1]]);
            float yWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[hpuiCustomMesh.MeshXRes]]);
            
            Transform meshTransform = keyboardMesh.gameObject.transform;
            for (int i = 0; i < remapped_vertices_data.Length; i++)
            {
                int x = i % hpuiCustomMesh.MeshXRes;
                int y = i / hpuiCustomMesh.MeshXRes;
                GameObject col = new GameObject();
                col.AddComponent<BoxCollider>();
                col.name = "X: " + x + "; Y: " + y + ";";
                col.transform.parent = meshTransform;
                col.transform.localPosition = vertices[remapped_vertices_data[i]];
                Vector3 targetScale = Vector3.one * 0.0001f;
                targetScale.x = xWidth;
                targetScale.y = yWidth;
                col.transform.localScale = targetScale;
                col.transform.localRotation = Quaternion.LookRotation(normals[remapped_vertices_data[i]]);
            }
            
            generatedColliders = true;
        }

        private void Update()
        {
            if (!generatedColliders) return;
            
            
            // rectifiedVertexDS.transform.localPosition = vertices[remappedVertices[id]];
        }

        private void OnDrawGizmos()
        {
            foreach (int t in remapped_vertices_data)
            {
                Debug.DrawRay(transform.TransformPoint(vertices[t]), Vector3.Normalize(normals[t]));
            }
        }
    }
}