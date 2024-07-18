using System.Collections.Generic;
using UnityEngine;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the cone ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorRayAngles", menuName = "HPUI/HPUI Interactor Ray Angles", order = 1)]
    public class HPUIInteractorRayAngles: ScriptableObject
    {
        // TODO: Cite source!
        // These are computed based on data collected during studies

        public List<HPUIInteractorRayAngle> IndexAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> MiddleAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> RingAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> LittleAngles = new List<HPUIInteractorRayAngle>();
    }

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
 
