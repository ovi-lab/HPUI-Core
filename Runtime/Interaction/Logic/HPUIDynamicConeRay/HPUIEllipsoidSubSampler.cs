using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public class HPUIEllipsoidSubSampler : IHPUIRaySubSampler
    {
        private float angleStep = 5f;

        public float AngleStep
        {
            get => angleStep;
            set => angleStep = Mathf.Max(1, Mathf.Min(value, 180));
        }

        [SerializeField, Range(1f, 180f)]
        [Tooltip("Angular Width of the cone from the target direction")]
        float coneAngularWidth = 45f;

        public float ConeAngularWidth
        {
            get => coneAngularWidth;
            set => angleStep = Mathf.Max(1, Mathf.Min(value, 180));
        }

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Target radius selected from cone ray data per phalange")]
        private float percentileSelectionForRadiusLength = 0.35f;

        public float PercentileSelectionForRadiusLength
        {
            get => percentileSelectionForRadiusLength;
            set => Mathf.Max(0f, Mathf.Min(percentileSelectionForRadiusLength, 1f));
        }

        [SerializeField] private HPUIInteractorConeRayAngles coneRayData;

        [SerializeField] private bool visualiseEllipsoid = false;

        Dictionary<(XRHandJointID, FingerSide), float> xrConeRayAngleMedian = new();

        private XRHandFingerID previousFingerID = XRHandFingerID.Thumb;
        [SerializeField] private bool recacheAngles;

        List<HPUIInteractorRayAngle> allAngles = new();
        List<Vector3> spherePoints = new();
        private float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);
        private int _cachedSamples;
        private float numberOfSamples;
        private float targetRadius;

        [SerializeField] private FingerSide currentFingerSide;

        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sampling Rays");
            if (recacheAngles)
            {
                CacheRayAngles(estimatedData, interactorObject);
                numberOfSamples = Mathf.Pow(360f / angleStep, 2);
                CacheSphere(numberOfSamples);
                recacheAngles = false;
            }

            if (previousFingerID != estimatedData._closestFinger.Value)
            {
                numberOfSamples = Mathf.Pow(360f / angleStep, 2);
                CacheRayAngles(estimatedData, interactorObject);
            }

            allAngles.Clear();
            if (estimatedData.GetPlaneOnFingerPlane(estimatedData._closestFinger.Value) > 25)
            {
                currentFingerSide = FingerSide.radial;
            }
            else
            {
                currentFingerSide = FingerSide.volar;
            }

            float distalRadius = xrConeRayAngleMedian[(XRHandJointID.IndexDistal, currentFingerSide)];
            float intermediateRadius = xrConeRayAngleMedian[(XRHandJointID.IndexIntermediate, currentFingerSide)];
            float proximalRadius = xrConeRayAngleMedian[(XRHandJointID.IndexProximal, currentFingerSide)];
            targetRadius = LerpThreeSmooth(distalRadius, intermediateRadius, proximalRadius, estimatedData.GetTipWeight());
            float cosMaxAngle = Mathf.Cos(coneAngularWidth * Mathf.Deg2Rad);
            Vector3 targetDir = estimatedData.TargetDirection.normalized;

            Vector3 localTargetDir = interactorObject.InverseTransformDirection(targetDir).normalized;
            Quaternion rotationToTarget = Quaternion.FromToRotation(Vector3.forward, localTargetDir);
            float xAngle, zAngle, distance;
            Vector3 rotatedDir, ellipsoidPoint;
            Vector3 forward = Vector3.forward;
            for (int i = 0; i < spherePoints.Count; i++)
            {
                Vector3 spherePoint = spherePoints[i];
                // Rotate point so cone is aligned to targetDir
                rotatedDir = rotationToTarget * spherePoint;
                // Stretch to ellipsoid dimensions
                ellipsoidPoint = new Vector3(rotatedDir.x * targetRadius, rotatedDir.y * targetRadius, rotatedDir.z * targetRadius);
                if (visualiseEllipsoid)
                {
                    Debug.DrawLine(
                        interactorObject.position,
                        interactorObject.position + interactorObject.TransformDirection(ellipsoidPoint),
                        Color.yellow
                    );
                }
                if (Vector3.Dot(forward, spherePoint) < cosMaxAngle)
                    continue;
                xAngle = Mathf.Rad2Deg * Mathf.Atan2(ellipsoidPoint.z, ellipsoidPoint.y);
                zAngle = Mathf.Rad2Deg * Mathf.Atan2(ellipsoidPoint.x, ellipsoidPoint.y);
                distance = ellipsoidPoint.magnitude;
                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle, distance));
            }

            UnityEngine.Profiling.Profiler.EndSample();
            previousFingerID = estimatedData._closestFinger.Value;
            return allAngles;
        }

        private void CacheSphere(float numberOfSamples)
        {
            float x, y, z, radius, theta;
            Vector3 spherePoint;
            spherePoints.Clear();
            for (int i = 0; i < numberOfSamples; i++)
            {
                y = 1f - (i / (numberOfSamples - 1f)) * 2f;
                radius = Mathf.Sqrt(1f - y * y);
                theta = phi * i;
                x = Mathf.Cos(theta) * radius;
                z = Mathf.Sin(theta) * radius;
                spherePoint = new Vector3(x, y, z).normalized;
                spherePoints.Add(spherePoint);
            }
        }

        public static float LerpThreeSmooth(float a, float b, float c, float t)
        {
            // Debug.Log($"distal:{a}  intermediate:{b} volar:{c}");
            float u = (1 - t) * (1 - t);   // weight for a
            float v = 2 * (1 - t) * t;     // weight for b
            float w = t * t;               // weight for c

            return a * u + b * v + c * w;
        }

        public void CacheRayAngles(HandJointEstimatedData estimatedData, Transform interactorObject)
        {
            switch (estimatedData._closestFinger.Value)
            {
                case XRHandFingerID.Index:
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexDistalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.IndexDistal);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexIntermediateAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.IndexIntermediate);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexProximalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.IndexProximal);
                    }
                    break;

                case XRHandFingerID.Middle:
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleDistalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.MiddleDistal);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleIntermediateAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.MiddleIntermediate);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleProximalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.MiddleProximal);
                    }
                    break;

                case XRHandFingerID.Ring:
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingDistalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.RingDistal);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingIntermediateAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.RingIntermediate);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingProximalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.RingProximal);
                    }
                    break;

                case XRHandFingerID.Little:
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleDistalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.LittleDistal);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleIntermediateAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.LittleIntermediate);
                    }
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleProximalAngles)
                    {
                        CacheJointAndSideAngle(side, XRHandJointID.LittleProximal);
                    }
                    break;

                case XRHandFingerID.Thumb:
                    Debug.LogError("Thumb should never be the closest finger?");
                    break;

            }
        }

        public void CacheJointAndSideAngle(HPUIInteractorConeRayAngleSides phalangeConeData, XRHandJointID targetJoint)
        {
            List<float> targetJointRayList = phalangeConeData.rayAngles
                .Select(x => x.RaySelectionThreshold)
                .ToList();

            float targetRadiusLength = targetJointRayList.Count == 0 ? 0 : targetJointRayList.Percentile(percentileSelectionForRadiusLength);

            if (xrConeRayAngleMedian.ContainsKey((targetJoint, phalangeConeData.side)))
            {
                xrConeRayAngleMedian[(targetJoint, phalangeConeData.side)] = targetRadiusLength;
            }
            else
            {
                xrConeRayAngleMedian.Add((targetJoint, phalangeConeData.side), targetRadiusLength);
            }
        }

        public void Dispose()
        {

        }
    }
}
