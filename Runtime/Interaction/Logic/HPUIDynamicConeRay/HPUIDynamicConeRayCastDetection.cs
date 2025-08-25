using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.utils;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public class HPUIDynamicConeRayCastDetectionLogic : HPUIRayCastDetectionBaseLogic
    {
        private HPUIInteractorFullRangeAngles fullRangeRayAngles;
        private TargetDirectionEstimator _targetDirectionEstimator;

        [SerializeField] private XRHandTrackingEvents _xrHandTrackingEvents;
        [SerializeField] private Transform XROrigin;

        [SerializeReference, SubclassSelector] private IHPUIRaySubSampler coneType;

        [Header("Dynamic Cone Properties")]
        [SerializeField]
        [Tooltip("Rotates the target vector orientation along the vector formed between the closest two XRI bones")]
        private float rotationAngle = 0;
        [SerializeField]
        [Tooltip("Rotates the target vector orientation perpendicular to the vector formed between the closest two XRI bones")]
        private float tiltRotation = 20f;
        [SerializeField]
        [Tooltip("The bias for the cone to deviate towards proximal or tip. Higher Sensitivity gives less resolution in the middle parts of the finger. Lower sensitivity gives less resolution in the extremities")]
        private float sensitivity = 2f;

        [Header("Debug")]
        [SerializeField] private float weightToTip;
        [SerializeField] private float weightToProximal;
        [SerializeField] private HandJointEstimatedData currentData;

        public HPUIDynamicConeRayCastDetectionLogic()
        {

        }

        public float InteractionHoverRadius { get; set; }

        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            // bool failed = false;

            if (_targetDirectionEstimator == null)
            {
                _targetDirectionEstimator = new TargetDirectionEstimator(_xrHandTrackingEvents, XROrigin);
            }

            if (_xrHandTrackingEvents == null || XROrigin == null)
            {
                Debug.LogError("Hand tracking events or XROrigin not set!");
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            _targetDirectionEstimator.Estimate(rotationAngle, tiltRotation, sensitivity, out HandJointEstimatedData estimatedData);

            currentData = estimatedData;
            weightToTip = estimatedData.GetTipWeight();
            weightToProximal = estimatedData.GetProximalWeight();
            Transform interactorObject = interactor.transform;
            List<HPUIInteractorRayAngle> angles = coneType.SampleRays(interactorObject, estimatedData);
            Debug.DrawRay(interactor.transform.position, estimatedData.TargetDirection, Color.magenta);
            Process(interactor, interactionManager, angles, validTargets, out hoverEndPoint);
        }

        // public void Reset(){ }
        //
        // public void Dispose(){ }

    }

    [Serializable]
    public class TargetDirectionEstimator : IDisposable
    {
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

        public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }
        // public XRHandFingerID ClosestFingerID => closestFingerID;

        [SerializeField]
        [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
        private Transform xrOriginTransform;
        [SerializeField]
        [Tooltip("The XR Hand Tracking Events component used to track the state of the segments.")]
        private XRHandTrackingEvents xrHandTrackingEvents;

        [SerializeField]
        [Tooltip("")]
        private Dictionary<XRHandJointID, Pose> jointLocations = new();

        private List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
            XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip
        };

        private Dictionary<XRHandJointID, XRHandJointID> trackedJointsToSegment = new()
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

        private Dictionary<XRHandFingerID?, (XRHandJointID start, XRHandJointID end)> fingerToJointsExtremities = new()
        {
            { XRHandFingerID.Index,  (XRHandJointID.IndexDistal,    XRHandJointID.IndexProximal) },
            { XRHandFingerID.Middle, (XRHandJointID.MiddleDistal,   XRHandJointID.MiddleProximal) },
            { XRHandFingerID.Ring,   (XRHandJointID.RingDistal,     XRHandJointID.RingProximal) },
            { XRHandFingerID.Little, (XRHandJointID.LittleDistal,   XRHandJointID.LittleProximal) }
        };

        private Dictionary<XRHandJointID, XRHandFingerID> jointToFinger = new()
        {
            // Index Finger
            { XRHandJointID.IndexProximal,      XRHandFingerID.Index },
            { XRHandJointID.IndexIntermediate,  XRHandFingerID.Index },
            { XRHandJointID.IndexDistal,        XRHandFingerID.Index },
            { XRHandJointID.IndexTip,           XRHandFingerID.Index },

            // Middle Finger
            { XRHandJointID.MiddleProximal,     XRHandFingerID.Middle },
            { XRHandJointID.MiddleIntermediate, XRHandFingerID.Middle },
            { XRHandJointID.MiddleDistal,       XRHandFingerID.Middle },
            { XRHandJointID.MiddleTip,          XRHandFingerID.Middle },

            // Ring Finger
            { XRHandJointID.RingProximal,       XRHandFingerID.Ring },
            { XRHandJointID.RingIntermediate,   XRHandFingerID.Ring },
            { XRHandJointID.RingDistal,         XRHandFingerID.Ring },
            { XRHandJointID.RingTip,            XRHandFingerID.Ring },

            // Little Finger
            { XRHandJointID.LittleProximal,     XRHandFingerID.Little },
            { XRHandJointID.LittleIntermediate, XRHandFingerID.Little },
            { XRHandJointID.LittleDistal,       XRHandFingerID.Little },
            { XRHandJointID.LittleTip,          XRHandFingerID.Little }
        };

        private bool receivedNewJointData;
        private GameObject tipViz;
        private GameObject proximalViz;
        public TargetDirectionEstimator(XRHandTrackingEvents handTrackingEvents, Transform _xrOriginTransform)
        {
            this.XRHandTrackingEvents = handTrackingEvents;
            xrOriginTransform = _xrOriginTransform;
            tipViz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipViz.GetComponent<MeshRenderer>().enabled = true;
            proximalViz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proximalViz.GetComponent<MeshRenderer>().enabled = true;
            Reset();
        }

        public void Reset()
        {
            foreach (XRHandJointID id in trackedJoints)
            {
                if (!jointLocations.ContainsKey(id))
                {
                    jointLocations.Add(id, Pose.identity);
                }
            }
            if (XRHandTrackingEvents != null)
            {
                XRHandTrackingEvents.jointsUpdated.AddListener(UpdateJointsData);
            }
        }

        private void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        {
            foreach (XRHandJointID id in trackedJoints)
            {
                if (args.hand.GetJoint(id).TryGetPose(out Pose pose))
                {
                    jointLocations[id] = pose.GetTransformedBy(XROriginTransform);
                    receivedNewJointData = true;
                }
            }
        }

        public void Estimate(float flatRotation, float tiltRotation, float sensitivity, out HandJointEstimatedData estimatedData)
        {

            XRHandFingerID? _closestFinger = null;
            var _closestJoint = XRHandJointID.BeginMarker;
            Vector3 vectorToFingerTip = Vector3.negativeInfinity;
            Vector3 vectorToFingerProximal = Vector3.negativeInfinity;
            Vector3 targetDirection = Vector3.negativeInfinity;
            Vector3 thumbReferencePoint = Vector3.negativeInfinity;

            estimatedData = new HandJointEstimatedData(_closestFinger, _closestJoint, vectorToFingerTip, vectorToFingerProximal, 0, targetDirection, thumbReferencePoint, jointLocations);
            XRHand hand = xrHandTrackingEvents.handedness == Handedness.Left
                ? xrHandTrackingEvents.subsystem.leftHand
                : xrHandTrackingEvents.subsystem.rightHand;
            if (!receivedNewJointData) return;

            receivedNewJointData = false;
            Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip].position;
            float shortestDistance = float.MaxValue;
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
                    shortestDistance = distance;
                    _closestJoint = kvp.Key;
                }
            }
            _closestFinger = jointToFinger[_closestJoint];
            (XRHandJointID start, XRHandJointID end) fingerExtremities = fingerToJointsExtremities[_closestFinger];
            XRHandJointID thumbDistal = XRHandJointID.ThumbDistal;
            thumbReferencePoint = (thumbTipPos + jointLocations[thumbDistal].position) / 2;

            XRHandJointID fingerTipID = trackedJointsToSegment[fingerExtremities.start];
            XRHandJointID distalID = fingerExtremities.start;
            XRHandJointID intermediateID = trackedJointsToSegment[fingerExtremities.end];
            XRHandJointID proximalID = fingerExtremities.end;

            Vector3 fingerTipPos = jointLocations[fingerTipID].position;
            Vector3 distalPos = jointLocations[distalID].position;
            Vector3 thumbToTipVector = fingerTipPos - thumbReferencePoint;
            vectorToFingerTip = thumbToTipVector;

            Vector3 fingerProximalPos = jointLocations[proximalID].position;
            Vector3 intermediatePos = jointLocations[intermediateID].position;
            //Proximal is too far off. Getting a mid-point between proximal and intermediate
            Vector3 targetPoint = (intermediatePos + fingerProximalPos) / 2;
            Vector3 thumbToProximalVector = targetPoint - thumbReferencePoint;
            vectorToFingerProximal = thumbToProximalVector;

            //debug visuals
            Debug.DrawLine(fingerTipPos, distalPos, Color.red);
            Debug.DrawRay(thumbReferencePoint, thumbToTipVector, Color.blue);
            Debug.DrawLine(fingerProximalPos, intermediatePos, Color.red);
            Debug.DrawRay(thumbReferencePoint, thumbToProximalVector, Color.blue);
            tipViz.transform.position = fingerTipPos;
            tipViz.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            proximalViz.transform.position = targetPoint;
            proximalViz.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            estimatedData = new HandJointEstimatedData(_closestFinger, _closestJoint, vectorToFingerTip, vectorToFingerProximal, sensitivity, targetDirection, thumbReferencePoint, jointLocations);
            targetDirection = estimatedData.GetTargetDirection(tiltRotation, flatRotation);
            Debug.DrawRay(thumbReferencePoint, targetDirection, Color.magenta);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            XRHandTrackingEvents.jointsUpdated.RemoveListener(UpdateJointsData);
        }

    }

    [Serializable]
    public class HandJointEstimatedData
    {
        public Vector3 TargetDirection => targetDirection;
        public XRHandFingerID? _closestFinger;
        public XRHandJointID _closestJoint;
        public Vector3 vectorToFingerTip;
        public Vector3 vectorToFingerProximal;
        public Vector3 thumbReferencePoint;
        private Vector3 targetDirection;
        private float sensitivity;
        private Dictionary<XRHandJointID, Pose> jointLocations;

        public HandJointEstimatedData(XRHandFingerID? closestFinger, XRHandJointID closestJoint, Vector3 vectorToFingerTip, Vector3 vectorToFingerProximal, float sensitivity, Vector3 targetDirection, Vector3 thumbReferencePoint, Dictionary<XRHandJointID, Pose> jointLocations)
        {
            _closestFinger = closestFinger;
            _closestJoint = closestJoint;
            this.vectorToFingerTip = vectorToFingerTip;
            this.vectorToFingerProximal = vectorToFingerProximal;
            this.sensitivity = sensitivity;
            this.thumbReferencePoint = thumbReferencePoint;
            this.jointLocations = jointLocations;
        }

        public bool IsDataValid()
        {
            return true;
        }

        public float GetTipWeight()
        {
            float distTip = vectorToFingerTip.magnitude;
            float distProximal = vectorToFingerProximal.magnitude;
            // Apply nonlinear falloff so small differences have less impact
            float tipScore = Mathf.Pow(1f / (distTip + 0.000001f), sensitivity);
            float proximalScore = Mathf.Pow(1f / (distProximal + 0.000001f), sensitivity);
            float totalScore = tipScore + proximalScore;
            float tipWeight = tipScore / totalScore;
            return tipWeight;
        }

        public float GetPlaneOnFingerPlane(XRHandFingerID fingerID)
        {
            Vector3 v1 = Vector3.zero;
            Vector3 v2 = Vector3.one;
            switch (fingerID)
            {
                case XRHandFingerID.Index:
                    v1 = jointLocations[XRHandJointID.IndexProximal].forward;
                    v2 = jointLocations[XRHandJointID.IndexProximal].up;
                    break;
                case XRHandFingerID.Middle:
                    v1 = jointLocations[XRHandJointID.MiddleProximal].forward;
                    v2 = jointLocations[XRHandJointID.MiddleProximal].right;
                    break;
                case XRHandFingerID.Ring:
                    v1 = jointLocations[XRHandJointID.RingProximal].forward;
                    v2 = jointLocations[XRHandJointID.RingProximal].right;
                    break;
                case XRHandFingerID.Little:
                    v1 = jointLocations[XRHandJointID.LittleProximal].forward;
                    v2 = jointLocations[XRHandJointID.LittleProximal].right;
                    break;
            }
            Vector3 planeNormal = Vector3.Cross(v1, v2).normalized;
            Vector3 localTargetDir = -new Vector3(targetDirection.x, targetDirection.y, targetDirection.z);
            float signedAngle = Vector3.SignedAngle(localTargetDir, planeNormal.normalized, jointLocations[XRHandJointID.IndexProximal].forward);
            Debug.DrawRay(jointLocations[XRHandJointID.IndexProximal].position, planeNormal, Color.yellow);
            Debug.DrawRay(jointLocations[XRHandJointID.IndexProximal].position, localTargetDir, Color.magenta);
            Debug.DrawRay(jointLocations[XRHandJointID.IndexProximal].position, ProjectOnPlane(targetDirection, planeNormal), Color.black);
            return AngleToPlane(targetDirection, planeNormal);
        }

        Vector3 ProjectOnPlane(Vector3 v, Vector3 normal)
        {
            return v - Vector3.Dot(v, normal.normalized) * normal.normalized;
        }

        // Returns the angle (in degrees) between the vector and the plane
        float AngleToPlane(Vector3 v, Vector3 normal)
        {
            float dot = Vector3.Dot(v.normalized, normal.normalized);
            // angle with plane = 90 - angle with normal
            float angle = Mathf.Asin(Mathf.Abs(dot)) * Mathf.Rad2Deg;
            return angle;
        }

        public float GetProximalWeight()
        {
            float distTip = vectorToFingerTip.magnitude;
            float distProximal = vectorToFingerProximal.magnitude;
            // Apply nonlinear falloff so small differences have less impact
            float tipScore = Mathf.Pow(1f / (distTip + 0.000001f), sensitivity);
            float proximalScore = Mathf.Pow(1f / (distProximal + 0.000001f), sensitivity);
            float totalScore = tipScore + proximalScore;
            float proximalWeight = proximalScore / totalScore;
            return proximalWeight;
        }

        private Vector3 GetTargetDirection()
        {
            float tipWeight = GetTipWeight();
            float proximalWeight = GetProximalWeight();
            targetDirection = (tipWeight * vectorToFingerTip + proximalWeight * vectorToFingerProximal);
            return targetDirection;
        }

        public Vector3 GetTargetDirection(float tiltRotation, float flatRotation)
        {
            targetDirection = GetTargetDirection();

            Vector3 planeNormal = Vector3.Cross(vectorToFingerProximal, vectorToFingerTip).normalized;
            // Rotate ALONG the plane (spin flat)
            targetDirection -= Vector3.Dot(targetDirection, planeNormal) * planeNormal; // project into plane
            Quaternion alongPlaneRot = Quaternion.AngleAxis(-flatRotation, planeNormal);
            targetDirection = alongPlaneRot * targetDirection;

            // Rotate PERPENDICULAR to the plane (tilt out)
            Vector3 perpendicularAxis = Vector3.Cross(planeNormal, targetDirection).normalized;
            Quaternion perpendicularRot = Quaternion.AngleAxis(tiltRotation, perpendicularAxis);
            targetDirection = perpendicularRot * targetDirection;

            return targetDirection;
        }
    }
}
