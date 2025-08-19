using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;


namespace ubco.ovilab.HPUI.Interaction
{

    [Serializable]

    public class HPUIEllipsoidSubSampler : IHPUIRaySubSampler
    {
        [Header("Pill Properties")]
        [SerializeField] [Tooltip("Length along local X")]
        float radiusA = 0.025f;
        [SerializeField] [Tooltip("Length along local Y")]
        float radiusB = 0.015f;
        [SerializeField] [Tooltip("Length along local Z")]
        float radiusC = 0.02f;
        [SerializeField] [Tooltip("Scaling factor of rays at index distal")]
        private float initalScalingFactor = 1f;
        [SerializeField] [Tooltip("Scaling factor of rays at index proximal")]
        float scalingFactor = 1.2f;

        [SerializeField] [Tooltip("Rays are created at every X degrees. Higher value requires more compute")]
        float angleStep = 5f;

        [SerializeField] [Tooltip("Angular Width of the cone from the target direction")]
        float coneAngularWidth = 45f;

        [SerializeField] private bool visualiseEllipsoid = false;
        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData)
        {

            List<HPUIInteractorRayAngle> allAngles = new();
            float numberOfSamples = Mathf.Pow(360f / angleStep, 2);
            float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);
            float scale = Mathf.Lerp(initalScalingFactor, scalingFactor, estimatedData.GetProximalWeight());
            // Your cone limit in degrees
            float maxAngleDeg = 45f; // example
            float cosMaxAngle = Mathf.Cos(maxAngleDeg * Mathf.Deg2Rad);
            // Direction from ellipsoid center you want rays near
            Vector3 targetDir = estimatedData.TargetDirection.normalized;
            Vector3 localTargetDir = interactorObject.InverseTransformDirection(targetDir).normalized;
            Quaternion rotationToTarget = Quaternion.FromToRotation(Vector3.forward, localTargetDir);
            for (int i = 0; i < numberOfSamples; i++)
            {
                float y = 1f - (i / (numberOfSamples - 1f)) * 2f;
                float radius = Mathf.Sqrt(1f - y * y);
                float theta = phi * i;
                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;
                Vector3 spherePoint = new Vector3(x, y, z);
                // Rotate point so cone is aligned to targetDir
                Vector3 rotatedDir = rotationToTarget * spherePoint;
                // Stretch to ellipsoid dimensions
                Vector3 ellipsoidPoint = new Vector3(rotatedDir.x * radiusA, rotatedDir.y * radiusB, rotatedDir.z * radiusC);
                if (visualiseEllipsoid)
                {
                    Debug.DrawLine(
                        interactorObject.position,
                        interactorObject.position + interactorObject.TransformDirection(ellipsoidPoint),
                        Color.yellow
                    );
                }
                if (Vector3.Dot(Vector3.forward, spherePoint.normalized) < cosMaxAngle)
                    continue;
                // Angles + distance
                float xAngle = Vector3.Angle(Vector3.up, new Vector3(0f, ellipsoidPoint.y, ellipsoidPoint.z)) * (ellipsoidPoint.z < 0f ? -1f : 1f);
                float zAngle = Vector3.Angle(Vector3.up, new Vector3(ellipsoidPoint.x, ellipsoidPoint.y, 0f)) * (ellipsoidPoint.x < 0f ? -1f : 1f);
                float distance = ellipsoidPoint.magnitude;
                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle, distance));
            }
            return allAngles;
        }

        public void Dispose()
        {

        }


    }
}
