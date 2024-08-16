using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// A component that makes the object follow a hand joint
    /// </summary>
    public class JointFollower: HandSubsystemSubscriber
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

        /// <inheritdoc />
        public override Handedness Handedness
        {
            get
            {
                if(jointFollowerData.Value != null)
                {
                    return jointFollowerData.Value.handedness;
                }
                else
                {
                    return Handedness.Invalid;
                }
            }
            set => jointFollowerData.Value.handedness = value;
        }

        private float cachedRadius = 0f;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            if (TargetTransform == null)
            {
                TargetTransform = transform;
            }

            base.OnEnable();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            // Reset cachedRadius when anything changes on the editor
            cachedRadius = 0;
        }

        /// <summary>
        /// Subscribe to events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        protected override void SubscribeHandSubsystem()
        {
            if (jointFollowerData.Value == null)
            {
                Debug.LogError($"jointFollowerData is not set. Not subscribing to events");
                return;
            }

            base.SubscribeHandSubsystem();
        }

        /// <summary>
        /// Set the parameters of this JointFollower.
        /// </summary>
        public void SetData(JointFollowerData jointFollowerData)
        {
            this.jointFollowerData.Value = jointFollowerData;
        }

        /// <summary>
        /// Apply data received to the transform.
        /// </summary>
        protected override void ProcessJointData(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags _)
        {
            if (!enabled)
            {
                return;
            }

            XRHand hand;
            if (Handedness.Left == jointFollowerData.Value.handedness)
            {
                hand = subsystem.leftHand;
            }
            else if (Handedness.Right == jointFollowerData.Value.handedness)
            {
                hand = subsystem.rightHand;
            }
            else
            {
                Debug.LogError($"Handedness value in JointFollowerData not valid (got {jointFollowerData.Value.handedness}), disabling JointFollower.");
                this.enabled = false;
                return;
            }

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
                Vector3 position = xrOrigin.transform.position;
                // position.y += xrOrigin.CameraYOffset;
                Pose xrOriginPose = new Pose(position , xrOrigin.transform.rotation);
                mainJointPose = mainJointPose.GetTransformedBy(xrOriginPose);
                if (secondPoseSuccess)
                {
                    secondJointPose = secondJointPose.GetTransformedBy(xrOriginPose);
                }

                SetPose(mainJointPose, secondJointPose, secondPoseSuccess);
            }
        }

        internal void InternalSetPose(Pose mainJointPose, Pose secondJointPose, bool secondPoseSuccess)
        {
            SetPose(mainJointPose, secondJointPose, secondPoseSuccess);
        }

        /// <summary>
        /// This method uses the jointFollowerDataValue and sets the poses.
        /// </summary>
        protected void SetPose(Pose mainJointPose, Pose secondJointPose, bool secondPoseSuccess)
        {
            JointFollowerData jointFollowerDataValue = jointFollowerData.Value;

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

            Vector3 jointLongitudinalOffset = secondPoseSuccess ? (secondJointPose.position - mainJointPose.position) * jointFollowerDataValue.longitudinalOffset : poseForward * jointFollowerDataValue.longitudinalOffset;

            TargetTransform.rotation = Quaternion.LookRotation(poseForward, jointPlaneOffset);
            TargetTransform.position = mainJointPose.position + jointPlaneOffset * (cachedRadius * jointFollowerDataValue.offsetAsRatioToRadius) + jointLongitudinalOffset;
        }
    }
}
