using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Detects which interactable is being selected with raycasts based on the the <see cref="ConeRayAngles"/>.
    /// The cone of rays is based on the finger segment that is closest to the thumb tip.  The heuristic
    /// assigned to the interactable is based on the number of rays that makes contact with the interactable
    /// and the distances to it.
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

        [SerializeField]
        [Tooltip("The XR Hand Tracking Events component used to track the state of the segments.")]
        private XRHandTrackingEvents xrHandTrackingEvents;

        /// <summary>
        /// The XR Hand Tracking Events component used to track the state of the segments.
        /// </summary>
        public XRHandTrackingEvents XrHandTrackingEvents { get => xrHandTrackingEvents; set => UpdateHandTrackingEventsHook(value); }

        [SerializeField]
        [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
        private Transform xrOriginTransform;

        /// <summary>
        /// XR Origin transform. If not set, will attempt to find XROrigin and use its transform.
        /// </summary>
        public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }

        protected bool receivedNewJointData;
        // When it's not set by DetectedInteractables, use default value
        protected List<HPUIInteractorRayAngle> activeFingerAngles = new();
        protected Dictionary<XRHandJointID, Vector3> jointLocations = new Dictionary<XRHandJointID, Vector3>();
        protected List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
            XRHandJointID.ThumbTip
        };
        protected Dictionary<XRHandJointID, XRHandJointID> trackedJointsToSegment = new ()
        {
            {XRHandJointID.IndexProximal,      XRHandJointID.IndexIntermediate},
            {XRHandJointID.IndexIntermediate,  XRHandJointID.IndexDistal},
            {XRHandJointID.IndexDistal,        XRHandJointID.IndexTip},
            {XRHandJointID.MiddleProximal,     XRHandJointID.MiddleIntermediate},
            {XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal},
            {XRHandJointID.MiddleDistal,       XRHandJointID.MiddleTip},
            {XRHandJointID.RingProximal,       XRHandJointID.RingIntermediate},
            {XRHandJointID.RingIntermediate,   XRHandJointID.RingDistal},
            {XRHandJointID.RingDistal,         XRHandJointID.RingTip},
            {XRHandJointID.LittleProximal,     XRHandJointID.LittleIntermediate},
            {XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal},
            {XRHandJointID.LittleDistal,       XRHandJointID.LittleTip},
        };

        public HPUIConeRayCastDetectionLogic()
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                jointLocations.Add(id, Vector3.zero);
            }
            UpdateHandTrackingEventsHook(xrHandTrackingEvents);
        }
        public HPUIConeRayCastDetectionLogic(float hoverRadius, HPUIInteractorConeRayAngles coneRayAngles, XRHandTrackingEvents xrHandTrackingEvents) : this()
        {
            this.InteractionHoverRadius = hoverRadius;
            this.coneRayAngles = coneRayAngles;
            UpdateHandTrackingEventsHook(xrHandTrackingEvents);
        }

        /// <summary>
        /// Update the <see cref="xrHandTrackingEvents"/> instance.
        /// Also tries to make sure the <see cref="XROriginTransform"/> is correctly setup.
        /// </summary>
        protected void UpdateHandTrackingEventsHook(XRHandTrackingEvents xrHandTrackingEvents)
        {
            if (this.xrHandTrackingEvents != xrHandTrackingEvents)
            {
                this.xrHandTrackingEvents?.jointsUpdated.RemoveListener(UpdateJointsData);
            }

            this.xrHandTrackingEvents = xrHandTrackingEvents;
            this.xrHandTrackingEvents?.jointsUpdated.AddListener(UpdateJointsData);
        }

        /// <summary>
        /// Callback to use with <see cref="xrHandTrackingEvents.jointsUpdated"/> event.
        /// </summary>
        protected void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
                {
                    jointLocations[id] = xrOriginTransform.TransformPoint(pose.position);
                    receivedNewJointData = true;
                }
            }
        }

        /// <inheritdoc />
        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            bool failed = false;
            if (ConeRayAngles == null)
            {
                Debug.LogError($"The `ConeRayAngle` asset is not set!");
                failed = true;
            }

            if (xrHandTrackingEvents == null)
            {
                Debug.LogError($"The `xrHandTrackingEvents` is not set!");
                failed = true;
            }

            if (failed)
            {
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            if (receivedNewJointData)
            {
                receivedNewJointData = false;
                Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip];
                XRHandJointID closestJoint = XRHandJointID.BeginMarker;
                float shortestDistance = float.MaxValue;

                foreach(KeyValuePair<XRHandJointID, XRHandJointID> kvp in trackedJointsToSegment)
                {
                    Vector3 baseVector = jointLocations[kvp.Key];
                    Vector3 segmentVector = jointLocations[kvp.Value] - baseVector;
                    Vector3 toTipVector = thumbTipPos - baseVector;
                    float distanceOnSegmentVector = Mathf.Clamp(Vector3.Dot(toTipVector, segmentVector.normalized), 0, segmentVector.magnitude);
                    Vector3 closestPoint = distanceOnSegmentVector * segmentVector.normalized + baseVector;
                    float distance = (thumbTipPos - closestPoint).sqrMagnitude;
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestJoint = kvp.Key;
                    }
                }

                if (closestJoint != XRHandJointID.BeginMarker)
                {
                    activeFingerAngles = ConeRayAngles.ActiveFingerAngles[closestJoint];
                }
            }
            Process(interactor, interactionManager, activeFingerAngles, validTargets, out hoverEndPoint);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            xrHandTrackingEvents.jointsUpdated.RemoveListener(UpdateJointsData);
        }

        /// <inheritdoc />
        public override void Reset()
        {
            if (XROriginTransform == null)
            {
                XROriginTransform = GameObject.FindFirstObjectByType<XROrigin>()?.transform;
                if (XROriginTransform == null)
                {
                    Debug.LogError($"XR Origin not found! Manually set value for XROriginTransform");
                }
            }
            UpdateHandTrackingEventsHook(xrHandTrackingEvents);
        }
    }
}
