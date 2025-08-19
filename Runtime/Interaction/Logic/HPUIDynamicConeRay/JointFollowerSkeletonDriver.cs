using System;
using System.Collections.Generic;
using ArtificeToolkit.Runtime.SerializedDictionary;
using EditorAttributes;
using ubco.ovilab.HPUI.Tracking;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;

public class JointFollowerSkeletonDriver : HandSubsystemSubscriber
{
    public Dictionary<XRHandJointID, JointFollowerDatumProperty> JointFollowerData { get => jointFollowerData; set => jointFollowerData = value; }
    public SerializedDictionary<XRHandJointID, Transform> HandJoints { get => handJoints;}
    public override Handedness Handedness { get => handedness; set => handedness = value; }

    [SerializeField] private SerializedDictionary<XRHandJointID, Transform> handJoints;
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private Transform referenceTransform;

    private Dictionary<XRHandJointID, JointFollowerDatumProperty> jointFollowerData = new();
    private float cachedRadius = 0f;
    [SerializeField] private Vector3 poseScale = Vector3.one;
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);


    private void Start()
    {
        foreach ((XRHandJointID jointID, Transform jointTransform) in handJoints)
        {
            JointFollowerData jointData = new JointFollowerData
            {
                handedness = handedness,
                jointID = jointID,
                useSecondJointID = false,
                defaultJointRadius = 0.00f,
                offsetAngle = 0f,
                offsetAsRatioToRadius = 0f,
                longitudinalOffset = 0f
            };
            JointFollowerDatumProperty jointDatumProperty = new (jointData);
            jointFollowerData.Add(jointID, jointDatumProperty);
        }

        if (referenceTransform == null)
        {
            if (xrOrigin != null)
            {
                referenceTransform = xrOrigin.transform;
            }
            else
            {
                Debug.LogError("XR Origin not found. Make sure an XR Origin Exists in the Scene");
            }
        }

        cachedRadius = 0.00f;
    }

    [Button]
    private void AutoAssignBonesToJoints()
    {
        Dictionary<XRHandJointID, string> skeletonTransformNamePair = new()
        {
            { XRHandJointID.Wrist , "Hand"},
            { XRHandJointID.ThumbMetacarpal, "R1D1" },
            { XRHandJointID.ThumbProximal, "R1D2" },
            { XRHandJointID.ThumbDistal , "R1D3" },
            { XRHandJointID.ThumbTip , "R1D4" },

            { XRHandJointID.IndexProximal, "R2D1"},
            { XRHandJointID.IndexIntermediate, "R2D2" },
            { XRHandJointID.IndexDistal, "R2D3" },
            { XRHandJointID.IndexTip , "R2D4" },

            { XRHandJointID.MiddleProximal, "R3D1"},
            { XRHandJointID.MiddleIntermediate, "R3D2" },
            { XRHandJointID.MiddleDistal, "R3D3" },
            { XRHandJointID.MiddleTip , "R3D4" },

            { XRHandJointID.RingProximal, "R4D1"},
            { XRHandJointID.RingIntermediate, "R4D2" },
            { XRHandJointID.RingDistal, "R4D3" },
            { XRHandJointID.RingTip , "R4D4" },

            { XRHandJointID.LittleProximal, "R5D1"},
            { XRHandJointID.LittleIntermediate, "R5D2" },
            { XRHandJointID.LittleDistal, "R5D3" },
            { XRHandJointID.LittleTip , "R5D4" }
        };
        handJoints.Clear();

        foreach (KeyValuePair<XRHandJointID,string> pair in skeletonTransformNamePair)
        {
            XRHandJointID jointID = pair.Key;
            Transform boneTransform = FindChildByName(transform.gameObject, pair.Value).transform;
            handJoints.Add(jointID, boneTransform);
        }
    }

    public GameObject FindChildByName(GameObject parent, string childName)
    {
        // If the parent GameObject matches the name, return it
        if (parent.name == childName)
        {
            return parent;
        }

        // Recursively search through all children
        foreach (Transform child in parent.transform)
        {
            GameObject result = FindChildByName(child.gameObject, childName);
            if (result != null)
            {
                return result;
            }
        }

        // Return null if no matching GameObject is found
        return null;
    }

    protected override void ProcessJointData(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags )
    {
        XRHand hand;
        if (handedness == Handedness.Invalid)
        {
            Debug.LogWarning($"Handedness value in JointFollowerData not valid on {transform.name}, Disabling JointFollowerManager");
            enabled = false;
            return;
        }
        hand = Handedness.Right == handedness ? subsystem.rightHand : subsystem.leftHand;

        foreach ((XRHandJointID jointID, JointFollowerDatumProperty jointDatum) in JointFollowerData)
        {
            JointFollowerData jointFollowerDataValue = jointDatum.Value;
            XRHandJoint currentJoint = hand.GetJoint(jointFollowerDataValue.jointID);
            bool jointPoseExists = currentJoint.TryGetPose(out Pose currentJointPose);
            if (jointPoseExists)
            {
                if (referenceTransform != null)
                {
                    Vector3 position = referenceTransform.transform.position;
                    Pose referencePose = new Pose(position, referenceTransform.transform.rotation);
                    currentJointPose = currentJointPose.GetTransformedBy(referencePose);
                }
                SetPose(currentJointPose, JointFollowerData[jointID],  handJoints[jointID]);
            }
        }
    }

    /// <summary>
    /// This method uses the jointFollowerDataValue and sets the poses.
    /// </summary>
    protected void SetPose(Pose mainJointPose,JointFollowerDatumProperty jointFollowerDatum,  Transform TargetTransform)
    {
        Vector3 forward = mainJointPose.forward * poseScale.x;
        Vector3 up = mainJointPose.up * poseScale.y;

        JointFollowerData jointFollowerDataValue = jointFollowerDatum.Value;

        Vector3 jointPlaneOffset;
        if (jointFollowerDataValue.offsetAngle == 0 || jointFollowerDataValue.offsetAsRatioToRadius == 0)
        {
            jointPlaneOffset = up;
        }
        else
        {
            jointPlaneOffset = Quaternion.AngleAxis(jointFollowerDataValue.offsetAngle, forward) * up;
        }

        TargetTransform.position = mainJointPose.position;// + jointPlaneOffset * (cachedRadius * jointFollowerDataValue.offsetAsRatioToRadius);
        TargetTransform.rotation = Quaternion.LookRotation(forward, jointPlaneOffset) * Quaternion.Euler(rotationOffset);
    }
}
