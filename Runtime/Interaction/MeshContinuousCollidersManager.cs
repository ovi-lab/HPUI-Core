using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Component to manage the colliders for a given rectangular custom mesh, based on <see cref="Interaction.DeformableSurfaceCollidersManager"/>
    /// </summary>
    public class MeshContinuousCollidersManager : MonoBehaviour
    {
        [SerializeField, HideInInspector] private int[] vertexRemapData;
        [Tooltip("Incase the vertices are being ordered in reverse for whatever reason")][SerializeField] private bool flipOrderForRecompute;

        [SerializeField] private int meshXResolution;

        [Tooltip("The associated SkinnedMeshRenderer used by this interactable")]
        [SerializeField] private SkinnedMeshRenderer mesh;
        /// <summary>
        /// The associated SkinnedMeshRenderer used by this interactable
        /// </summary>
        public SkinnedMeshRenderer Mesh
        {
            get
            {
                if (mesh == null)
                {
                    mesh = GetComponent<SkinnedMeshRenderer>();
                }
                return mesh;
            }
            set => mesh = value;
        }

        private NativeArray<Vector3> vertices_native, normals_native;
        private List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        private NativeArray<int> remapped_vertices_data;
        private TransformAccessArray colliderObjects;
        private Mesh tempMesh;
        private bool generatedColliders;
        private int meshYResolution;
        private float xWidth, yWidth, offsetX, offsetY;
        private Dictionary<Collider, Vector2> colliderCoords = new Dictionary<Collider, Vector2>();
        private Dictionary<Vector2Int, Collider> rawCoordsToCollider = new Dictionary<Vector2Int, Collider>();

        public float XWidth => xWidth;
        public float YWidth => yWidth;
        public float OffsetX => offsetX;
        public float OffsetY => offsetY;

        public int MeshXResolution
        {
            get => meshXResolution;
            set => meshXResolution = value;
        }

        public int MeshYResolution => meshYResolution;
        public Dictionary<Vector2Int, Collider> RawCoordsToCollider => rawCoordsToCollider;

        private void Update()
        {
            if (!generatedColliders)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("MeshContinuousCollidersManager.Update");
            UpdateColliderPositions();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void OnDestroy()
        {
            if (vertices_native.IsCreated) vertices_native.Dispose();
            if (normals_native.IsCreated) normals_native.Dispose();
            if (remapped_vertices_data.IsCreated) remapped_vertices_data.Dispose();
            if (colliderObjects.isCreated) colliderObjects.Dispose();
        }

        /// <summary>
        /// Configures the colliders on the respective mesh.
        /// </summary>
        public List<Collider> SetupColliders()
        {
            tempMesh = new Mesh();
            mesh.BakeMesh(tempMesh, true);

            if (vertexRemapData == null)
            {
                throw new ArgumentException("Run \"Compute Vertex Remapping\" outside play mode first!");
            }

            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);

            remapped_vertices_data = new NativeArray<int>(vertexRemapData, Allocator.Persistent);

            meshYResolution = vertices.Count / meshXResolution;
            Transform meshTransform = mesh.gameObject.transform;

            xWidth = Vector3.Distance(meshTransform.TransformPoint(vertices[remapped_vertices_data[0]]),
                                      meshTransform.TransformPoint(vertices[remapped_vertices_data[1]]));
            yWidth = Vector3.Distance(meshTransform.TransformPoint(vertices[remapped_vertices_data[0]]),
                                      meshTransform.TransformPoint(vertices[remapped_vertices_data[meshXResolution]]));

            float localXWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[1]]);
            float localYWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[meshXResolution]]);

            offsetX = xWidth * meshXResolution * 0.5f;
            offsetY = yWidth * meshYResolution * 0.5f;
            Transform[] colliderTransforms = new Transform[vertices.Count];

            for (int i = 0; i < remapped_vertices_data.Length; i++)
            {
                int x = i % meshXResolution;
                int y = i / meshXResolution;
                GameObject colliderGameObject = new GameObject();
                colliderGameObject.layer = meshTransform.gameObject.layer;
                Collider col = colliderGameObject.AddComponent<BoxCollider>();
                colliderGameObject.name = "X: " + x + "; Y: " + y + ";";
                colliderGameObject.transform.parent = meshTransform;
                colliderGameObject.transform.localPosition = vertices[remapped_vertices_data[i]];
                Vector3 targetScale = Vector3.one * 0.001f;
                targetScale.x = localXWidth;
                targetScale.z = localYWidth;
                colliderGameObject.transform.localScale = targetScale;
                colliderGameObject.transform.localRotation = Quaternion.identity;
                colliderTransforms[i] = colliderGameObject.transform;
                Vector2 coords = new Vector2(xWidth * x - offsetX, yWidth * y - offsetY);
                colliderCoords.Add(col, coords);
                rawCoordsToCollider.Add(new Vector2Int(x, y), col);
            }

            colliderObjects = new TransformAccessArray(colliderTransforms);
            vertices_native = new NativeArray<Vector3>(vertices.ToArray(), Allocator.Persistent);
            normals_native = new NativeArray<Vector3>(normals.ToArray(), Allocator.Persistent);
            generatedColliders = true;

            return colliderCoords.Keys.ToList();
        }

        /// <summary>
        /// Return the (approximate) point on the surface of where the collider is.
        /// The returned Vector2 - (x, z) on the xz-plane. This is relative to the
        /// center of the surface.
        /// </summary>
        public Vector2 GetSurfacePointForCollider(Collider col)
        {
            if (!colliderCoords.TryGetValue(col, out Vector2 coordsForCol))
            {
                throw new ArgumentException($"Unknown {col.name}");
            }
            return coordsForCol;
        }

        protected void UpdateColliderPositions()
        {
            mesh.BakeMesh(tempMesh, true);
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);

            vertices_native.CopyFrom(vertices.ToArray());
            normals_native.CopyFrom(normals.ToArray());

            DeformedCollidersJob job = new DeformedCollidersJob()
            {
                Normals = normals_native,
                Vertices = vertices_native,
                RemappedIndices = remapped_vertices_data,
                MaxX = meshXResolution,
                MaxY = meshYResolution,
            };

            JobHandle jobHandle = job.Schedule(colliderObjects);
            jobHandle.Complete();
        }

        struct DeformedCollidersJob: IJobParallelForTransform
        {
            private Vector3 right, forward, temppos;
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
                    forward = Vertices[RemappedIndices[i - MaxX]] - Vertices[RemappedIndices[i]];
                else
                    forward = Vertices[RemappedIndices[i+ MaxX]] - Vertices[RemappedIndices[i]];

                col.localPosition = temppos;
                col.localRotation = Quaternion.LookRotation(forward, Normals[RemappedIndices[i]]);
            }
        }
    }
}
