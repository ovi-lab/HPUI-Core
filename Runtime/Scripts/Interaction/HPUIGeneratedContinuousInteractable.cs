using System;
using System.Collections;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Core.Tracking;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIGeneratedContinuousInteractable: HPUIBaseInteractable, IHPUIContinuousInteractable
    {
        //TODO: make following configs an asset
        [Space()]
        [Tooltip("The size along the abduction-adduction axis of the fingers (x-axis of joints) in unity units")]
        [SerializeField] private float x_size;
        [Tooltip("The size along the flexion-extension axis of the fingers (z-axis of joints) in unity units.")]
        [SerializeField] private float y_size;
        [Tooltip("The number of subdivisions along the flexion-extension axis of the fingers. The subdivisions along the abduction-adduction axis will be computed from this such that the resulting subdivisions are squares.")]
	[SerializeField] private int y_divisions = 35;
        [Tooltip("Offset from the center of the joints (as reported by XRHands) towards the palmer side of the hand.")]
	[SerializeField] private float offset = 0.0005f;
        [Tooltip("The number of bones to use per vertex in the SkinnedMeshRenderer.")]
        [SerializeField] private byte numberOfBonesPerVertex = 3;
        [Tooltip("The keypoints that will be used for the SkinnedMeshRenderer.")]
        [SerializeField] private List<DeformableSurfaceKeypoint> keypointsData;
        [Tooltip("(Optional) The default material to use for the surface.")]
        [SerializeField] private Material defaultMaterial;
        [Tooltip("(Optional) the MeshFilter of the corresponding SkinnedMeshRenderer. If not set, will create a child object with the MeshFilter and SkinnedMeshRenderer.")]
	[SerializeField] private MeshFilter filter;
        [Tooltip("A multiplier for sigma that controls locality when generating mesh. Smaller sigma results in more local influence.")]
	[SerializeField] private float sigmaFactor = 0.25f;
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

        /// <inheritdoc />
        public float X_size { get => x_size; set => x_size = value; }

        /// <inheritdoc />
        public float Y_size { get => y_size; set => y_size = value; }

        /// <summary>
        /// The number of subdivisions along the abduction-adduction
        /// axis of the fingers. This is computed be computed from the
        /// <see cref="Y_divisions"/> such that the resulting subdivisions
        /// are squares.
        /// </summary>
        public int X_divisions { get; private set; }

        /// <summary>
        /// The number of subdivisions along the flexion-extension
        /// axis of the fingers. The subdivisions along the
        /// abduction-adduction axis will be computed from this such
        /// that the resulting subdivisions are squares.
        /// </summary>
        public int Y_divisions { get => y_divisions; set => y_divisions = value; }

        /// <summary>
        /// Offset from the center of the joints (as reported by XRHands) towards the palmer side of the hand.
        /// </summary>
        public float Offset { get => offset; set => offset = value; }

        /// <summary>
        /// The number of bones to use per vertex in the SkinnedMeshRenderer.
        /// </summary>
        public byte NumberOfBonesPerVertex { get => numberOfBonesPerVertex; set => numberOfBonesPerVertex = value; }

        /// <summary>
        /// The joints that will be used for the SkinnedMeshRenderer.
        /// </summary>
        public List<DeformableSurfaceKeypoint> KeypointsData { get => keypointsData; set => keypointsData = value; }

        /// <summary>
        /// (Optional) The default material to use for the surface.
        /// </summary>
        public Material DefaultMaterial { get => defaultMaterial; set => defaultMaterial = value; }

        /// <summary>
        /// (Optional) the MeshFilter of the corresponding SkinnedMeshRenderer. If not set, will create a child object with the MeshFilter and SkinnedMeshRenderer.
        /// </summary>
        public MeshFilter Filter { get => filter; set => filter = value; }

        /// <summary>
        /// The keypointTransforms used by this continuous interactable;
        /// </summary>
        public List<Transform> KeypointTransforms { get; private set; }

        /// <summary>
        /// A multiplier for sigma that controls locality when generating mesh. Smaller sigma results in more local influence.
        /// </summary>
        public float SigmaFactor { get => sigmaFactor; set => sigmaFactor = value; }

        private DeformableSurfaceCollidersManager surfaceCollidersManager;
        private GameObject collidersRoot;
        private JointFollower jointFollower;

        /// <inheritdoc />
        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            if (interactor == null)
            {
                return this.transform;
            }
            return GetDistance(interactor.GetAttachTransform(this).transform.position).collider.transform;
        }

        /// <inheritdoc />
        protected override void ComputeSurfaceBounds()
        {}

        /// <inheritdoc />
        public override bool ComputeInteractorPosition(IHPUIInteractor interactor, out Vector2 position)
        {
            if (interactor.GetDistanceInfo(this, out DistanceInfo info))
            {
                Vector2 offsetOnCollider = ComputeTargetPointOnTransformXZPlane(info.point, info.collider.transform);
                position = surfaceCollidersManager.GetSurfacePointForCollider(info.collider) + offsetOnCollider;
                return true;
            }
            position = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Set up the list of keypoints to be used for the <see cref="SkinnedMeshRenderer"/>.
        /// </summary>
        internal void SetupKeypoints()
        {
            KeypointTransforms = new List<Transform>();
            foreach (DeformableSurfaceKeypoint joint in KeypointsData)
            {
                Transform keypoint;
                GameObject obj;
                JointFollower jointFollower;
                bool setParent = true;
                switch(joint.keypointType)
                {
                    case DeformableSurfaceKeypoint.KeypointsOptions.JointFollowerData:
                        JointFollowerData jointFollowerData = joint.jointFollowerData;
                        obj = new GameObject($"{Handedness}_{jointFollowerData.jointID}");
                        jointFollower = obj.AddComponent<JointFollower>();
                        jointFollower.SetData(jointFollowerData);
                        keypoint = obj.transform;
                        break;
                    case DeformableSurfaceKeypoint.KeypointsOptions.JointID:
                        XRHandJointID jointID = joint.jointID;
                        obj = new GameObject($"{Handedness}_{jointID}");
                        jointFollower = obj.AddComponent<JointFollower>();
                        jointFollower.SetData(new JointFollowerData(Handedness, jointID, 0, 0, 0));
                        keypoint = obj.transform;
                        break;
                    case DeformableSurfaceKeypoint.KeypointsOptions.Transform:
                        setParent = false;
                        keypoint = joint.jointTransform;
                        break;
                    default:
                        throw new InvalidOperationException("How did this even happen?");
                }

                if (setParent)
                {
                    keypoint.parent = this.transform;
                }
                KeypointTransforms.Add(keypoint);
            }

            if (Filter == null)
            {
                GameObject skinNode = new GameObject("SkinNode");
                skinNode.transform.parent = this.transform;
                Filter = skinNode.AddComponent<MeshFilter>();
                skinNode.transform.localPosition = Vector3.zero;
                skinNode.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Destroy the keypoint objects
        /// </summary>
        internal void ClearKeypointsCache()
        {
            if (KeypointTransforms != null)
            {
                for (int i = 0; i < KeypointTransforms.Count; ++i)
                {
                    if (KeypointTransforms[i] != transform && KeypointsData[i].keypointType != DeformableSurfaceKeypoint.KeypointsOptions.Transform)
                    {
                        Destroy(KeypointTransforms[i].gameObject);
                    }
                }
                KeypointTransforms.Clear();
            }
        }

        /// <summary>
        /// Manually compute the continuous surface. The hand posture
        /// should be held such that all joints are flat when this is
        /// being called.
        /// </summary>
        public void ManualRecompute()
        {
            colliders.Clear();
            ClearKeypointsCache();
            SetupKeypoints();
            StartCoroutine(DelayedExecuteCalibration());
        }


        /// <summary>
        /// Generate the mesh after a short wait.
        /// </summary>
        private IEnumerator DelayedExecuteCalibration()
        {
            yield return new WaitForSeconds(0.1f);
            ExecuteCalibration();
        }

        /// <summary>
        /// Generate the mesh.
        /// </summary>
        internal void ExecuteCalibration()
        {
            if (Filter.mesh != null)
            {
                Destroy(Filter.mesh);
            }

            if (collidersRoot != null)
            {
                Destroy(collidersRoot);
            }

            float step_size = y_size / Y_divisions;
	    X_divisions = (int)(x_size / step_size);

            DeformableSurface.GenerateMesh(x_size, y_size, X_divisions, Y_divisions, Offset, Filter, KeypointTransforms, NumberOfBonesPerVertex, sigmaFactor);

            if (DefaultMaterial != null)
            {
                Filter.GetComponent<Renderer>().material = DefaultMaterial;
            }

            collidersRoot = new GameObject("CollidersRoot");
            collidersRoot.transform.parent = this.transform;
            collidersRoot.transform.localPosition = Vector3.zero;
            collidersRoot.transform.localRotation = Quaternion.identity;

            surfaceCollidersManager = Filter.GetComponent<DeformableSurfaceCollidersManager>();
            if (surfaceCollidersManager == null)
            {
                surfaceCollidersManager = Filter.gameObject.AddComponent<DeformableSurfaceCollidersManager>();
            }

            List<Collider> generatedColliders = surfaceCollidersManager.SetupColliders(collidersRoot.transform);

            colliders.AddRange(generatedColliders);

            // Forcing registration of interactable to run
            OnDisable();
            OnEnable();

            XRPokeFilter pokeFilter = GetComponent<XRPokeFilter>();
            if (pokeFilter != null)
            {
                pokeFilter.enabled = true;
            }
            continuousSurfaceCreatedEvent?.Invoke(new HPUIContinuousSurfaceCreatedEventArgs(this));
        }

    }
}
