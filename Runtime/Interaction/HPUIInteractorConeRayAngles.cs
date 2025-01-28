using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the cone ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorConeRayAngles", menuName = "HPUI/HPUI Interactor Cone Ray Angles",
        order = 1)]
    public class HPUIInteractorConeRayAngles : ScriptableObject
    {
        public SerializableDictionary<XRHandJointID, List<Vector3>> RightHandAngles
        {
            get
            {
                //Not the ideal way to validate but good enough ¯\_(ツ)_/¯
                if (rightHandAngles.Count == joints.Count)
                {
                    return rightHandAngles;
                }
                CacheAngles();
                return rightHandAngles;
            }
        }
        
        public SerializableDictionary<XRHandJointID, List<Vector3>> LeftHandAngles
        {
            get
            {
                //Not the ideal way to validate but good enough ¯\_(ツ)_/¯
                if (leftHandAngles.Count == joints.Count)
                {
                    return leftHandAngles;
                }
                CacheAngles();
                return leftHandAngles;
            }
        }
        
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

        [SerializeField] private SerializableDictionary<XRHandJointID, List<Vector3>> rightHandAngles = new();
        [SerializeField] private SerializableDictionary<XRHandJointID, List<Vector3>> leftHandAngles = new();
        
        
        private List<XRHandJointID> joints = new()
        {
            XRHandJointID.IndexProximal,
            XRHandJointID.IndexIntermediate,
            XRHandJointID.IndexDistal,
            XRHandJointID.MiddleProximal,
            XRHandJointID.MiddleIntermediate,
            XRHandJointID.MiddleDistal,
            XRHandJointID.RingProximal,
            XRHandJointID.RingIntermediate,
            XRHandJointID.RingDistal,
            XRHandJointID.LittleProximal,
            XRHandJointID.LittleIntermediate,
            XRHandJointID.LittleDistal
        };

        

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

        public void CacheAngles()
        {
            rightHandAngles.Clear();
            leftHandAngles.Clear();
            foreach (XRHandJointID joint in joints)
            {
                Debug.Log("Caching angles");
                List<HPUIInteractorRayAngle> jointAngles = joint switch
                {
                    XRHandJointID.IndexProximal => IndexProximalAngles,
                    XRHandJointID.IndexIntermediate => IndexIntermediateAngles,
                    XRHandJointID.IndexDistal => IndexDistalAngles,
                    XRHandJointID.MiddleProximal => MiddleProximalAngles,
                    XRHandJointID.MiddleIntermediate => MiddleIntermediateAngles,
                    XRHandJointID.MiddleDistal => MiddleDistalAngles,
                    XRHandJointID.RingProximal => RingProximalAngles,
                    XRHandJointID.RingIntermediate => RingIntermediateAngles,
                    XRHandJointID.RingDistal => RingDistalAngles,
                    XRHandJointID.LittleProximal => LittleProximalAngles,
                    XRHandJointID.LittleIntermediate => LittleIntermediateAngles,
                    XRHandJointID.LittleDistal => LittleDistalAngles,
                    _ => throw new System.InvalidOperationException($"Unknown joint,")
                };
                
                List<Vector3> processedAnglesRight = new();
                List<Vector3> processedAnglesLeft = new();
                
                foreach (HPUIInteractorRayAngle angleData in jointAngles)
                {
                    processedAnglesRight.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, false));
                    processedAnglesLeft.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, true));
                }
                
                rightHandAngles.Add(joint, processedAnglesRight);
                leftHandAngles.Add(joint, processedAnglesLeft);
            }
        }
    }

}
 
