using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using NUnit.Framework;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{

    [Serializable]
    public class HPUICalibratedConeSubSampler : IHPUIRaySubSampler
    {
        [SerializeField] private HPUIInteractorConeRayAngles coneRayData;
        [SerializeField] private float coneAngle = 45f;
        [SerializeField] private float lowerPercentile = 0f;
        [SerializeField] private float upperPercentile = 90f;
        [SerializeField] private float IQRThreshold = 0.4f;
        [SerializeField] private float IQRThreshold2 = 0.8f;
        [SerializeField] private bool visualiseAllRays;

        private XRHandFingerID previousFingerID = XRHandFingerID.Thumb;
        private List<HPUIInteractorRayAngle> fingerRelevantRays = new();
        private List<Vector3> rayDirections = new();
        private float Q1;
        private float Q3;
        private float IQR;
        private float lowerBound;
        private float upperBound;
        [SerializeField] private float angleToPlane = 0f;
        [SerializeField] private float clampValue;

        private Dictionary<XRHandFingerID, List<XRHandJointID>> fingerToJoints = new()
        {
            { XRHandFingerID.Index, new() { XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip } },
            { XRHandFingerID.Middle, new() { XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip } },
            { XRHandFingerID.Ring, new() { XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip } },
            { XRHandFingerID.Little, new() { XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip } }
        };


        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sampling Rays");
            if (estimatedData._closestFinger == null)
            {
                Debug.LogWarning("No finger joints found.");
                return fingerRelevantRays;
            }

            if (previousFingerID != estimatedData._closestFinger.Value)
            {
                fingerRelevantRays = CacheRayAngles(estimatedData, interactorObject);
            }

            Vector3 targetDir = estimatedData.TargetDirection.normalized;
            Vector3 localTargetDir = interactorObject.InverseTransformDirection(targetDir).normalized;
            Debug.DrawRay(interactorObject.position, targetDir, Color.green);
            List<HPUIInteractorRayAngle> filteredRays = new();
            float targetThreshold = Mathf.Lerp(Q1, Q3, estimatedData.GetProximalWeight());
            float IQRMultiplier = Mathf.Lerp(IQRThreshold, IQRThreshold2, estimatedData.GetProximalWeight());
            float thresholdRange = IQR * IQRMultiplier;
            float lowerbound = targetThreshold - thresholdRange;
            float upperbound = lowerbound + thresholdRange;
            lowerbound = Mathf.Clamp(lowerbound, Q1, Q3);
            upperbound = Mathf.Clamp(upperbound, Q1, Q3);
            // Debug.Log($"Target Length:{targetThreshold} LowerBound:{lowerbound} UpperBound:{upperbound}");
            float cosMaxAngle = Mathf.Cos(coneAngle * Mathf.Deg2Rad);
            angleToPlane = estimatedData.GetPlaneOnFingerPlane(estimatedData._closestFinger.Value);
            float radialClampParameter = Mathf.Clamp(angleToPlane, 0,90)/ 90;
            clampValue = Mathf.Lerp(upperbound, Q1, radialClampParameter);
            for (int i = 0; i < fingerRelevantRays.Count; i++)
            {
                if (visualiseAllRays)
                {

                    Debug.DrawLine(
                        interactorObject.position,
                        interactorObject.position + interactorObject.TransformDirection(rayDirections[i]),
                        Color.yellow
                    );
                }
                HPUIInteractorRayAngle ray = fingerRelevantRays[i];
                Vector3 direction = rayDirections[i];
                if (Vector3.Dot(localTargetDir.normalized, direction.normalized) < cosMaxAngle)
                    continue;
                if (ray.RaySelectionThreshold < lowerbound || ray.RaySelectionThreshold > upperbound)
                    continue;
                if (angleToPlane > 35)
                {
                    var hpuiInteractorRayAngle = new HPUIInteractorRayAngle(ray.X, ray.Z, Mathf.Clamp(ray.RaySelectionThreshold, 0, Q1));
                    filteredRays.Add(hpuiInteractorRayAngle);
                    continue;
                }
                filteredRays.Add(ray);
            }
            UnityEngine.Profiling.Profiler.EndSample();
            previousFingerID = estimatedData._closestFinger.Value;
            return filteredRays;
        }

        [Button]
        public List<HPUIInteractorRayAngle> CacheRayAngles(HandJointEstimatedData estimatedData, Transform interactorObject)
        {
            rayDirections.Clear();
            List<HPUIInteractorRayAngle> rays = new List<HPUIInteractorRayAngle>();
            switch (estimatedData._closestFinger.Value)
            {
                case XRHandFingerID.Index:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Middle:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Ring:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Little:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Thumb:
                {
                    Debug.LogError("Thumb should never be the closest finger?");
                    break;
                }
            }
            List<float> values = fingerRelevantRays.Select(x => x.RaySelectionThreshold).OrderBy(x => x).ToList();
            float bandwidth = 0.005f;
            values = BinValues(values, bandwidth);
            Q1 = GetPercentile(values, lowerPercentile);
            Q3 = GetPercentile(values, upperPercentile);
            IQR = Q3 - Q1;
            foreach (HPUIInteractorRayAngle ray in fingerRelevantRays)
            {
                rayDirections.Add(ray.GetDirection(false) * ray.RaySelectionThreshold);
            }

            return rays;
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
        public void Dispose()
        {

        }

    }
}