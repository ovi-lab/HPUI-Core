using System.Collections.Generic;
using UnityEngine;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public struct HPUIInteractorRayAngle
    {
        public int x, z;

        public HPUIInteractorRayAngle(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public static Vector3 GetDirection(int x, int z, Vector3 right, Vector3 forward, Vector3 up, bool flipZAngles)
        {
            int x_ = x,
                z_ = flipZAngles ? -z : z;

            Quaternion rotation = Quaternion.AngleAxis(x_, right) * Quaternion.AngleAxis(z_, forward);
            return rotation * up;
        }

        public Vector3 GetDirection(Transform attachTransform, bool flipZAngles)
        {
            return GetDirection(x, z, attachTransform.right, attachTransform.forward, attachTransform.up, flipZAngles);
        }
    }
}
 
