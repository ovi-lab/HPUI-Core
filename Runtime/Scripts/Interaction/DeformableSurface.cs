using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// Main class to generate deformable meshes that use Unity's <see cref="SkinnedMeshRenderer"/>.
    /// </summary>
    public static class DeformableSurface
    {
        /// <summary>
        /// The main method to generate mesh. This will generate a
        /// mesh to match the parameters passed and set up the <see
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
        /// <param name="sigmaFactor"> A multiplier for sigma. Sigma controls
        /// locality. Smaller sigma results in more local influence. Sigma is the
        /// sigmaFctor * max(x_size, y_size) </param>
        public static void GenerateMesh(float x_size, float y_size, int x_divisions, int y_divisions, float surfaceOffset, MeshFilter filter, List<Transform> bones, byte numberOfBonesPerVertex, float sigmaFactor)
        {
            Mesh mesh;
            List<Vector3> vertices;
            Transform surfaceRootTransform = filter.transform;

            GenerateMeshBottomMiddleOrigin(x_size, y_size, surfaceOffset, x_divisions, y_divisions, out mesh, out vertices);
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

            byte[] bonesPerVertex;
            List<BoneWeight1> weights;
            float sigma = Mathf.Max(x_size, y_size) * sigmaFactor;
            GenerateGeodesicBoneWeights(mesh, vertices, surfaceRootTransform, bones, numberOfBonesPerVertex, sigma, out bonesPerVertex, out weights);

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

        // KLUDGE: GenerateGeodesicBoneWeights and its helpers are mostly LLM generated and hasn't been fully verfied.
        /// <summary>
        /// small Edge struct instead of named tuples.
        /// </summary>
        private struct Edge
        {
            public int Target;
            public float Length;

            public Edge(int t, float l)
            {
                Target = t;
                Length = l;
            }
        }

        /// <summary>
        /// Small binary min-heap for Dijkstra
        /// </summary>
        private struct HeapNode
        {
            public int Vertex;
            public float Dist;

            public HeapNode(int v, float d)
            {
                Vertex = v;
                Dist = d;
            }
        }

        /// <summary>
        /// Build adjacency graph (undirected) using mesh.triangles and world-space vertex positions.
        /// </summary>
        private static List<Edge>[] BuildGraph(Vector3[] vertsWorld, int[] triangles)
        {
            int verticesCount = vertsWorld.Length;
            List<Edge>[] graph = new List<Edge>[verticesCount];
            for (int i = 0; i < verticesCount; i++) graph[i] = new List<Edge>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];

                void AddEdge(int u, int v)
                {
                    float len = Vector3.Distance(vertsWorld[u], vertsWorld[v]);
                    List<Edge> lst = graph[u];
                    // avoid duplicates
                    bool exists = false;
                    for (int i = 0; i < lst.Count; i++)
                    {
                        if (lst[i].Target == v)
                        {
                            exists = true; break;
                        }
                    }
                    if (!exists)
                    {
                        lst.Add(new Edge(v, len));
                    }
                }

                AddEdge(a, b); AddEdge(b, a);
                AddEdge(b, c); AddEdge(c, b);
                AddEdge(c, a); AddEdge(a, c);
            }

            return graph;
        }

        /// <summary>
        /// Min-heap implementation to use with Dijkstra
        /// </summary>
        private class MinHeap
        {
            private List<HeapNode> data = new List<HeapNode>();
            public int Count => data.Count;

            public void Push(HeapNode n)
            {
                data.Add(n);
                int i = data.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (data[p].Dist <= data[i].Dist)
                    {
                        break;
                    }
                    HeapNode tmp = data[p]; data[p] = data[i]; data[i] = tmp;
                    i = p;
                }
            }

            public HeapNode Pop()
            {
                HeapNode ret = data[0];
                int last = data.Count - 1;
                data[0] = data[last];
                data.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = i * 2 + 2, smallest = i;
                    if (l < data.Count && data[l].Dist < data[smallest].Dist)
                    {
                        smallest = l;
                    }
                    if (r < data.Count && data[r].Dist < data[smallest].Dist)
                    {
                        smallest = r;
                    }
                    if (smallest == i)
                    {
                        break;
                    }
                    HeapNode tmp = data[i]; data[i] = data[smallest]; data[smallest] = tmp;
                    i = smallest;
                }
                return ret;
            }
        }

        /// <summary>
        /// Dijkstra from a source vertex on the graph (returns distances, unreachable = +inf)
        /// </summary>
        private static float[] Dijkstra(List<Edge>[] graph, int source)
        {
            int n = graph.Length;
            float[] dist = new float[n];
            for (int i = 0; i < n; i++)
            {
                dist[i] = float.PositiveInfinity;
            }
            dist[source] = 0f;

            MinHeap heap = new MinHeap();
            heap.Push(new HeapNode(source, 0f));
            bool[] visited = new bool[n];

            while (heap.Count > 0)
            {
                HeapNode node = heap.Pop();
                int u = node.Vertex;
                if (visited[u])
                {
                    continue;
                }
                visited[u] = true;

                foreach (Edge edge in graph[u])
                {
                    int v = edge.Target;
                    float w = edge.Length;
                    float nd = dist[u] + w;
                    if (nd < dist[v])
                    {
                        dist[v] = nd;
                        heap.Push(new HeapNode(v, nd));
                    }
                }
            }

            return dist;
        }

        /// <summary>
        /// Generate geodesic-based bone weights. Outputs bonesPerVertex array and a flattened list of BoneWeight1 per-vertex (top-K ordering)
        /// </summary>
        private static void GenerateGeodesicBoneWeights(Mesh mesh, List<Vector3> verticesLocal, Transform surfaceRootTransform,
                                                        List<Transform> bones, byte numberOfBonesPerVertex, float sigma,
                                                        out byte[] bonesPerVertexOut, out List<BoneWeight1> weightsOut)
        {
            int verticesCount = verticesLocal.Count;
            Vector3[] vertsWorld = new Vector3[verticesCount];
            for (int i = 0; i < verticesCount; i++)
            {
                vertsWorld[i] = surfaceRootTransform.TransformPoint(verticesLocal[i]);
            }

            int[] triangles = mesh.triangles;
            List<Edge>[] graph = BuildGraph(vertsWorld, triangles);

            int boneCount = bones.Count;
            float[][] boneDists = new float[boneCount][];
            for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
            {
                Vector3 bonePos = bones[boneIdx].position;
                int seed = 0;
                float best = float.PositiveInfinity;
                for (int vertexIdx = 0; vertexIdx < verticesCount; vertexIdx++)
                {
                    float sqrtDist = (vertsWorld[vertexIdx] - bonePos).sqrMagnitude;
                    if (sqrtDist < best)
                    {
                        best = sqrtDist;
                        seed = vertexIdx;
                    }
                }
                boneDists[boneIdx] = Dijkstra(graph, seed);
            }

            float sigmaSqFactor = (sigma > 0f) ? (2f * sigma * sigma) : -1f;
            float eps = 1e-6f;

            bonesPerVertexOut = Enumerable.Repeat<byte>(numberOfBonesPerVertex, verticesCount).ToArray();
            weightsOut = new List<BoneWeight1>(verticesCount * numberOfBonesPerVertex);

            for (int vertexIdx = 0; vertexIdx < verticesCount; vertexIdx++)
            {
                List<KeyValuePair<int, float>> perBone = new List<KeyValuePair<int, float>>(boneCount);
                for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                {
                    float dist = boneDists[boneIdx][vertexIdx];
                    float weight = 0f;
                    if (float.IsPositiveInfinity(dist))
                    {
                        weight = 0f;
                    }
                    else if (sigma > 0f)
                    {
                        weight = Mathf.Exp(-(dist * dist) / sigmaSqFactor);
                    }
                    else
                    {
                        weight = 1f / (dist + eps);
                    }
                    perBone.Add(new KeyValuePair<int, float>(boneIdx, weight));
                }

                List<KeyValuePair<int, float>> top = perBone.OrderByDescending(kv => kv.Value).Take(numberOfBonesPerVertex).ToList();
                float sum = top.Sum(kv => kv.Value);

                if (sum <= 0f)
                {
                    // fallback: assign closest bone (smallest geodesic distance)
                    int closestBone = 0;
                    float bestDist = float.PositiveInfinity;
                    for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                    {
                        float dist = boneDists[boneIdx][vertexIdx];
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            closestBone = boneIdx;
                        }
                    }
                    for (int k = 0; k < numberOfBonesPerVertex; k++)
                    {
                        BoneWeight1 bw = new BoneWeight1()
                        {
                            boneIndex = (k == 0) ? closestBone : 0,
                            weight = (k == 0) ? 1f : 0f
                        };
                        weightsOut.Add(bw);
                    }
                    continue;
                }

                foreach (KeyValuePair<int, float> kv in top)
                {
                    BoneWeight1 bw = new BoneWeight1()
                    {
                        boneIndex = kv.Key,
                        weight = kv.Value / sum
                    };
                    weightsOut.Add(bw);
                }
            }
        }
    }
}
