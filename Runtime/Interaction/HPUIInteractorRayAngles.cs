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
    }
}
 
