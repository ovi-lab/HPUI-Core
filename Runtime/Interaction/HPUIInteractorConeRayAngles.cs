using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Contains the angles for the cone ray cast to be used with the <see cref="HPUIInteractor"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HPUIInteractorConeRayAngles", menuName = "HPUI/HPUI Interactor Cone Ray Angles", order = 1)]
    public class HPUIInteractorConeRayAngles: ScriptableObject
    {
        [SerializeField, Tooltip("The side of the finger to fall back when a side is missing.")]
        private FingerSide fallbackSide = FingerSide.volar;

        /// <summary>
        /// The side of the finger to fall back when a side is missing.
        /// </summary>
        public FingerSide FallbackSide { get => fallbackSide; set => fallbackSide = value; }

        // TODO: Cite source!
        // These are computed based on data collected during studies
        public List<HPUIInteractorConeRayAngleSides> IndexDistalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> IndexIntermediateAngles = new();
        public List<HPUIInteractorConeRayAngleSides> IndexProximalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> MiddleDistalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> MiddleIntermediateAngles = new();
        public List<HPUIInteractorConeRayAngleSides> MiddleProximalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> RingDistalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> RingIntermediateAngles = new();
        public List<HPUIInteractorConeRayAngleSides> RingProximalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> LittleDistalAngles = new();
        public List<HPUIInteractorConeRayAngleSides> LittleIntermediateAngles = new();
        public List<HPUIInteractorConeRayAngleSides> LittleProximalAngles = new();

        private Dictionary<(XRHandJointID, FingerSide), List<HPUIInteractorRayAngle>> ActiveFingerAngles;

        public void OnEnable()
        {
            ActiveFingerAngles = new();

            Dictionary<XRHandJointID, List<HPUIInteractorConeRayAngleSides>> jointToAnglesMapping = new()
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

            foreach (KeyValuePair<XRHandJointID, List<HPUIInteractorConeRayAngleSides>> kvp in jointToAnglesMapping)
            {
                foreach (HPUIInteractorConeRayAngleSides angleSide in kvp.Value)
                {
                    if (!ActiveFingerAngles.ContainsKey((kvp.Key, angleSide.side)))
                    {
                        ActiveFingerAngles.Add((kvp.Key, angleSide.side), angleSide.rayAngles);
                    }
                }
            }
        }

        /// <summary>
        /// Get the corresponding list angles for a given joint and side.
        /// If the there is not angles for a given side for the joint, the angles of
        /// <see cref="FallbackSide"/> will be returned if it exists.
        /// Otherwise, this will return null.
        /// </summary>
        public IReadOnlyList<HPUIInteractorRayAngle> GetAngles(XRHandJointID joint, FingerSide side)
        {
            List<HPUIInteractorRayAngle> angles;
            if (ActiveFingerAngles.TryGetValue((joint, side), out angles) ||
                ActiveFingerAngles.TryGetValue((joint, fallbackSide), out angles))
            {
                return angles.AsReadOnly();
            }
            return null;
        }
    }

    /// <summary>
    /// Enum representing the sides of the finger
    /// </summary>
    public enum FingerSide
    {
        volar = 0,
        radial = 1
        // NOTE ulnar and dorsal are not included
    }

    [Serializable]
    public struct HPUIInteractorConeRayAngleSides
    {
        /// <summary>
        /// The side of the finger the <see cref="rayAngles"/> corresponds to.
        /// </summary>
        public FingerSide side;

        /// <summary>
        /// The list of ray angles
        /// </summary>
        public List<HPUIInteractorRayAngle> rayAngles;

        public HPUIInteractorConeRayAngleSides(FingerSide side, List<HPUIInteractorRayAngle> rayAngles) : this()
        {
            this.side = side;
            this.rayAngles = rayAngles;
        }
    }
}

