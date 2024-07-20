using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

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
        private TransformAccessArray colliderObjects;
        private Mesh tempMesh;
        private bool generatedColliders;
        private SkinnedMeshRenderer targetMesh;
        private float scaleFactor = 0.001f;
        private int meshXRes, meshYRes;
        
        private void Update()
        {
            if (!generatedColliders || !isActiveAndEnabled) return;

            UpdateColliderPositions();
            // rectifiedVertexDS.transform.localPosition = vertices[remappedVertices[id]];
        }

        private void OnDestroy()
        {
            vertices_native.Dispose();
            normals_native.Dispose();
            remapped_vertices_data.Dispose();
        }

        public void SetupColliders(SkinnedMeshRenderer keyboardMesh, HPUICustomMesh hpuiCustomMesh)
        {
            targetMesh = keyboardMesh;
            tempMesh = new Mesh(); 
            targetMesh.BakeMesh(tempMesh, true);
            
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
            
            remapped_vertices_data = new NativeArray<int>(vertexRemapData.RemappedVertices, Allocator.Persistent);
            
            
            meshXRes = hpuiCustomMesh.MeshXRes;
            meshYRes = vertices.Count / meshXRes;
            
            float xWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[1]]);
            float yWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[meshXRes]]);
            Transform[] colliderTransforms = new Transform[vertices.Count];
            Transform meshTransform = targetMesh.gameObject.transform;
            for (int i = 0; i < remapped_vertices_data.Length; i++)
            {
                int x = i % meshXRes;
                int y = i / meshXRes;
                GameObject col = new GameObject();
                col.AddComponent<BoxCollider>();
                col.name = "X: " + x + "; Y: " + y + ";";
                col.transform.parent = meshTransform;
                col.transform.localPosition = vertices[remapped_vertices_data[i]];
                Vector3 targetScale = Vector3.one * scaleFactor;
                targetScale.x = xWidth;
                targetScale.y = yWidth;
                col.transform.localScale = targetScale;
                col.transform.localRotation = Quaternion.LookRotation(normals[remapped_vertices_data[i]]);
                colliderTransforms[i] = col.transform;
            }
            colliderObjects = new TransformAccessArray(colliderTransforms);
            vertices_native = new NativeArray<Vector3>(vertices.ToArray(), Allocator.Persistent);
            normals_native = new NativeArray<Vector3>(normals.ToArray(), Allocator.Persistent);
            generatedColliders = true;
        }

        
        protected void UpdateColliderPositions()
        {
            targetMesh.BakeMesh(tempMesh, true);
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);

            vertices_native.CopyFrom(vertices.ToArray());
            normals_native.CopyFrom(normals.ToArray());
            
            DeformedCollidersJob job = new DeformedCollidersJob()
            {
                Normals = normals_native,
                Vertices = vertices_native,
                RemappedIndices = remapped_vertices_data,
                MaxX = meshXRes,
                MaxY = meshYRes,
            };

            JobHandle jobHandle = job.Schedule(colliderObjects);
            jobHandle.Complete();
        }
        
        struct DeformedCollidersJob: IJobParallelForTransform
        {
            private Vector3 right, upwards, temppos;
            public float ScaleFactor, GridSize; 
            public int MaxX, MaxY;

            [ReadOnly] public NativeArray<Vector3> Vertices;
            [ReadOnly] public NativeArray<Vector3> Normals;
            [ReadOnly] public NativeArray<int> RemappedIndices;
	
            public void Execute(int i, TransformAccess col)
            {
                temppos = Vertices[RemappedIndices[i]];
                temppos.z += -0.0002f;

                if (i >= (MaxY-1) * MaxX )
                    upwards = Vertices[RemappedIndices[i - MaxX]] - Vertices[RemappedIndices[i]];
                else
                    upwards = Vertices[RemappedIndices[i+ MaxX]] - Vertices[RemappedIndices[i]];
                
                col.localPosition = temppos;
                col.localRotation = Quaternion.LookRotation(Normals[RemappedIndices[i]], upwards);
            }
        }
    }
}