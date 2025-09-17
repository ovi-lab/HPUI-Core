using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core.Tracking
{
    [Serializable]
    public struct JointToTransformMapping
    {
        [SerializeField]
        [Tooltip("The XR Hand Joint Identifier that will drive the Transform.")]
        XRHandJointID xrHandJointID;

        [SerializeField]
        [Tooltip("The Transform that will be driven by the specified XR Joint.")]
        Transform jointTransform;

        /// <summary>
        /// The <see cref="XRHandJointID"/> that will drive the Transform.
        /// </summary>
        public XRHandJointID XRHandJointID => xrHandJointID;

        /// <summary>
        /// The Transform that will be driven by the specified joint's tracking data.
        /// </summary>
        public Transform JointTransform => jointTransform;
    }

    /// <summary>
    /// A component that makes the object follow a hand joint
    /// </summary>
    public class HandSkeletonDriver : HandSubsystemSubscriber
    {
        [SerializeField]
        [Tooltip("The handedness used.")]
        private Handedness handedness;

        /// <inheritdoc />
        public override Handedness Handedness {get => handedness; set => handedness = value;}

        [SerializeField]
        [Tooltip("The Transform that will be driven by the hand's root position and rotation.")]
        Transform rootTransform;

        /// <summary>
        /// The Transform that will be driven by the hand's root position and rotation.
        /// </summary>
        public Transform RootTransform => rootTransform;

        /// <summary>
        /// The list of joint to transform mappings
        /// </summary>
        [SerializeField]
        [Tooltip("List of XR Hand Joints with a mapping to a transform to drive.")]
        protected List<JointToTransformMapping> jointTransformMappings;

        /// <summary>
        /// The list of <see cref="XRHandJointID"/> with a mapping to a transform to drive.
        /// </summary>
        public List<JointToTransformMapping> JointTransformMappings => jointTransformMappings;

        protected Pose xrOriginPose;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            OnValidate();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            if (Application.isPlaying && xrOrigin != null)
            {
                Vector3 position = xrOrigin.transform.position;
                // position.y += xrOrigin.CameraYOffset;
                xrOriginPose = new Pose(position , xrOrigin.transform.rotation);
            }
        }

        /// <summary>
        /// Subscribe to events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        protected override void SubscribeHandSubsystem()
        {
            base.SubscribeHandSubsystem();
        }

        /// <summary>
        /// Apply data received to the transform.
        /// </summary>
        protected override void ProcessJointData(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags)
        {
            if (!enabled)
            {
                return;
            }

            XRHand hand;
            bool jointsUpdated;
            bool rootPoseUpdated;

            if (Handedness.Left == Handedness)
            {
                hand = subsystem.leftHand;

                jointsUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != XRHandSubsystem.UpdateSuccessFlags.None;
                rootPoseUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != XRHandSubsystem.UpdateSuccessFlags.None;
            }
            else if (Handedness.Right == Handedness)
            {
                hand = subsystem.rightHand;

                jointsUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != XRHandSubsystem.UpdateSuccessFlags.None;
                rootPoseUpdated = (updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != XRHandSubsystem.UpdateSuccessFlags.None;
            }
            else
            {
                Debug.LogError($"Handedness value in JointFollowerData not valid (got {Handedness}), disabling JointFollower.");
                this.enabled = false;
                return;
            }

            if (rootPoseUpdated)
            {
                Pose rootPose = hand.rootPose.GetTransformedBy(xrOriginPose);
                RootTransform.localRotation = Quaternion.LookRotation(rootPose.forward, -rootPose.up);
                RootTransform.localPosition = rootPose.position;
            }

            if (!jointsUpdated)
            {
                return;
            }

            foreach (JointToTransformMapping mapping in JointTransformMappings)
            {
                if (hand.GetJoint(mapping.XRHandJointID).TryGetPose(out Pose pose))
                {
                    pose = pose.GetTransformedBy(xrOriginPose);
                    mapping.JointTransform.rotation = Quaternion.LookRotation(pose.forward, -pose.up);;
                    mapping.JointTransform.position = pose.position;
                }
            }
        }
    }
}
