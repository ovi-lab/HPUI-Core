using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the cone ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorConeRayAngles", menuName = "HPUI/HPUI Interactor Cone Ray Angles", order = 1)]
    public class HPUIInteractorConeRayAngles: ScriptableObject
    {
        public Dictionary<XRHandJointID, List<HPUIInteractorRayAngle>> ActiveFingerAngles;
        // TODO: Cite source!
        // These are computed based on data collected during studies
        public List<HPUIInteractorRayAngle> IndexDistalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> IndexIntermediateAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> IndexProximalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> MiddleDistalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> MiddleIntermediateAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> MiddleProximalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> RingDistalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> RingIntermediateAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> RingProximalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> LittleDistalAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> LittleIntermediateAngles = new List<HPUIInteractorRayAngle>();
        public List<HPUIInteractorRayAngle> LittleProximalAngles = new List<HPUIInteractorRayAngle>();


        public HPUIInteractorConeRayAngles()
        {
            ActiveFingerAngles = new()
            {
                { XRHandJointID.IndexProximal, IndexProximalAngles },
                { XRHandJointID.IndexIntermediate, IndexIntermediateAngles },
                { XRHandJointID.IndexDistal, IndexDistalAngles },
                { XRHandJointID.MiddleProximal, MiddleProximalAngles },
                { XRHandJointID.MiddleIntermediate, MiddleIntermediateAngles },
                { XRHandJointID.MiddleDistal, MiddleDistalAngles },
                { XRHandJointID.RingProximal, RingProximalAngles },
                { XRHandJointID.RingIntermediate, RingIntermediateAngles },
                { XRHandJointID.RingDistal, RingDistalAngles },
                { XRHandJointID.LittleProximal, LittleProximalAngles },
                { XRHandJointID.LittleIntermediate, LittleIntermediateAngles },
                { XRHandJointID.LittleDistal, LittleDistalAngles }
            };
        }
    }

}

