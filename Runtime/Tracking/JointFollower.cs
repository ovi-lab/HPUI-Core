using System;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovi.HPUI.Core
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
        [Tooltip("(optional) Second joint to use as reference. If not set to invalid, offsetAlongJoint behaves differently.")]
        [ConditionalField("secondJointID")]
        public XRHandJointID secondJointID;
        [Tooltip("Default joint radius to use when joint radius is not provided by XR Hands. In unity units.")]
        public float defaultJointRadius = 0.01f;

        [Tooltip("The offset angle.")][SerializeField]
        public float offsetAngle = 0f;
        [Tooltip("The offset as a ratio of the joint radius.")][SerializeField]
        public float offsetAsRatioToRadius = 1f;
        [Tooltip("The offset along joint (the joint's up) if no secondJoint is set. Otherwise, the position along joint as a ratio to the distance between jointID and secondJointID. In unity units.")]
        [SerializeField]
        public float longitudinalOffset = 0f;

        private EventHandler<JointDataEventArgs> handler = null;
        private float cachedRadius = 0f;
        private bool jointPoseRecieved, secondJointPoseRecieved;
        private Pose jointPose, secondJointPose;

        /// <summary>
        /// The callback used to get joint data from <see cref="HandJointData"/>
        /// </summary>
        protected void OnUpdate(object _, JointDataEventArgs args)
        {
            if (args.radiusSuccess)
            {
                cachedRadius = args.radius;
            }
            else if (cachedRadius == 0)
            {
                cachedRadius = defaultJointRadius;
            }

            if (args.poseSuccess)
            {
                if (args.jointID == jointID)
                {
                    jointPose = args.pose;
                    jointPoseRecieved = true;
                }
                else if (args.jointID == secondJointID)
                {
                    secondJointPose = args.pose;
                    secondJointPoseRecieved = true;
                }

                if (jointPoseRecieved && (secondJointID == XRHandJointID.Invalid || secondJointPoseRecieved))
                {
                    Vector3 poseForward = jointPose.forward;
                    Quaternion rotationOffset = Quaternion.AngleAxis(offsetAngle, poseForward);

                    Vector3 jointPlaneOffset = rotationOffset * jointPose.up * offsetAsRatioToRadius;
                    Vector3 jointLongitudianlOffset = secondJointPoseRecieved ? (secondJointPose.position - jointPose.position) * longitudinalOffset : poseForward * longitudinalOffset;

                    transform.rotation = Quaternion.LookRotation(poseForward, jointPlaneOffset);
                    transform.position = jointPose.position;
                    transform.localPosition += jointPlaneOffset * cachedRadius + jointLongitudianlOffset;

                    jointPoseRecieved = false;
                    secondJointPoseRecieved = false;
                }
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
            // TODO: Do this if the values for the joints are updated during runtime
            if (handler == null)
            {
                handler = new EventHandler<JointDataEventArgs>(OnUpdate);
            }
            HandJointData.Instance.SubscribeToJointDataEvent(handedness, jointID, handler);
            if (secondJointID != XRHandJointID.Invalid)
            {
                HandJointData.Instance.SubscribeToJointDataEvent(handedness, secondJointID, handler);
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
            // TODO: Do this if the values for the joints are updated during runtime
            if (handler != null)
            {
                HandJointData.Instance?.UnsubscribeToJointDataEvent(handedness, jointID, handler);
                if (secondJointID != XRHandJointID.Invalid)
                {
                    HandJointData.Instance?.SubscribeToJointDataEvent(handedness, secondJointID, handler);
                }
            }
        }
    }
}
