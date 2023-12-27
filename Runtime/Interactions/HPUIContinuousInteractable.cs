using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// </summary>
    public class HPUIContinuousInteractable: HPUIBaseInteractable
    {
        [Space()]
        [Header("Continuous surface configuration")]
        [Tooltip("The size along the abduction-adduction axis of the fingers (x-axis of joints).")]
        public float x_size;
        [Tooltip("The size along the flexion-extension axis of the fingers (z-axis of joints).")]
        public float y_size;
        [Tooltip("The number of subdivisions along the flexion-extension axis of the fingers. The subdivisions along the abduction-adduction axis will be computed from this such that the resulting subdivisions are squares.")]
	public int y_divisions = 35;
        [Tooltip("Offset from the center of the joints (as reported by XRHands) towards the palmer side of the hand.")]
	public float offset = 0.0005f;
        [Tooltip("The number of bones to use per vertex in the SkinnedMeshRenderer.")]
        public byte numberOfBonesPerVertex = 3;
        [Tooltip("The joints that will be used for the SkinnedMeshRenderer.")]
        public List<XRHandJointID> keypointJoints;
        [Tooltip("(Optional) The default material to use for the surface.")]
        public Material defaultMaterial;

        public int x_divisions { get; private set; }
        private List<Transform> keypointsCache;
	private MeshFilter filter;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Setup and return the list of keypoints to be used for the <see cref="SkinnedMeshRenderer"/>.
        /// </summary>
        private List<Transform> SetupKeypoints()
        {
            List<Transform> keypointTransforms = new List<Transform>();
            foreach (XRHandJointID jointID in keypointJoints)
            {
                GameObject obj = new GameObject($"{Handedness}_{jointID}");
                JointFollower jointFollower = obj.AddComponent<JointFollower>();
                jointFollower.SetParams(Handedness, jointID, 0, 0, 0);

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

        /// <summary>
        /// Configure the continuous surface.
        /// </summary>
        public void Calibrate()
        {
            colliders.Clear();

            if (keypointsCache != null)
            {
                for (int i = 0; i < keypointsCache.Count; ++i)
                {
                    Destroy(keypointsCache[i].gameObject);
                }
            }
            keypointsCache = SetupKeypoints();
            gameObject.SetActive(true);
            StartCoroutine(DelayedExecuteCalibration(x_size, y_size, keypointsCache));
        }

        /// <summary>
        /// Generate the mesh after a short wait.
        /// </summary>
        private IEnumerator DelayedExecuteCalibration(float x_size, float y_size, List<Transform> keypoints)
        {
            yield return new WaitForSeconds(0.5f);
            if (filter.mesh != null)
            {
                Destroy(filter.mesh);
            }

            float step_size = y_size / y_divisions;
	    x_divisions = (int)(x_size / step_size);

            DeformableSurface.GenerateMesh(x_size, y_size, x_divisions, y_divisions, offset, filter, keypoints, numberOfBonesPerVertex);

            if (defaultMaterial != null)
            {
                filter.GetComponent<Renderer>().material = defaultMaterial;
            }

            DeformableSurfaceCollidersManager surfaceCollidersManager = filter.GetComponent<DeformableSurfaceCollidersManager>();
            if (surfaceCollidersManager == null)
            {
                surfaceCollidersManager = filter.gameObject.AddComponent<DeformableSurfaceCollidersManager>();
            }

            List<Collider> generatedColliders = surfaceCollidersManager.SetupColliders();

            colliders.AddRange(generatedColliders);
        }
    }
}
