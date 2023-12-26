using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// </summary>
    public class HandJointContinuousInteractable: MonoBehaviour
    {
	private MeshFilter filter;

	public int y_divisions = 35;
	public float offset = 0.0005f;
        public float x_size;
        public float y_size;
        public byte numberOfBonesPerVertex = 3;

        public Handedness handedness;
        public List<XRHandJointID> keypointJoints;

        public Material defaultMaterial;

        private List<Transform> SetupKeypoints()
        {
            List<Transform> keypointTransforms = new List<Transform>();
            foreach (XRHandJointID jointID in keypointJoints)
            {
                GameObject obj = new GameObject($"{handedness}_{jointID}");
                JointFollower jointFollower = obj.AddComponent<JointFollower>();
                jointFollower.SetParams(handedness, jointID, 0, 0, 0);

                Transform keypoint = obj.transform;
                keypoint.parent = this.transform;
                keypointTransforms.Add(keypoint);
            }

            GameObject surfaceRoot = keypointTransforms[0].gameObject;
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
            if (filter.mesh != null)
            {
                Destroy(filter.mesh);
            }

            DeformableSurface.GenerateMesh(x_size, y_size, y_divisions, offset, filter, keypoints, numberOfBonesPerVertex);

            if (defaultMaterial != null)
            {
                filter.GetComponent<Renderer>().material = defaultMaterial;
            }
        }

    }
}
