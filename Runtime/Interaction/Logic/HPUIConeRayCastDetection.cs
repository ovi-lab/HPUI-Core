using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// TODO docs
    /// </summary>
    [Serializable]
    public class HPUIConeRayCastDetectionLogic : HPUIRayCastDetectionBaseLogic
    {
        [SerializeField]
        [Tooltip("The HPUIInteractorConeRayAngles asset to use when using cone")]
        private HPUIInteractorConeRayAngles coneRayAngles;

        /// <summary>
        /// The HPUIInteractorConeRayAngles asset to use when using cone
        /// </summary>
        public HPUIInteractorConeRayAngles ConeRayAngles { get => coneRayAngles; set => coneRayAngles = value; }

        private XRHandTrackingEvents xrHandTrackingEvents;
        protected Dictionary<XRHandJointID, Vector3> jointLocations = new Dictionary<XRHandJointID, Vector3>();


            // foreach(XRHandJointID id in trackedJoints)
            // {
            //     jointLocations.Add(id, Vector3.zero);
            // }

            // ResetAngleFunctions();
            // if (XROriginTransform == null)
            // {
            //     XROriginTransform = FindObjectOfType<XROrigin>()?.transform;
            //     if (XROriginTransform == null)
            //     {
            //         Debug.LogError($"XR Origin not found! Manually set value for XROriginTransform");
            //     }
            // }
            // xrHandTrackingEvents.jointsUpdated.AddListener(UpdateJointsData);

        // // Used with the RayCastTechnique.SegmentVector, and RayCastTechnique.FullRange
        // protected List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        // {
        //     XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
        //     XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
        //     XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
        //     XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
        //     XRHandJointID.ThumbTip
        // };

        // private Dictionary<XRHandJointID, XRHandJointID> trackedJointsToSegment = new ()
        // {
        //     {XRHandJointID.IndexProximal,      XRHandJointID.IndexIntermediate},
        //     {XRHandJointID.IndexIntermediate,  XRHandJointID.IndexDistal},
        //     {XRHandJointID.IndexDistal,        XRHandJointID.IndexTip},
        //     {XRHandJointID.MiddleProximal,     XRHandJointID.MiddleIntermediate},
        //     {XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal},
        //     {XRHandJointID.MiddleDistal,       XRHandJointID.MiddleTip},
        //     {XRHandJointID.RingProximal,       XRHandJointID.RingIntermediate},
        //     {XRHandJointID.RingIntermediate,   XRHandJointID.RingDistal},
        //     {XRHandJointID.RingDistal,         XRHandJointID.RingTip},
        //     {XRHandJointID.LittleProximal,     XRHandJointID.LittleIntermediate},
        //     {XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal},
        //     {XRHandJointID.LittleDistal,       XRHandJointID.LittleTip},
        // };

        // protected void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        // {
        //     foreach(XRHandJointID id in trackedJoints)
        //     {
        //         if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
        //         {
        //             jointLocations[id] = xrOriginTransform.TransformPoint(pose.position);
        //             receivedNewJointData = true;
        //         }
        //     }
        // }

        public HPUIConeRayCastDetectionLogic()
        {}

        public HPUIConeRayCastDetectionLogic(float hoverRadius, HPUIInteractorConeRayAngles coneRayAngles)
        {
            this.InteractionHoverRadius = hoverRadius;
            this.coneRayAngles = coneRayAngles;
        }


        /// <inheritdoc />
        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, InteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            // TODO Update this

            // TODO: Move this logic to its own component
            // switch(RayCastTechnique)
            // {
            //     case RayCastTechniqueEnum.Cone:
            //         if (receivedNewJointData)
            //         {
            //             receivedNewJointData = false;
            //             Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip];

            //             XRHandJointID closestJoint = XRHandJointID.BeginMarker;
            //             float shortestDistance = float.MaxValue;
            //             foreach(KeyValuePair<XRHandJointID, XRHandJointID> kvp in trackedJointsToSegment)
            //             {
            //                 Vector3 baseVector = jointLocations[kvp.Key];
            //                 Vector3 segmentVector = jointLocations[kvp.Value] - baseVector;
            //                 Vector3 toTipVector = thumbTipPos - baseVector;
            //                 Vector3 closestPoint = Vector3.Dot(toTipVector, segmentVector.normalized) * segmentVector.normalized + baseVector;
            //                 float distance = (thumbTipPos - closestPoint).sqrMagnitude;
            //                 if (distance < shortestDistance)
            //                 {
            //                     shortestDistance = distance;
            //                     closestJoint = kvp.Key;
            //                 }
            //             }

            //             if (closestJoint != XRHandJointID.BeginMarker)
            //             {
            //                 activeFingerAngles = closestJoint switch
            //                     {
            //                         XRHandJointID.IndexProximal      => ConeRayAngles.IndexProximalAngles,
            //                         XRHandJointID.IndexIntermediate  => ConeRayAngles.IndexIntermediateAngles,
            //                         XRHandJointID.IndexDistal        => ConeRayAngles.IndexDistalAngles,
            //                         XRHandJointID.MiddleProximal     => ConeRayAngles.MiddleProximalAngles,
            //                         XRHandJointID.MiddleIntermediate => ConeRayAngles.MiddleIntermediateAngles,
            //                         XRHandJointID.MiddleDistal       => ConeRayAngles.MiddleDistalAngles,
            //                         XRHandJointID.RingProximal       => ConeRayAngles.RingProximalAngles,
            //                         XRHandJointID.RingIntermediate   => ConeRayAngles.RingIntermediateAngles,
            //                         XRHandJointID.RingDistal         => ConeRayAngles.RingDistalAngles,
            //                         XRHandJointID.LittleProximal     => ConeRayAngles.LittleProximalAngles,
            //                         XRHandJointID.LittleIntermediate => ConeRayAngles.LittleIntermediateAngles,
            //                         XRHandJointID.LittleDistal       => ConeRayAngles.LittleDistalAngles,
            //                         _ => throw new System.InvalidOperationException($"Unknown joint seen. Got {closestJoint},")
            //                     };
            //             }
            //         }
            //         break;
            //     case RayCastTechniqueEnum.FullRange:
            //         activeFingerAngles = FullRangeRayAngles.angles;
            //         break;
            //     default:
            //         break;
            // }
            Process(interactor, interactionManager, ConeRayAngles.IndexDistalAngles, validTargets, out hoverEndPoint);
        }
    }
}
