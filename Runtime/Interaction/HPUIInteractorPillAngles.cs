using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    [CreateAssetMenu(fileName = "HPUIInteractorPillAngles", menuName = "HPUI/HPUI Interactor Pill Angles", order = 3)]
    public class HPUIInteractorPillAngles : HPUIInteractorFullRangeAngles
    {
        public static List<HPUIInteractorRayAngle> ComputeAngles(int maxAngle, int angleStep, float a, float b, float c)
        {
            List<HPUIInteractorRayAngle> allAngles = new();
            float numberOfSamples = Mathf.Pow(360 / angleStep, 2);
            float phi = Mathf.PI * (Mathf.Sqrt(5) - 1);
            float yMin = Mathf.Cos(Mathf.Min(maxAngle * Mathf.Deg2Rad));

            for (int i = 0; i < numberOfSamples; i++)
            {
                float y = 1 - (i / (numberOfSamples - 1)) * 2;
                if (y < yMin)
                    break;

                float radius = Mathf.Sqrt(1 - y * y);
                float theta = phi * i;
                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;

                // Stretch point to ellipsoid dimensions
                Vector3 ellipsoidPoint = new Vector3(x * a, y * b, z * c);

                float xAngle = Vector3.Angle(Vector3.up, new Vector3(0, ellipsoidPoint.y, ellipsoidPoint.z)) * (ellipsoidPoint.z < 0 ? -1 : 1);
                float zAngle = Vector3.Angle(Vector3.up, new Vector3(ellipsoidPoint.x, ellipsoidPoint.y, 0)) * (ellipsoidPoint.x < 0 ? -1 : 1);
                float distance = ellipsoidPoint.magnitude;

                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle, distance));
            }

            return allAngles;
        }
    }
}

