using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Main class to generate deformable meshes that use Unity's <see cref="SkinnedMeshRenderer"/>.
    /// </summary>
    public static class DeformableSurface
    {
        /// <summary>
        /// The main method to generate mesh. This will generate a
        /// mesh to match the parameters passed and setup the <see
        /// cref="SkinnedMeshRenderer"/>.
        /// </summary>
        /// <param name="x_size">
        /// The size along the abduction-adduction axis of the fingers (x-axis of joints).</param>
        /// <param name="y_size">
        /// The size along the flexion-extension axis of the fingers (z-axis of joints).</param>
        /// <param name="x_divisions">
        /// The number of subdivisions along the abduction-adduction
        /// axis of the fingers.</param>
        /// <param name="y_divisions">
        /// The number of subdivisions along the flexion-extension
        /// axis of the fingers. </param>
        /// <param name="surfaceOffset">
        /// Offset from the center of the joints (as reported by <see
        /// cref="XRHands"/>) towards the palmer side of the
        /// hand.</param>
        /// <param name="filter">
        /// The mesh filter to which the new mesh will be added
        /// to. The gameObject of this should also have a
        /// <see cref="SkinnedMeshRenderer"/> attached to it; if not, a new
        /// SkinnedMeshRenderer will be added. The SkinnedMeshRenderer
        /// will be using the generated mesh.</param>
        /// <param name="bones">
        /// The bones that will be used for the <see cref="SkinnedMeshRenderer"/>.</param>
        /// <param name="numberOfBonesPerVertex">
        /// The number of bones to use per vertex in the <see cref="SkinnedMeshRenderer"/>.</param>
        public static void GenerateMesh(float x_size, float y_size, int x_divisions, int y_divisions, float surfaceOffset, MeshFilter filter, List<Transform> bones, byte numberOfBonesPerVertex)
        {
            Mesh mesh;
            List<Vector3> vertices;
            Transform surfaceRootTransform = filter.transform;

            GenerateMeshBottomMiddleOrigin(x_size,y_size, surfaceOffset, x_divisions, y_divisions, out mesh, out vertices);
            filter.mesh = mesh;
            filter.mesh.MarkDynamic();

            SkinnedMeshRenderer renderer = surfaceRootTransform.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                renderer = surfaceRootTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            // Create a Transform and bind pose for two bones
            List<Matrix4x4> bindPoses = new List<Matrix4x4>();

            // Setting up bones and bindPose
            foreach (Transform t in bones)
            {
                bindPoses.Add(t.worldToLocalMatrix * surfaceRootTransform.localToWorldMatrix);
            }

            // Create an array that describes the number of bone weights per vertex
            byte[] bonesPerVertex = Enumerable.Repeat<byte>(numberOfBonesPerVertex, vertices.Count).ToArray();

            // Create a array with one BoneWeight1 struct for each of the <numberofbonespervertex> bone weights
            List<BoneWeight1> weights = new List<BoneWeight1>();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertexPos = surfaceRootTransform.TransformPoint(vertices[i]);

                // The weights are the inverse of the distance from the vertex to a bone (1/dist)
                List<(int idx, float weight)> vals = bones
                    .Select((t, idx) => (idx, (1 / (t.position - vertexPos).magnitude)))
                    .OrderBy(el => el.Item2) // in ascending order
                    .Reverse()
                    .Take(numberOfBonesPerVertex)
                    .ToList();

                float normalizingFactor = vals.Select(x => x.weight).Sum();

                foreach ((int idx, float weight) item in vals)
                {
                    BoneWeight1 bw = new BoneWeight1()
                    {
                        boneIndex = item.idx,
                        weight = item.weight / normalizingFactor
                    };
                    weights.Add(bw);
                }
            }

            // Create NativeArray versions of the two arrays
            NativeArray<byte> bonesPerVertexArray = new NativeArray<byte>(bonesPerVertex, Allocator.Temp);
            NativeArray<BoneWeight1> weightsArray = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp);

            // Set the bone weights on the mesh
            mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
            bonesPerVertexArray.Dispose();
            weightsArray.Dispose();

            // Assign the bind poses to the mesh
            mesh.bindposes = bindPoses.ToArray();

            // Assign the bones and the mesh to the renderer
            renderer.bones = bones.ToArray();
            renderer.sharedMesh = mesh;
        }

        /// <summary>
        /// Generate a mesh anchored at the bottom middle of the mesh.
        /// </summary>
        public static void GenerateMeshBottomMiddleOrigin(float x_size, float y_size, float surfaceOffset, int x_divisions, int y_divisions, out Mesh mesh, out List<Vector3> vertices)
        {
            mesh = new Mesh();

            vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int k = 0; k < y_divisions; k++)
            {

                //for (int i = -x_divisions/2; i < x_divisions/2; i++)
                for (int i = 0; i < x_divisions; i++)
                {
                    vertices.Add(new Vector3(x_size * ((i - ((float)x_divisions / 2.0f)) / (float)x_divisions), surfaceOffset, y_size * (k / (float)y_divisions)));
                    normals.Add(Vector3.down);

                    uvs.Add(new Vector2(1 - k / (float)(y_divisions - 1), i / (float)(x_divisions - 1)));
                }
            }

            var triangles = new List<int>();

            for (int i = 0; i < (y_divisions - 1) * (x_divisions) - 1; i++)
            {
                if ((i + 1) % (x_divisions) == 0)
                {
                    continue;
                }

                triangles.AddRange(new List<int>()
                {
                    i,i+x_divisions,i+x_divisions+1,
                    i,i+x_divisions+1,i+1
                });
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }
}
