using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;

namespace ubc.ok.ovilab.HPUI.Core.DeformableSurfaceDisplay
{
    public class SkinnedMeshCoordinateManager : CoordinateManager
    {
        public Transform meshRoot;

        private bool calibrationDone = false;
        public byte numberOfBonesPerVertex = 3;

        #region unity functions
        void Start()
        {
            calibrationDone = false;
            SetupKeypoints();
        }

        void Update()
        {
        
        }
        #endregion

        #region overrides
        public override bool isCalibrated()
        {
            return calibrationDone;
        }

        public override void Calibrate()
        {
            calibrationDone = false;
            //0: height, 1: width
            float[] dimensions = new float[2];
                
            dimensions[0] = height;
            dimensions[1] = width;
            planeMeshGenerator.CreateFlatMesh(dimensions);
            Mesh mesh = planeMeshGenerator.mesh;
            mesh.RecalculateNormals();

            SkinnedMeshRenderer renderer = meshRoot.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                renderer = meshRoot.gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            ComputeMeshBoneWeights(mesh, renderer);
            calibrationDone = true;
        }
        #endregion

        private void ComputeMeshBoneWeights(Mesh mesh, SkinnedMeshRenderer renderer)
        {
            // Create a Transform and bind pose for two bones
            List<Transform> bones = new List<Transform>();
            List<Matrix4x4> bindPoses = new List<Matrix4x4>();

            // Setting up bones and bindPose
            foreach (GameObject obj in keypointObjects)
            {
                Transform t = obj.transform;
                bones.Add(t);
                bindPoses.Add(t.worldToLocalMatrix * meshRoot.localToWorldMatrix);
            }

            // Create an array that describes the number of bone weights per vertex
            byte[] bonesPerVertex = Enumerable.Repeat<byte>(numberOfBonesPerVertex, planeMeshGenerator.vertices.Count).ToArray();

            // Create a array with one BoneWeight1 struct for each of the <numberofbonespervertex> bone weights
            List<BoneWeight1> weights = new List<BoneWeight1>();

            for (int i = 0; i < planeMeshGenerator.vertices.Count; i++)
            {
                Vector3 vertexPos = planeMeshGenerator.transformAnchor.TransformPoint(planeMeshGenerator.vertices[i]);

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
            var bonesPerVertexArray = new NativeArray<byte>(bonesPerVertex, Allocator.Temp);
            var weightsArray = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp);

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
    }
}
