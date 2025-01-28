using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the FullRange ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorFullRangeAngles", menuName = "HPUI/HPUI Interactor Full Ray Angles", order = 1)]
    public class HPUIInteractorFullRangeAngles: ScriptableObject
    {
        public List<Vector3> RightHandAngles
        {
            get
            {
                if (rightHandAngles.Count == angles.Count)
                {
                    return rightHandAngles;
                }
                else
                {
                    CacheAngles();
                    return rightHandAngles;
                }
            }
        } 
        
        public List<Vector3> LeftHandAngles
        {
            get
            {
                if (leftHandAngles.Count == angles.Count)
                {
                    return rightHandAngles;
                }
                else
                {
                    CacheAngles();
                    return rightHandAngles;
                }
            }
        }
        
        public List<HPUIInteractorRayAngle> angles;
        [SerializeField] private List<Vector3> rightHandAngles = new List<Vector3>();
        [SerializeField] private List<Vector3> leftHandAngles = new List<Vector3>();
        // FIXME: Compute this on the fly and store it

        public static List<HPUIInteractorRayAngle> ComputeAngles(int maxAngle, int angleStep, float raySelectionThreshold)
        {
            List<HPUIInteractorRayAngle> allAngles = new();

            float numberOfSamples = Mathf.Pow(360 / angleStep, 2);
            List<Vector3> spericalPoints = new();
            float phi = Mathf.PI * (Mathf.Sqrt(5) - 1);

            float yMin = Mathf.Cos(Mathf.Min(maxAngle * Mathf.Deg2Rad));

            for(int i=0; i < numberOfSamples ; i++)
            {
                float y = 1 - (i / (numberOfSamples - 1)) * 2;
                if (y < yMin)
                {
                    break;
                }

                float radius = Mathf.Sqrt(1 - y * y);

                float theta = phi * i;

                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;

                Vector3 point = new Vector3(x, y, z);

                float xAngle = Vector3.Angle(Vector3.up, new Vector3(0, y, z)) * (z < 0 ? -1: 1);
                float zAngle = Vector3.Angle(Vector3.up, new Vector3(x, y, 0)) * (x < 0 ? -1: 1);

                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle, raySelectionThreshold));
            }
            return allAngles;
        }


        private void Awake()
        {
            CacheAngles();
        }

        private void CacheAngles()
        {
            foreach (HPUIInteractorRayAngle angleData in angles)
            {
                rightHandAngles.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, false));
                leftHandAngles.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, true));
            }
        }
        
    }
    
}
 
