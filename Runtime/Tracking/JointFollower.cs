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
        [Tooltip("Default joint radius to use when joint radius is not provided by XR Hands. In unity units.")]
        public float defaultJointRadius = 0.01f;

        [Tooltip("The offset angle.")][SerializeField]
        public float offsetAngle = 0f;
        [Tooltip("The offset as a ratio of the joint radius.")][SerializeField]
        public float offsetAsRatioToRadius = 1f;
        [Tooltip("The offset along joint (the joint's up). In unity units.")][SerializeField]
        public float offsetAlongJoint = 0f;

        private EventHandler<JointDataEventArgs> handler = null;
        private float cachedRadius = 0f;

        private void OnUpdate(object _, JointDataEventArgs args)
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
                Vector3 poseForward = args.pose.forward;
                Quaternion rotationOffset = Quaternion.AngleAxis(offsetAngle, poseForward);

                Vector3 jointPlaneOffset = rotationOffset * args.pose.up * offsetAsRatioToRadius;
                Vector3 jointLongitudianlOffset = poseForward * offsetAlongJoint;

                transform.rotation = Quaternion.LookRotation(poseForward, jointPlaneOffset);
                transform.position = args.pose.position;
                transform.localPosition += jointPlaneOffset * cachedRadius + jointLongitudianlOffset;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
            if (handler == null)
            {
                handler = new EventHandler<JointDataEventArgs>(OnUpdate);
            }
            HandJointData.Instance.SubscribeToJointDataEvent(handedness, jointID, handler);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
            if (handler != null)
            {
                HandJointData.Instance.UnsubscribeToJointDataEvent(handedness, jointID, handler);
            }
        }

        // NOTE: for testing
        public Transform testGameObject;
        private void Update()
        {
            if (testGameObject != null)
                OnUpdate(null, new JointDataEventArgs(handedness, jointID, new Pose(testGameObject.position, testGameObject.rotation), 0.01f, true, false));
        }
    }
}
