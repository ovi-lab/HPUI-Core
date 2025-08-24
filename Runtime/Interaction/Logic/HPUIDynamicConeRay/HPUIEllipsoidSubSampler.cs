using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;
using UXF.UI;


namespace ubco.ovilab.HPUI.Interaction
{

    [Serializable]

    public class HPUIEllipsoidSubSampler : IHPUIRaySubSampler
    {
        // [Header("Pill Properties")] [SerializeField] [Tooltip("Length along local X")]
        // float radiusA = 1f;
        //
        // [SerializeField] [Tooltip("Length along local Y")]
        // float radiusB = 1f;
        //
        // [SerializeField] [Tooltip("Length along local Z")]
        // float radiusC = 1f;
        //
        // [SerializeField] [Tooltip("Scaling factor of rays at index distal")]
        // private float initalScalingFactor = 1f;
        //
        // [SerializeField] [Tooltip("Scaling factor of rays at index proximal")]
        // float scalingFactor = 1.2f;
        [SerializeField] private float targetRadius = 0;
        [SerializeField] [Tooltip("Rays are created at every X degrees. Higher value requires more compute")]
        float angleStep = 5f;

        [SerializeField] [Tooltip("Angular Width of the cone from the target direction")]
        float coneAngularWidth = 45f;

        [SerializeField] private HPUIInteractorConeRayAngles coneRayData;
        [SerializeField] private bool visualiseEllipsoid = false;

        [SerializeField]
        Dictionary<(XRHandJointID, FingerSide), float> xrConeRayAngleMedian = new();

        [SerializeField] private float percentile = 35f;
        private XRHandFingerID previousFingerID = XRHandFingerID.Thumb;
        [SerializeField] private bool recacheAngles;

        List<HPUIInteractorRayAngle> allAngles = new();
        List<Vector3> spherePoints = new();
        private float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);
        private int _cachedSamples;
        private float numberOfSamples;

        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sampling Rays");
            if (recacheAngles)
            {
                CacheRayAngles(estimatedData, interactorObject);
                CacheSphere(numberOfSamples);
                recacheAngles = false;
                numberOfSamples = Mathf.Pow(360f / angleStep, 2);
            }

            if (previousFingerID != estimatedData._closestFinger.Value)
            {
                numberOfSamples = Mathf.Pow(360f / angleStep, 2);
                CacheRayAngles(estimatedData, interactorObject);
                CacheSphere(numberOfSamples);
            }
            
            allAngles.Clear();
            if(estimatedData.GetPlaneOnFingerPlane(estimatedData._closestFinger.Value) > 30)
            {
                FingerSide targetSide = FingerSide.radial;
                float distalRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexDistal, targetSide)];
                float intermediateRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexIntermediate, targetSide)];
                float proximalRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexProximal, targetSide)];
                targetRadius = LerpThreeSmooth(distalRadius, intermediateRadius, proximalRadius, estimatedData.GetTipWeight());
            }
            else
            {
                FingerSide targetSide = FingerSide.volar;
                float distalRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexDistal, targetSide)];
                float intermediateRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexIntermediate, targetSide)];
                float proximalRadius  = xrConeRayAngleMedian[(XRHandJointID.IndexProximal, targetSide)];
                targetRadius = LerpThreeSmooth(distalRadius, intermediateRadius, proximalRadius, estimatedData.GetTipWeight());
            }
            // Your cone limit in degrees
            float cosMaxAngle = Mathf.Cos(coneAngularWidth * Mathf.Deg2Rad);
            // Direction from ellipsoid center you want rays near
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
            Debug.Log("Cache Ray Angles");
            switch (estimatedData._closestFinger.Value)
            {
                case XRHandFingerID.Index:
                {
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
                }

                case XRHandFingerID.Thumb:
                {
                    Debug.LogError("Thumb should never be the closest finger?");
                    break;
                }
            }
        }

        public void CacheJointAndSideAngle(HPUIInteractorConeRayAngleSides jointConeData, XRHandJointID targetJoint)
        {
            List<float> values = jointConeData.rayAngles.Select(x => x.RaySelectionThreshold).OrderBy(x => x).ToList();
            List<float> binnedLengths = BinValues(values, 0.001f);
            // float medianSelectionThreshold = GetMedian(binnedLengths);// GetPercentile(binnedLengths, 25f);
            float medianSelectionThreshold = GetPercentile(binnedLengths, percentile);
            if (xrConeRayAngleMedian.ContainsKey((targetJoint, jointConeData.side)))
            {
                xrConeRayAngleMedian[(targetJoint, jointConeData.side)] = medianSelectionThreshold;
            }
            else
            {
                xrConeRayAngleMedian.Add((targetJoint, jointConeData.side), medianSelectionThreshold);
            }
        }

        public static List<float> BinValues(List<float> data, float binSize)
        {
            // Bin and return one representative per bin (bin center)
            return data
                .GroupBy(v => (float)Math.Floor(v / binSize))
                .Select(g => g.Key * binSize + binSize / 2f) // center of bin
                .OrderBy(v => v)
                .ToList();
        }
        private float GetPercentile(List<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0) return 0;

            float position = (sortedValues.Count + 1) * percentile / 100f;
            int index = Mathf.FloorToInt(position);

            if (index < 1) return sortedValues[0];
            if (index >= sortedValues.Count) return sortedValues[sortedValues.Count - 1];

            float fraction = position - index;
            return sortedValues[index - 1] + fraction * (sortedValues[index] - sortedValues[index - 1]);
        }
        static float GetMedian(List<float> numbers)
        {
            if (numbers == null || numbers.Count == 0)
                throw new InvalidOperationException("List must not be empty.");

            var sorted = numbers.OrderBy(n => n).ToList();
            int count = sorted.Count;
            int mid = count / 2;

            if (count % 2 == 0)
            {
                // Even count: average of two middle values
                return (sorted[mid - 1] + sorted[mid]) / 2f;
            }
            else
            {
                // Odd count: middle value
                return sorted[mid];
            }
        }
        public void Dispose()
        {

        }
    }
}