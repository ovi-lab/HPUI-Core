using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the cone ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorConeRayAngles", menuName = "HPUI/HPUI Interactor Cone Ray Angles", order = 1)]
    public class HPUIInteractorConeRayAngles: ScriptableObject
    {
        // TODO: Cite source!
        // These are computed based on data collected during studies

        public List<HPUIInteractorRayAngle> IndexAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> MiddleAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> RingAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> LittleAngles = new List<HPUIInteractorRayAngle>();
    }
}
 
