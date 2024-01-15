using System.Collections.Generic;
using ubco.ovilab.HPUI.Utils;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// A component that makes the object follow a hand joint
    /// </summary>
    public class JointFollower: MonoBehaviour
    {
        [Tooltip("The handedness of the joint to follow")]
        public Handedness handedness;
        [Tooltip("The joint to follow.")]
        public XRHandJointID jointID;
        [Tooltip("Should a second joint be used. If `useSecondJointID` is true, offsetAlongJoint behaves differently.")]
        public bool useSecondJointID;
        [Tooltip("Second joint to use as reference. If `useSecondJointID` is true, offsetAlongJoint behaves differently.")]
        [ConditionalField("useSecondJointID")]
        public XRHandJointID secondJointID;
        [Tooltip("Default joint radius to use when joint radius is not provided by XR Hands. In unity units.")]
        public float defaultJointRadius = 0.01f;

        [Tooltip("(optional) The target transform to use. If not set, use this transform.")]
        public Transform targetTransform;

        [Tooltip("The offset angle.")][SerializeField]
        public float offsetAngle = 0f;
        [Tooltip("The offset as a ratio of the joint radius.")][SerializeField]
        public float offsetAsRatioToRadius = 1f;
        [Tooltip("The offset along joint (the joint's up) if no secondJoint is set. Otherwise, the position along joint as a ratio to the distance between jointID and secondJointID. In unity units.")]
        [SerializeField]
        public float longitudinalOffset = 0f;

        private float cachedRadius = 0f;
        private XRHandSubsystem handSubsystem;

        /// <inheritdoc />
        protected void Update()
        {
            if (handSubsystem != null && handSubsystem.running)
            {
                return;
            }

            List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);
            bool foundRunningHandSubsystem = false;
            for (var i = 0; i < handSubsystems.Count; ++i)
            {
                XRHandSubsystem handSubsystem = handSubsystems[i];
                if (handSubsystem.running)
                {
                    UnsubscribeHandSubsystem();
                    this.handSubsystem = handSubsystem;
                    foundRunningHandSubsystem = true;
                    break;
                }
            }

            if (!foundRunningHandSubsystem)
            {
                return;
            }

            SubscribeHandSubsystem();
        }

        /// <inheritdoc />
        protected void OnEnable()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            SubscribeHandSubsystem();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
            UnsubscribeHandSubsystem();
            if (handSubsystem != null)
            {
                handSubsystem = null;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            // Reset cachedradius when anything changes on the editor
            cachedRadius = 0;
        }

        /// <summary>
        /// Subscribe to events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        private void SubscribeHandSubsystem()
        {
            if (handSubsystem == null)
                return;

            handSubsystem.updatedHands += OnUpdatedHands;
        }

        /// <summary>
        /// Unsubscribe from events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        private void UnsubscribeHandSubsystem()
        {
            if (handSubsystem == null)
                return;

            handSubsystem.updatedHands -= OnUpdatedHands;
        }

        /// <summary>
        /// Set the parameters of this JointFollower.
        /// </summary>
        public void SetParams(Handedness handedness, XRHandJointID jointID, float offsetAngle, float offsetAsRationToRadius, float longitudinalOffset)
        {
            this.handedness = handedness;
            this.jointID = jointID;
            this.offsetAngle = offsetAngle;
            this.offsetAsRatioToRadius = offsetAsRationToRadius;
            this.longitudinalOffset = longitudinalOffset;
        }

        private void OnUpdatedHands(XRHandSubsystem subsystem,
                                    XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
                                    XRHandSubsystem.UpdateType updateType)
        {
            switch (updateType)
            {
                case XRHandSubsystem.UpdateType.Dynamic:
                    // Update game logic that uses hand data
                    break;
                case XRHandSubsystem.UpdateType.BeforeRender:
                    if (handedness == Handedness.Left)
                    {
                        ProcessJointData(subsystem.leftHand);
                    }
                    else if (handedness == Handedness.Right)
                    {
                        ProcessJointData(subsystem.rightHand);
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply data recieved to the transform.
        /// </summary>
        private void ProcessJointData(XRHand hand)
        {
            XRHandJoint mainJoint = hand.GetJoint(jointID);
            bool mainPoseSuccess = mainJoint.TryGetPose(out Pose mainJointPose);
            bool mainRadiusSuccess = mainJoint.TryGetRadius(out float mainRadius);

            XRHandJoint secondJoint;
            bool secondPoseSuccess = false;
            Pose secondJointPose = default;
            if (useSecondJointID)
            {
                secondJoint = hand.GetJoint(secondJointID);
                secondPoseSuccess = secondJoint.TryGetPose(out secondJointPose);
            }

            if (mainRadiusSuccess)
            {
                cachedRadius = mainRadius;
            }
            else if (cachedRadius == 0)
            {
                cachedRadius = defaultJointRadius;
            }

            if (mainPoseSuccess && (!useSecondJointID || secondPoseSuccess))
            {
                Vector3 poseForward = mainJointPose.forward;
                Vector3 jointPlaneOffset;
                if (offsetAngle == 0 || offsetAsRatioToRadius == 0)
                {
                    jointPlaneOffset = -mainJointPose.up;
                }
                else
                {
                    jointPlaneOffset = Quaternion.AngleAxis(offsetAngle, poseForward) * -mainJointPose.up;
                }

                Vector3 jointLongitudianlOffset = secondPoseSuccess ? (secondJointPose.position - mainJointPose.position) * longitudinalOffset : poseForward * longitudinalOffset;

                targetTransform.rotation = Quaternion.LookRotation(poseForward, jointPlaneOffset);
                targetTransform.position = mainJointPose.position + jointPlaneOffset * (cachedRadius * offsetAsRatioToRadius) + jointLongitudianlOffset;
            }
        }
    }
}
