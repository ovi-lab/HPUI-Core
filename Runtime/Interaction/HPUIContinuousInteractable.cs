using System.Collections;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Tracking;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIContinuousInteractable: HPUIBaseInteractable
    {
        //TODO: make following configs an asset
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
        [Tooltip("(Optional) the MeshFilter of the corresponding SkinnedMeshRenderer. If not set, will create a child object with the MeshFilter and SkinnedMeshRenderer.")]
	public MeshFilter filter;

        /// <inheritdoc />
        public override Vector2 boundsMax { get => surfaceCollidersManager?.boundsMax ?? Vector2.zero; }

        /// <inheritdoc />
        public override Vector2 boundsMin { get => surfaceCollidersManager?.boundsMin ?? Vector2.zero; }

        [SerializeField]
        private HPUIContinuousSurfaceEvent continuousSurfaceCreatedEvent = new HPUIContinuousSurfaceEvent();

        /// <summary>
        /// Event triggered when surface gets created.
        /// </summary>
        public HPUIContinuousSurfaceEvent ContinuousSurfaceEvent
        {
            get => continuousSurfaceCreatedEvent;
            set => continuousSurfaceCreatedEvent = value;
        }

        public int x_divisions { get; private set; }
        private List<Transform> keypointsCache;
        private DeformableSurfaceCollidersManager surfaceCollidersManager;
        private GameObject collidersRoot;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            gameObject.SetActive(false);
        }

        /// <inheritdoc />
        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            // NOTE: This also should allow the XRPokeFilter to work with ContinuousInteractable, I think!
            return GetDistance(interactor.GetAttachTransform(this).transform.position).collider.transform;
        }

        /// <inheritdoc />
        protected override void ComputeSurfaceBounds()
        {}

        /// <inheritdoc />
        public override Vector2 ComputeInteractorPostion(IXRInteractor interactor)
        {
            // TODO: add value from pointOnPlane (the point on the collider)
            DistanceInfo distanceInfo = GetDistanceOverride(this, interactor.GetAttachTransform(this).position);
            // Vector3 closestPointOnCollider = distanceInfo.point;
            // Vector2 pointOnPlane = ComputeTargetPointOnInteractablePlane(closestPointOnCollider, GetAttachTransform(interactor));
            return surfaceCollidersManager.GetSurfacePointForCollider(distanceInfo.collider);
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
                jointFollower.SetData(new JointFollowerData(Handedness, jointID, 0, 0, 0));

                Transform keypoint = obj.transform;
                keypoint.parent = this.transform;
                keypointTransforms.Add(keypoint);
            }

            if (filter == null)
            {
                GameObject skinNode = new GameObject("SkinNode");
                skinNode.transform.parent = this.transform;
                filter = skinNode.AddComponent<MeshFilter>();
                skinNode.transform.localPosition = Vector3.zero;
                skinNode.transform.localRotation = Quaternion.identity;
            }

            return keypointTransforms;
        }

        /// <summary>
        /// Configure the continuous surface.
        /// </summary>
        public void Configure()
        {
            colliders.Clear();

            if (keypointsCache != null)
            {
                for (int i = 0; i < keypointsCache.Count; ++i)
                {
                    if (keypointsCache[i] != transform)
                    {
                        Destroy(keypointsCache[i].gameObject);
                    }
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
            yield return new WaitForSeconds(0.1f);
            if (filter.mesh != null)
            {
                Destroy(filter.mesh);
            }

            if (collidersRoot != null)
            {
                Destroy(collidersRoot);
            }

            float step_size = y_size / y_divisions;
	    x_divisions = (int)(x_size / step_size);

            DeformableSurface.GenerateMesh(x_size, y_size, x_divisions, y_divisions, offset, filter, keypoints, numberOfBonesPerVertex);

            if (defaultMaterial != null)
            {
                filter.GetComponent<Renderer>().material = defaultMaterial;
            }

            collidersRoot = new GameObject("CollidersRoot");
            collidersRoot.transform.parent = this.transform;
            collidersRoot.transform.localPosition = Vector3.zero;
            collidersRoot.transform.localRotation = Quaternion.identity;

            surfaceCollidersManager = filter.GetComponent<DeformableSurfaceCollidersManager>();
            if (surfaceCollidersManager == null)
            {
                surfaceCollidersManager = filter.gameObject.AddComponent<DeformableSurfaceCollidersManager>();
            }

            List<Collider> generatedColliders = surfaceCollidersManager.SetupColliders(collidersRoot.transform);

            colliders.AddRange(generatedColliders);

            // Forcing regsitration of interactable to run
            OnDisable();
            OnEnable();

            continuousSurfaceCreatedEvent?.Invoke(new HPUIContinuousSurfaceCreatedEventArgs(this));
        }
    }
}
