using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// </summary>
    public class DeformableSurface: MonoBehaviour
    {
	private MeshFilter filter;
	public Mesh mesh
	{
	    get { return filter.mesh; }
	}
        private GameObject surfaceRoot;

	public int x_divisions { get; private set; }
	public int y_divisions = 35;

	public float zVerticesOffset = -0.0005f;

        public float x_size;
        public float y_size;
        public float step_size { get; private set; }

        public byte numberOfBonesPerVertex = 3;

        public Handedness handedness;
        public List<XRHandJointID> keypointJoints;

        public Material defaultMaterial;

        #region unity functions
        void Start()
        {

        }

        void Update()
        {
        }
        #endregion

        private List<Transform> SetupKeypoints()
        {
            List<Transform> keypointTransforms = new List<Transform>();
            foreach (XRHandJointID jointID in keypointJoints)
            {
                GameObject obj = new GameObject($"{handedness}_{jointID}");
                JointFollower jointFollower = obj.AddComponent<JointFollower>();
                jointFollower.SetParams(handedness, jointID, 0, 0, 0);

                Transform keypoint = obj.transform;
                if (surfaceRoot == null)
                {
                    surfaceRoot = obj;
                    keypoint.parent = this.transform;
                }
                else
                {
                    keypoint.parent = surfaceRoot.transform;
                }

                keypointTransforms.Add(keypoint);
            }

	    filter = surfaceRoot.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = surfaceRoot.AddComponent<MeshFilter>();
            }

            return keypointTransforms;
        }

        public void Calibrate()
        {
            List<Transform> keypoints = SetupKeypoints();
            StartCoroutine(DelayedExecuteCalibration(x_size, y_size, keypoints));
        }

        private IEnumerator DelayedExecuteCalibration(float x_size, float y_size, List<Transform> keypoints)
        {
            yield return new WaitForSeconds(1);
            ExecuteCalibration(x_size, y_size, keypoints);
        }

        private void ExecuteCalibration(float x_size, float y_size, List<Transform> keypoints)
        {
            List<Vector3> vertices = CreateFlatMesh(x_size, y_size);
            mesh.RecalculateNormals();

            SkinnedMeshRenderer renderer = surfaceRoot.transform.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                renderer = surfaceRoot.transform.gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            if (defaultMaterial != null)
            {
                renderer.material = defaultMaterial;
            }

            ComputeMeshBoneWeights(mesh, renderer, vertices, keypoints);
        }

        private void ComputeMeshBoneWeights(Mesh mesh, SkinnedMeshRenderer renderer, List<Vector3> vertices, List<Transform> keypoints)
        {
            // Create a Transform and bind pose for two bones
            List<Transform> bones = new List<Transform>();
            List<Matrix4x4> bindPoses = new List<Matrix4x4>();

            // Setting up bones and bindPose
            foreach (Transform t in keypoints)
            {
                bones.Add(t);
                bindPoses.Add(t.worldToLocalMatrix * surfaceRoot.transform.localToWorldMatrix);
            }

            // Create an array that describes the number of bone weights per vertex
            byte[] bonesPerVertex = Enumerable.Repeat<byte>(numberOfBonesPerVertex, vertices.Count).ToArray();

            // Create a array with one BoneWeight1 struct for each of the <numberofbonespervertex> bone weights
            List<BoneWeight1> weights = new List<BoneWeight1>();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertexPos = surfaceRoot.transform.TransformPoint(vertices[i]);

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

        public List<Vector3> CreateFlatMesh(float x_size, float y_size)
	{
            if (mesh != null)
            {
                Destroy(mesh);
            }
	    if (y_divisions == 0)
            {
                return null;
            }

            // FIXME: Where are these magic values coming from?
	    //x/y scaling factors
	    // float xsf = 1.5f;
	    // float ysf = 1.5f;

	    // xsf /= surface.transform.lossyScale.x;
	    // ysf /= surface.transform.lossyScale.x;

	    // //sizes based on calibration distances on hand model
	    // y_size = ysf*(y_size);
	    // x_size = xsf*(x_size);

	    step_size = y_size / y_divisions;
	    x_divisions = (int)(x_size / step_size);

            Mesh newMesh;
            List<Vector3> vertices;
            GenerateMeshBottomMiddleOrigin(x_divisions, y_divisions, out newMesh, out vertices);
            filter.mesh = newMesh;

            filter.mesh.MarkDynamic();
            return vertices;
        }

	private void GenerateMeshBottomMiddleOrigin(int x_divisions, int y_divisions, out Mesh mesh, out List<Vector3> vertices)
	{
	    mesh = new Mesh();

            vertices = new List<Vector3>();
	    List<Vector3> normals = new List<Vector3>();
	    List<Vector2> uvs = new List<Vector2>();

	    for (int k = 0; k < y_divisions; k++)
	    {

		//for (int i = -x_divisions/2; i < x_divisions/2; i++)
		for (int i = 0; i < x_divisions ; i++)
		{
		    vertices.Add(new Vector3(x_size * ((i- ((float)x_divisions / 2.0f)) / (float)x_divisions), zVerticesOffset,y_size * (k / (float)y_divisions)));
		    normals.Add(Vector3.forward);

		    uvs.Add(new Vector2(1 - k / (float)(y_divisions-1), i / (float)(x_divisions-1)));
		}
	    }

	    var triangles = new List<int>();

	    for (int i = 0; i < (y_divisions-1) * (x_divisions) - 1; i++)
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

	    AlignDisplay();
	}

	void AlignDisplay(bool calcRotation=true)
	{
	    // if (calcRotation)
	    // {
            //     Vector3 forwardDirectionVector, sidewaysDirectionVector;
            //     if (orientationInformation.useStrings)
            //     {
            //         forwardDirectionVector = handCoordinateManager.GetManagedCoord(orientationInformation.forwardVectorNameP2).position - handCoordinateManager.GetManagedCoord(orientationInformation.forwardVectorNameP1).position;
            //         sidewaysDirectionVector = handCoordinateManager.GetManagedCoord(orientationInformation.sideVectorNameP2).position - handCoordinateManager.GetManagedCoord(orientationInformation.sideVectorNameP1).position;
            //     }
            //     else
            //     {
            //         forwardDirectionVector = orientationInformation.forwardVectorTransformP2.position - orientationInformation.forwardVectorTransformP1.position;
            //         sidewaysDirectionVector = orientationInformation.sideVectorTransformP2.position - orientationInformation.sideVectorTransformP1.position;
            //     }
	    //     Vector3 upwardDirectionVector = Vector3.Cross(sidewaysDirectionVector, forwardDirectionVector);

	    //     surface.transform.rotation = Quaternion.LookRotation(upwardDirectionVector, forwardDirectionVector);
	    // }
            // TODO: Set the rotation
	    surfaceRoot.transform.position = surfaceRoot.transform.position;
	}
    }
}
