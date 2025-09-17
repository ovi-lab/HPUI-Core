using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core.Tracking
{
    /// <summary>
    /// Represents the joint follower data to be used.
    /// </summary>
    [Serializable]
    public class JointFollowerData
    {
        [Tooltip("The handedness of the joint to follow")]
        public Handedness handedness;
        [Tooltip("The joint to follow.")]
        public XRHandJointID jointID;
        [Tooltip("Should a second joint be used. If `useSecondJointID` is true, offsetAlongJoint behaves differently.")]
        public bool useSecondJointID;
        [Tooltip("Second joint to use as reference. If `useSecondJointID` is true, offsetAlongJoint behaves differently.")]
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

        public JointFollowerData()
        {}

        public JointFollowerData(Handedness handedness, XRHandJointID jointID, float offsetAngle, float offsetAsRationToRadius, float longitudinalOffset)
        {
            this.handedness = handedness;
            this.jointID = jointID;
            this.offsetAngle = offsetAngle;
            this.offsetAsRatioToRadius = offsetAsRationToRadius;
            this.longitudinalOffset = longitudinalOffset;
        }

        public JointFollowerData(Handedness handedness, XRHandJointID firstJointID, XRHandJointID secondJointID, float offsetAngle, float offsetAsRationToRadius, float longitudinalOffset)
        {
            this.handedness = handedness;
            this.jointID = firstJointID;
            this.secondJointID = secondJointID;
            this.useSecondJointID = true;
            this.offsetAngle = offsetAngle;
            this.offsetAsRatioToRadius = offsetAsRationToRadius;
            this.longitudinalOffset = longitudinalOffset;
        }


        public IEnumerable<XRHandJointID> JointsUsed()
        {
            List<XRHandJointID> usedJoints = new();
            usedJoints.Add(jointID);
            if (useSecondJointID)
            {
                usedJoints.Add(secondJointID);
            }
            return usedJoints;
        }
    }
}
