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

        private EventHandler<JointDataEventArgs> handler = null;

        private void OnUpdate(object _, JointDataEventArgs args)
        {
            transform.position = args.pose.position;
            transform.rotation = args.pose.rotation;
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
    }
}
