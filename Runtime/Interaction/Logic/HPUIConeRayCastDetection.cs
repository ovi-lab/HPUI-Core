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
        [Tooltip("The closest joint estimator.")]
        protected ClosestJointAndSideEstimator closestJointAndSideEstimator;

        protected IReadOnlyList<HPUIInteractorRayAngle> activeFingerAngles;
        protected readonly IReadOnlyList<HPUIInteractorRayAngle> defaultActiveFingerAngles;
        protected const float volarRadialThreshold = 0.70710678119f; // cost(45deg)

        public HPUIConeRayCastDetectionLogic()
        {
            // When it's not set by DetectedInteractables, use default value
            defaultActiveFingerAngles = new List<HPUIInteractorRayAngle>().AsReadOnly();
            activeFingerAngles = defaultActiveFingerAngles;
        }

        public HPUIConeRayCastDetectionLogic(float hoverRadius, HPUIInteractorConeRayAngles coneRayAngles, XRHandTrackingEvents xrHandTrackingEvents) : this()
        {
            this.InteractionHoverRadius = hoverRadius;
            this.coneRayAngles = coneRayAngles;
            closestJointAndSideEstimator = new ClosestJointAndSideEstimator(xrHandTrackingEvents);
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

            if (closestJointAndSideEstimator.XRHandTrackingEvents == null)
            {
                Debug.LogError($"The `xrHandTrackingEvents` is not set!");
                failed = true;
            }

            if (failed)
            {
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            if (closestJointAndSideEstimator.Estimate(out XRHandJointID closestJoint, out FingerSide closestSide))
            {
                activeFingerAngles = ConeRayAngles.GetAngles(closestJoint, closestSide);
                if (activeFingerAngles == null)
                {
                    activeFingerAngles = defaultActiveFingerAngles;
                }
            }
            Process(interactor, interactionManager, activeFingerAngles, validTargets, out hoverEndPoint);
        }

        /// <inheritdoc />
        public override void Reset()
        {
            closestJointAndSideEstimator.Reset();

            if (closestJointAndSideEstimator.XROriginTransform == null)
            {
                closestJointAndSideEstimator.XROriginTransform = GameObject.FindFirstObjectByType<XROrigin>()?.transform;
                if (closestJointAndSideEstimator.XROriginTransform == null)
                {
                    Debug.LogError($"XR Origin not found! Manually set value for XROriginTransform");
                }
            }
        }

        [Serializable]
        public class ClosestJointAndSideEstimator: IDisposable
        {
            private Dictionary<XRHandJointID, Pose> jointLocations = new();
            private List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
            {
                XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
                XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
                XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
                XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
                XRHandJointID.ThumbTip
            };
            private Dictionary<XRHandJointID, XRHandJointID> trackedJointsToSegment = new ()
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

            // FIXME: Handle setting hooks when this value changes in editor during play mode.
            [SerializeField]
            [Tooltip("The XR Hand Tracking Events component used to track the state of the segments.")]
            private XRHandTrackingEvents xrHandTrackingEvents;

            public XRHandTrackingEvents XRHandTrackingEvents
            {
                get => xrHandTrackingEvents;
                set
                {
                    if (value != xrHandTrackingEvents)
                    {
                        xrHandTrackingEvents?.jointsUpdated.RemoveListener(UpdateJointsData);
                    }

                    xrHandTrackingEvents = value;
                    xrHandTrackingEvents?.jointsUpdated.AddListener(UpdateJointsData);
                }
            }

            // FIXME: Handle setting hooks when this value changes in editor during play mode.
            [SerializeField]
            [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
            private Transform xrOriginTransform;

            /// <summary>
            /// XR Origin transform. If not set, will attempt to find XROrigin and use its transform.
            /// </summary>
            public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }

            /// <summary>
            /// The closest joint used for computing the cone.
            /// </summary>
            public XRHandJointID ClosestJoint { get => closestJoint; }

            /// <summary>
            /// The closest side used for computing the cone.
            /// </summary>
            public FingerSide ClosestSide { get => closestSide; }

            private bool receivedNewJointData;
            private XRHandJointID closestJoint = XRHandJointID.BeginMarker;
            private FingerSide closestSide;

            public ClosestJointAndSideEstimator()
            {
                Reset();
            }

            public ClosestJointAndSideEstimator(XRHandTrackingEvents handTrackingEvents) :this()
            {
                this.XRHandTrackingEvents = handTrackingEvents;
                Reset();
            }

            /// <summary>
            /// Resets/initializes the logic.
            /// </summary>
            public void Reset()
            {
                foreach(XRHandJointID id in trackedJoints)
                {
                    if (!jointLocations.ContainsKey(id))
                    {
                        jointLocations.Add(id, Pose.identity);
                    }
                }
                if (this.XRHandTrackingEvents != null)
                {
                    XRHandTrackingEvents.jointsUpdated.AddListener(UpdateJointsData);
                }
            }

            /// <summary>
            /// Callback to use with <see cref="xrHandTrackingEvents.jointsUpdated"/> event.
            /// </summary>
            private void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
            {
                foreach(XRHandJointID id in trackedJoints)
                {
                    if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
                    {
                        jointLocations[id] = pose.GetTransformedBy(XROriginTransform);
                        receivedNewJointData = true;
                    }
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                XRHandTrackingEvents.jointsUpdated.RemoveListener(UpdateJointsData);
            }

            /// <summary>
            /// Estimate and return the closest joint and side. If the estimate (because new event arrives), return true.
            /// </summary>
            public bool Estimate(out XRHandJointID _closestJoint, out FingerSide _closestSide)
            {
                if (receivedNewJointData)
                {
                    receivedNewJointData = false;
                    Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip].position;
                    Vector3 toClosestPoint = jointLocations[XRHandJointID.ThumbTip].forward;
                    float shortestDistance = float.MaxValue;

                    _closestJoint = XRHandJointID.BeginMarker;
                    foreach (KeyValuePair<XRHandJointID, XRHandJointID> kvp in trackedJointsToSegment)
                    {
                        Vector3 baseVector = jointLocations[kvp.Key].position;
                        Vector3 segmentVector = jointLocations[kvp.Value].position - baseVector;
                        Vector3 toTipVector = thumbTipPos - baseVector;
                        float distanceOnSegmentVector = Mathf.Clamp(Vector3.Dot(toTipVector, segmentVector.normalized), 0, segmentVector.magnitude);
                        Vector3 closestPoint = distanceOnSegmentVector * segmentVector.normalized + baseVector;
                        Vector3 currentToClosestPoint = (closestPoint - thumbTipPos);
                        float distance = currentToClosestPoint.sqrMagnitude;
                        if (distance < shortestDistance)
                        {
                            toClosestPoint = currentToClosestPoint;
                            shortestDistance = distance;
                            _closestJoint = kvp.Key;
                        }
                    }

                    if (_closestJoint != XRHandJointID.BeginMarker)
                    {
                        if (Vector3.Dot(jointLocations[_closestJoint].up, toClosestPoint.normalized) > volarRadialThreshold)
                        {
                            _closestSide = FingerSide.volar;
                        }
                        else
                        {
                            _closestSide = FingerSide.radial;
                        }

                        if (_closestJoint != closestJoint && _closestSide != closestSide)
                        {
                            closestJoint = _closestJoint;
                            closestSide = _closestSide;
                            return true;
                        }
                    }
                }

                _closestJoint = closestJoint;
                _closestSide = closestSide;
                return false;
            }
        }
    }
}
