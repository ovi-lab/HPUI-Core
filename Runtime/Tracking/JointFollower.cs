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
        private float offsetAngle = 0f;
        /// <summary>
        /// The offset angle.
        /// </summary>
        public float OffsetAngle {
            get => offsetAngle;
            set {
                offsetAngle = value;
                OnValidate();
            }
        }

        [Tooltip("The offset as a ratio of the joint radius.")][SerializeField]
        private float offsetAsRatioToRadius = 1f;
        /// <summary>
        /// The offset as a ratio of the joint radius.
        /// </summary>
        public float OffsetAsRatioToRadius {
            get => offsetAsRatioToRadius;
            set {
                offsetAsRatioToRadius = value;
                OnValidate();
            }
        }
        [Tooltip("The offset along joint (the joint's up). In unity units.")][SerializeField]
        private float offsetAlongJoint = 0f;
        /// <summary>
        /// The offset along joint (the joint's up). In unity units.
        /// </summary>
        public float OffsetAlongJoint {
            get => offsetAlongJoint;
            set {
                offsetAlongJoint = value;
                OnValidate();
            }
        }

        private EventHandler<JointDataEventArgs> handler = null;
        private Vector3 jointPlaneOffset = Vector3.zero;
        private Vector3 jointLongitudianlOffset = Vector3.zero;
        private Quaternion rotationOffset = Quaternion.identity;
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
                transform.position = args.pose.position;
                transform.localPosition += jointPlaneOffset * cachedRadius + jointLongitudianlOffset;
                transform.rotation = args.pose.rotation;
                transform.localRotation *= rotationOffset;
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
            OnValidate();
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

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            rotationOffset = Quaternion.AngleAxis(offsetAngle, Vector3.up);
            jointPlaneOffset = rotationOffset * Vector3.up * offsetAsRatioToRadius;
            jointLongitudianlOffset = Vector3.up * offsetAlongJoint;
        }
    }
}
