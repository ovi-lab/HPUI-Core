using UnityEngine;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public struct HPUIInteractorRayAngle
    {
        public float x, z;

        public HPUIInteractorRayAngle(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        public static Vector3 GetDirection(float x, float z, Vector3 right, Vector3 forward, Vector3 up, bool flipZAngles)
        {
            float x_ = x,
                  z_ = flipZAngles ? -z : z;

            Quaternion rotation = Quaternion.AngleAxis(x_, right) * Quaternion.AngleAxis(z_, forward);
            return rotation * up;
            // return 
        }

        public Vector3 GetDirection(Transform attachTransform, bool flipZAngles)
        {
            return GetDirection(x, z, attachTransform.right, attachTransform.forward, attachTransform.up, flipZAngles);
        }
    }
}
 
