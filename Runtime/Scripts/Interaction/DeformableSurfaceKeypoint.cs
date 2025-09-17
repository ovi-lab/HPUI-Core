using System;
using ubco.ovilab.HPUI.Core.Tracking;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// Represents what one keypoint to be used when generating surfaces.
    /// </summary>
    [Serializable]
    public struct DeformableSurfaceKeypoint
    {
        public enum KeypointsOptions { JointID, JointFollowerData, Transform}

        public KeypointsOptions keypointType;
        public XRHandJointID jointID;
        public JointFollowerDatumProperty jointFollowerData;
        public Transform jointTransform;

        public DeformableSurfaceKeypoint(KeypointsOptions keypointType, XRHandJointID jointID = XRHandJointID.Invalid, JointFollowerDatumProperty jointFollowerData = null, Transform jointTransform = null)
        {
            this.keypointType = keypointType;
            this.jointID = jointID;
            this.jointFollowerData = jointFollowerData;
            this.jointTransform = jointTransform;
        }
    }
}
