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
    public class StaticMeshCollidersManager : MonoBehaviour
    {
        [SerializeField] private VertexRemapData vertexRemapData;
        [Tooltip("Incase the vertices are being ordered in reverse for whatever reason")][SerializeField] private bool flipOrderForRecompute;
        
        //FIXME: Debug Code
        // [SerializeField] private GameObject rectifiedVertexDS;
        // [SerializeField, Range(0, 120)] private int id;
        
        private NativeArray<Vector3> vertices_native, normals_native;
        private List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        private NativeArray<int> remapped_vertices_data;
        private TransformAccessArray colliderObjects;
        private Mesh tempMesh;
        private SkinnedMeshRenderer targetMesh;
        private bool generatedColliders;
        private float scaleFactor = 0.001f;
        private int meshXResolution, meshYRes;
        private Dictionary<Collider, Vector2> colliderCoords = new Dictionary<Collider, Vector2>();
        private Dictionary<Collider, Vector2> colliderCoordsNormalisedAndFlipped = new Dictionary<Collider, Vector2>();

        private void Update()
        {
            if (!generatedColliders)
            {
                return;
            }
            
            UpdateColliderPositions();
            
            //FIXME: Debug Code
            // rectifiedVertexDS.transform.localPosition = vertices[remappedVertices[id]];
        }

        private void OnDestroy()
        {
            vertices_native.Dispose();
            normals_native.Dispose();
            remapped_vertices_data.Dispose();
            colliderObjects.Dispose();
        }

        public List<Collider> SetupColliders(SkinnedMeshRenderer keyboardMesh, HPUIStaticContinuousInteractable hpuiStaticContinuousInteractable)
        {
            targetMesh = keyboardMesh;
            tempMesh = new Mesh(); 
            targetMesh.BakeMesh(tempMesh, true);
            
            if (vertexRemapData == null)
            {
                throw new ArgumentException("Missing Vertex Remap Data Asset! Create a new one or provide an existing one!");
            }
            
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);
            
            remapped_vertices_data = new NativeArray<int>(vertexRemapData.RemappedVertices, Allocator.Persistent);
            
            meshXResolution = hpuiStaticContinuousInteractable.MeshXResolution;
            if (vertices.Count % meshXResolution != 0)
            {
                throw new Exception("Total vertex count doesn't divide properly with X mesh resolution!");
            }
            meshYRes = vertices.Count / meshXResolution;
            float xWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[1]]);
            float yWidth = Vector3.Distance(vertices[remapped_vertices_data[0]], vertices[remapped_vertices_data[meshXResolution]]);
            float offsetX = xWidth * meshXResolution * 0.5f;
            float offsetY = yWidth * meshYRes * 0.5f;
            Transform[] colliderTransforms = new Transform[vertices.Count];
            Transform meshTransform = targetMesh.gameObject.transform;
            InitCollidersState(meshTransform, xWidth, yWidth, colliderTransforms, offsetX, offsetY);
            colliderObjects = new TransformAccessArray(colliderTransforms);
            vertices_native = new NativeArray<Vector3>(vertices.ToArray(), Allocator.Persistent);
            normals_native = new NativeArray<Vector3>(normals.ToArray(), Allocator.Persistent);
            generatedColliders = true;

            return colliderCoords.Keys.ToList();
        }

        private void InitCollidersState(Transform meshTransform, float xWidth, float yWidth, Transform[] colliderTransforms
            , float offsetX, float offsetY)
        {
            for (int i = 0; i < remapped_vertices_data.Length; i++)
            {
                int x = i % meshXResolution;
                int y = i / meshXResolution;
                GameObject colliderGameObject = new GameObject();
                Collider col = colliderGameObject.AddComponent<BoxCollider>();
                colliderGameObject.name = "X: " + x + "; Y: " + y + ";";
                colliderGameObject.transform.parent = meshTransform;
                colliderGameObject.transform.localPosition = vertices[remapped_vertices_data[i]];
                Vector3 targetScale = Vector3.one * scaleFactor;
                targetScale.x = xWidth;
                targetScale.y = yWidth;
                colliderGameObject.transform.localScale = targetScale;
                colliderGameObject.transform.localRotation = Quaternion.LookRotation(normals[remapped_vertices_data[i]]);
                colliderTransforms[i] = colliderGameObject.transform;
                Vector2 coords = new Vector2((float)((meshXResolution - 1) - x)/meshXResolution, (float)((meshYRes - 1) - y)/meshYRes);
                colliderCoordsNormalisedAndFlipped.Add(col, coords);
                coords = new Vector2(xWidth * x - offsetX, yWidth * y - offsetY);
                colliderCoords.Add(col, coords);
            }
        }

        public Vector2 GetSurfacePointForCollider(Collider col, bool flippedAndNormalisedCoords = false)
        {
            Vector2 coordsForCol;
            if (flippedAndNormalisedCoords)
            {
                if (!colliderCoordsNormalisedAndFlipped.TryGetValue(col, out coordsForCol))
                {
                    throw new ArgumentException("Unknown {collider.name}");
                }

            }
            else
            {
                if (!colliderCoords.TryGetValue(col, out coordsForCol))
                {
                    throw new ArgumentException("Unknown {collider.name}");
                }

            }
            return coordsForCol;
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
                MaxX = meshXResolution,
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


        public void RemapVertices()
        {
            targetMesh = GetComponent<HPUIStaticContinuousInteractable>().StaticHPUIMesh;
            tempMesh = new Mesh(); 
            if(targetMesh==null)
            {
                Debug.LogError("Please assign static mesh to the HPUI Static Continuous Interactable Component first!");
                return;
            }
            targetMesh.BakeMesh(tempMesh, true);
            GetRectifiedIndices(vertexRemapData, tempMesh, flipOrderForRecompute);
        }
        
        private static void GetRectifiedIndices(VertexRemapData remapData, Mesh mesh, bool flipOrder)
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
            remapData.RemappedVertices = flipOrder ? correctedIndices.Reverse().ToArray() : correctedIndices;
        }
    }
}