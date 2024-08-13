using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using System;
using System.Linq;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Component to manage the colliders of the deformable surface.
    /// </summary>
    public class DeformableSurfaceCollidersManager: MonoBehaviour
    {
	private NativeArray<Vector3> vertices_native, normals_native; 
        private List<Vector3> normals, vertices;
        private bool generatedColliders;

        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Mesh tempMesh;

        private Vector3 scaleFactor;
        private int maxY, maxX;
        private float gridSize, offset_x, offset_y;
        private TransformAccessArray colliderObjects;

        private Dictionary<Collider, Vector2> colliderCoords;

        public Vector2 boundsMax { get; protected set; }
        public Vector2 boundsMin { get; protected set; }

        /// <inheritdoc />
        private void OnDestroy()
	{
            if (vertices_native.IsCreated)
            {
                vertices_native.Dispose();
            }
            if (normals_native.IsCreated)
            {
                normals_native.Dispose();
            }
            colliderObjects.Dispose();
        }

        /// <inheritdoc />
	private void Update()
	{
            if (!isActiveAndEnabled)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DeformableSurfaceCollidersManager.Update");
	    if (generatedColliders)
	    {
                UpdateColliderPoses();
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Setup and return colliders. A collider will be placed on each vertex of the <see cref="SkinnedMeshRenderer"/>.
        /// </summary>
        public List<Collider> SetupColliders(Transform collidersRootTransform)
        {
            generatedColliders = false;
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }
            if (tempMesh == null)
            {
                tempMesh = new Mesh();
            }

            vertices = new List<Vector3>();
            normals = new List<Vector3>();

            if (vertices_native.IsCreated)
            {
                vertices_native.Dispose();
            }
            if (normals_native.IsCreated)
            {
                normals_native.Dispose();
            }

            // See https://forum.unity.com/threads/get-skinned-vertices-in-real-time.15685/
            skinnedMeshRenderer.BakeMesh(tempMesh, true);
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);

            HPUIContinuousInteractable continuousInteractable = GetComponentInParent<HPUIContinuousInteractable>();

            maxY = vertices_native.Length - continuousInteractable.X_divisions;
            maxX = continuousInteractable.X_divisions;

            List<Collider> colliders = GenerateColliders(vertices, normals, collidersRootTransform, continuousInteractable.X_divisions, continuousInteractable.Y_divisions);

            generatedColliders = true;
            return colliders;
        }

        /// <summary>
        /// Return the (approximate) point on the surface of where the collider is.
        /// The returned Vector2 - (x, z) on the xz-plane. This is relative to the
        /// center of the surface.
        /// </summary>
        public Vector2 GetSurfacePointForCollider(Collider collider)
        {
            if (!colliderCoords.ContainsKey(collider))
            {
                throw new ArgumentException("Unknown {collider.name}");
            }

            return colliderCoords[collider];
        }

        /// <summary>
        /// Generate the colliders for a given set of vertices. The vertices are expected to be the order along x then along y.
        /// The generated colliders will be parented to the rootTransform.
        /// </summary>
	private List<Collider> GenerateColliders(List<Vector3> positions, List<Vector3> _normals, Transform rootTransform, int x_divisions, int y_divisions)
	{
	    var right = positions[1] - positions[0];
	    GameObject colliderObj;
	    scaleFactor = Vector3.zero;

            Transform[] colliderTransforms = new Transform[positions.Count];
            List<Collider> colliders = new List<Collider>();

            colliderCoords = new Dictionary<Collider, Vector2>();

            for(var i = 0; i < positions.Count; i ++)
	    {
                colliderObj = new GameObject();
                Collider collider = colliderObj.AddComponent<BoxCollider>();
                colliders.Add(collider);
                int x = (int)i % x_divisions;
                int y = (int)i / x_divisions;
                colliderObj.transform.name = "X" + x + "-Y" + y;
		// Getting the scale values to set the size of the buttons based on the size of a single square in the generated mesh
		if (scaleFactor == Vector3.zero)
		{
		    Vector3 buttonSize = collider.bounds.size;
		    gridSize = rootTransform.InverseTransformVector(positions[0] - positions[1]).magnitude;
		
		    scaleFactor = colliderObj.transform.localScale;
		    // making them slightly larger to remove the spaces between the pixels
		    scaleFactor.x = (gridSize / buttonSize.x) * 1.05f * rootTransform.lossyScale.x;
		    scaleFactor.z = (gridSize / buttonSize.y) * 1.05f * rootTransform.lossyScale.y;
                    scaleFactor.y = 0.00001f;
                    gridSize = (positions[0] - positions[1]).magnitude;

                    offset_x = gridSize * x_divisions * 0.5f;
                    offset_y = gridSize * y_divisions * 0.5f;
                }
		colliderObj.transform.parent = rootTransform;
		colliderObj.transform.localPosition = positions[i];
		colliderObj.transform.localRotation = Quaternion.identity;
		colliderObj.transform.localScale = scaleFactor;
                colliderTransforms[i] = colliderObj.transform;

                colliderCoords.Add(collider, new Vector2(gridSize * x - offset_x, gridSize * y - offset_y));
            }

	    colliderObjects = new TransformAccessArray(colliderTransforms);

            boundsMax = new Vector2(colliderCoords.Values.Select(v => v.x).Max(), colliderCoords.Values.Select(v => v.y).Max());
            boundsMin = new Vector2(colliderCoords.Values.Select(v => v.x).Min(), colliderCoords.Values.Select(v => v.y).Min());

            return colliders;
        }

        /// <summary>
        /// Update the poses of the colliders to follow the vertices of the <see cref="SkinnedMeshRenderer"/>
        /// </summary>
        protected void UpdateColliderPoses()
        {
            // See https://forum.unity.com/threads/get-skinned-vertices-in-real-time.15685/
            skinnedMeshRenderer.BakeMesh(tempMesh, true);
            tempMesh.GetVertices(vertices);
            tempMesh.GetNormals(normals);

            if (vertices_native.IsCreated)
                vertices_native.CopyFrom(vertices.ToArray());
            else
                vertices_native = new NativeArray<Vector3>(vertices.ToArray(), Allocator.Persistent);

            if (normals_native.IsCreated)
                normals_native.CopyFrom(normals.ToArray());
            else
                normals_native = new NativeArray<Vector3>(normals.ToArray(), Allocator.Persistent);

            // Once the mesh has been deformed, update the locations of the buttons to match the mesh
            DeformedCollidersJob job = new DeformedCollidersJob()
            {
                scaleFactor = scaleFactor,
                gridSize = gridSize,
                maxX = maxX,
                maxY = maxY,
                normals = normals_native,
                vertices = vertices_native
            };

            var jobHandle = job.Schedule(colliderObjects);
            jobHandle.Complete();
        }

        /// <summary>
        /// Job for setting the transforms when the positions are updated.
        /// </summary>
        struct DeformedCollidersJob: IJobParallelForTransform
        {
            private Vector3 right, forward, tempPos, _scaleFactor;
            public Vector3 scaleFactor;
            public float gridSize; 
            public int maxX, maxY;

            [Unity.Collections.ReadOnly]
            public NativeArray<Vector3> vertices;
            [Unity.Collections.ReadOnly]
            public NativeArray<Vector3> normals;
	
            public void Execute(int i, TransformAccess btn)
            {
                tempPos = vertices[i];
                tempPos.z += -0.0002f;
                btn.localPosition = tempPos;

                if (i > maxX)
                    forward = vertices[i] - vertices[i - maxX];
                else
                    forward = vertices[i + maxX] - vertices[i];
		    
                if (i % maxX == 0)
                    right = vertices[i + 1] - vertices[i];
                else
                    right = vertices[i] - vertices[i - 1];

                btn.localRotation = Quaternion.LookRotation(forward, normals[i]);
                _scaleFactor.x = (right.magnitude / gridSize) * scaleFactor.x;
                _scaleFactor.y = (forward.magnitude / gridSize) * scaleFactor.y;
                _scaleFactor.z = scaleFactor.z;
                btn.localScale = _scaleFactor;
            }
        }
    }
}
