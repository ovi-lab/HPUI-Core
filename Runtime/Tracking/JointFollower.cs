using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// A component that makes the object follow a hand joint
    /// </summary>
    public class JointFollower: MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Joint follower data to use for this Joint.")]
        private JointFollowerDatumProperty jointFollowerData = new JointFollowerDatumProperty(new JointFollowerData());
        /// <summary>
        /// Joint follower data to use for this Joint.
        /// </summary>
        public JointFollowerDatumProperty JointFollowerDatumProperty { get => jointFollowerData; set => jointFollowerData = value; }

        [SerializeField]
        [Tooltip("(optional) The target transform to use. If not set, use this transform.")]
        private Transform targetTransform;
        /// <summary>
        /// The target transform to use. If not set, use this transform.
        /// </summary>
        public Transform TargetTransform { get => targetTransform; set => targetTransform = value; }

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
            if (TargetTransform == null)
            {
                TargetTransform = transform;
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
        public void SetData(JointFollowerData jointFollowerData)
        {
            this.jointFollowerData.Value = jointFollowerData;
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
                    if (jointFollowerData.Value.handedness == Handedness.Left)
                    {
                        ProcessJointData(subsystem.leftHand);
                    }
                    else if (jointFollowerData.Value.handedness == Handedness.Right)
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
            JointFollowerData jointFollowerDataValue = jointFollowerData.Value;
            XRHandJoint mainJoint = hand.GetJoint(jointFollowerDataValue.jointID);
            bool mainPoseSuccess = mainJoint.TryGetPose(out Pose mainJointPose);
            bool mainRadiusSuccess = mainJoint.TryGetRadius(out float mainRadius);

            XRHandJoint secondJoint;
            bool secondPoseSuccess = false;
            Pose secondJointPose = default;
            if (jointFollowerDataValue.useSecondJointID)
            {
                secondJoint = hand.GetJoint(jointFollowerDataValue.secondJointID);
                secondPoseSuccess = secondJoint.TryGetPose(out secondJointPose);
            }

            if (mainRadiusSuccess)
            {
                cachedRadius = mainRadius;
            }
            else if (cachedRadius == 0)
            {
                cachedRadius = jointFollowerDataValue.defaultJointRadius;
            }

            if (mainPoseSuccess && (!jointFollowerDataValue.useSecondJointID || secondPoseSuccess))
            {
                Vector3 poseForward = mainJointPose.forward;
                Vector3 jointPlaneOffset;
                if (jointFollowerDataValue.offsetAngle == 0 || jointFollowerDataValue.offsetAsRatioToRadius == 0)
                {
                    jointPlaneOffset = -mainJointPose.up;
                }
                else
                {
                    jointPlaneOffset = Quaternion.AngleAxis(jointFollowerDataValue.offsetAngle, poseForward) * -mainJointPose.up;
                }

                Vector3 jointLongitudianlOffset = secondPoseSuccess ? (secondJointPose.position - mainJointPose.position) * jointFollowerDataValue.longitudinalOffset : poseForward * jointFollowerDataValue.longitudinalOffset;

                TargetTransform.rotation = Quaternion.LookRotation(poseForward, jointPlaneOffset);
                TargetTransform.position = mainJointPose.position + jointPlaneOffset * (cachedRadius * jointFollowerDataValue.offsetAsRatioToRadius) + jointLongitudianlOffset;
            }
        }
    }
}
