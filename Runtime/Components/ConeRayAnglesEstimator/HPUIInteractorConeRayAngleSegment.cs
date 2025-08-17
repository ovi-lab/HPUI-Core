using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Segments of the cone estimation. Corresponds to the fields of <see cref="HPUIInteractorConeRayAngles"/>
    /// </summary>
    public enum HPUIInteractorConeRayAngleSegment
    {
        IndexDistalVolarSegment = 0,
        IndexIntermediateVolarSegment = 1,
        IndexProximalVolarSegment = 2,
        MiddleDistalVolarSegment = 3,
        MiddleIntermediateVolarSegment = 4,
        MiddleProximalVolarSegment = 5,
        RingDistalVolarSegment = 6,
        RingIntermediateVolarSegment = 7,
        RingProximalVolarSegment = 8,
        LittleDistalVolarSegment = 9,
        LittleIntermediateVolarSegment = 10,
        LittleProximalVolarSegment = 11,

        IndexDistalRadialSegment = 12,
        IndexIntermediateRadialSegment = 13,
        IndexProximalRadialSegment = 14,
        MiddleDistalRadialSegment = 15,
        MiddleIntermediateRadialSegment = 16,
        MiddleProximalRadialSegment = 17,
        RingDistalRadialSegment = 18,
        RingIntermediateRadialSegment = 19,
        RingProximalRadialSegment = 20,
        LittleDistalRadialSegment = 21,
        LittleIntermediateRadialSegment = 22,
        LittleProximalRadialSegment = 23,
    }

    /// <summary>
    /// Provides conversion helpers between XR hand joint identifiers with a finger side
    /// and HPUI interactor cone ray angle segments.
    /// </summary>
    public static class HPUIInteractorConeRayAngleSegmentConversion
    {
        private static readonly Dictionary<(XRHandJointID, FingerSide), HPUIInteractorConeRayAngleSegment> mapping = new()
        {
            { (XRHandJointID.IndexDistal,        FingerSide.volar),  HPUIInteractorConeRayAngleSegment.IndexDistalVolarSegment },
            { (XRHandJointID.IndexIntermediate,  FingerSide.volar),  HPUIInteractorConeRayAngleSegment.IndexIntermediateVolarSegment },
            { (XRHandJointID.IndexProximal,      FingerSide.volar),  HPUIInteractorConeRayAngleSegment.IndexProximalVolarSegment },

            { (XRHandJointID.MiddleDistal,       FingerSide.volar),  HPUIInteractorConeRayAngleSegment.MiddleDistalVolarSegment },
            { (XRHandJointID.MiddleIntermediate, FingerSide.volar),  HPUIInteractorConeRayAngleSegment.MiddleIntermediateVolarSegment },
            { (XRHandJointID.MiddleProximal,     FingerSide.volar),  HPUIInteractorConeRayAngleSegment.MiddleProximalVolarSegment },

            { (XRHandJointID.RingDistal,         FingerSide.volar),  HPUIInteractorConeRayAngleSegment.RingDistalVolarSegment },
            { (XRHandJointID.RingIntermediate,   FingerSide.volar),  HPUIInteractorConeRayAngleSegment.RingIntermediateVolarSegment },
            { (XRHandJointID.RingProximal,       FingerSide.volar),  HPUIInteractorConeRayAngleSegment.RingProximalVolarSegment },

            { (XRHandJointID.LittleDistal,       FingerSide.volar),  HPUIInteractorConeRayAngleSegment.LittleDistalVolarSegment },
            { (XRHandJointID.LittleIntermediate, FingerSide.volar),  HPUIInteractorConeRayAngleSegment.LittleIntermediateVolarSegment },
            { (XRHandJointID.LittleProximal,     FingerSide.volar),  HPUIInteractorConeRayAngleSegment.LittleProximalVolarSegment },

            { (XRHandJointID.IndexDistal,        FingerSide.radial), HPUIInteractorConeRayAngleSegment.IndexDistalRadialSegment },
            { (XRHandJointID.IndexIntermediate,  FingerSide.radial), HPUIInteractorConeRayAngleSegment.IndexIntermediateRadialSegment },
            { (XRHandJointID.IndexProximal,      FingerSide.radial), HPUIInteractorConeRayAngleSegment.IndexProximalRadialSegment },

            { (XRHandJointID.MiddleDistal,       FingerSide.radial), HPUIInteractorConeRayAngleSegment.MiddleDistalRadialSegment },
            { (XRHandJointID.MiddleIntermediate, FingerSide.radial), HPUIInteractorConeRayAngleSegment.MiddleIntermediateRadialSegment },
            { (XRHandJointID.MiddleProximal,     FingerSide.radial), HPUIInteractorConeRayAngleSegment.MiddleProximalRadialSegment },

            { (XRHandJointID.RingDistal,         FingerSide.radial), HPUIInteractorConeRayAngleSegment.RingDistalRadialSegment },
            { (XRHandJointID.RingIntermediate,   FingerSide.radial), HPUIInteractorConeRayAngleSegment.RingIntermediateRadialSegment },
            { (XRHandJointID.RingProximal,       FingerSide.radial), HPUIInteractorConeRayAngleSegment.RingProximalRadialSegment },

            { (XRHandJointID.LittleDistal,       FingerSide.radial), HPUIInteractorConeRayAngleSegment.LittleDistalRadialSegment },
            { (XRHandJointID.LittleIntermediate, FingerSide.radial), HPUIInteractorConeRayAngleSegment.LittleIntermediateRadialSegment },
            { (XRHandJointID.LittleProximal,     FingerSide.radial), HPUIInteractorConeRayAngleSegment.LittleProximalRadialSegment },
        };

        private static readonly Dictionary<HPUIInteractorConeRayAngleSegment, (XRHandJointID, FingerSide)> reverseMapping;

        static HPUIInteractorConeRayAngleSegmentConversion()
        {
            reverseMapping = mapping.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        /// <summary>
        /// Gets the XRHandJointID associated with the specified HPUIInteractorConeRayAngleSegment.
        /// </summary>
        /// <param name="segment">The cone ray angle segment to convert.</param>
        /// <returns>The XRHandJointID that corresponds to <paramref name="segment"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided segment does not have a corresponding joint mapping.
        /// </exception>
        public static XRHandJointID ToJointID(HPUIInteractorConeRayAngleSegment segment)
        {
            if (reverseMapping.TryGetValue(segment, out (XRHandJointID, FingerSide) val))
            {
                return val.Item1;
            }
            throw new ArgumentException($"Unexpected cone ray angle segment: {segment}");
        }

        /// <summary>
        /// Gets the FingerSide associated with the specified HPUIInteractorConeRayAngleSegment.
        /// </summary>
        /// <param name="segment">The cone ray angle segment to convert.</param>
        /// <returns>The FingerSide that corresponds to <paramref name="segment"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided segment does not have a corresponding finger side mapping.
        /// </exception>
        public static FingerSide ToFingerSide(HPUIInteractorConeRayAngleSegment segment)
        {
            if (reverseMapping.TryGetValue(segment, out (XRHandJointID, FingerSide) val))
            {
                return val.Item2;
            }
            throw new ArgumentException($"Unexpected cone ray angle segment: {segment}");
        }

        /// <summary>
        /// Gets the (XRHandJointID, FingerSide) pair associated with the specified
        /// HPUIInteractorConeRayAngleSegment.
        /// </summary>
        /// <param name="segment">The cone ray angle segment to convert.</param>
        /// <returns>
        /// A tuple containing the XRHandJointID and FingerSide that correspond to
        /// <paramref name="segment"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided segment does not have a corresponding mapping.
        /// </exception>
        public static (XRHandJointID, FingerSide) ToJointIDAndFingerSide(HPUIInteractorConeRayAngleSegment segment)
        {
            if (reverseMapping.TryGetValue(segment, out (XRHandJointID, FingerSide) val))
            {
                return val;
            }
            throw new ArgumentException($"Unexpected cone ray angle segment: {segment}");
        }

        /// <summary>
        /// Gets the HPUIInteractorConeRayAngleSegment associated with the specified
        /// XRHandJointID and FingerSide.
        /// </summary>
        /// <param name="jointID">The joint identifier.</param>
        /// <param name="side">The side of the finger (e.g., volar or radial).</param>
        /// <returns>The corresponding HPUIInteractorConeRayAngleSegment.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided (jointID, side) pair does not have a corresponding segment mapping.
        /// </exception>
        public static HPUIInteractorConeRayAngleSegment ToConeRayAngleSegment(XRHandJointID jointID, FingerSide side)
        {
            if (mapping.TryGetValue((jointID, side), out HPUIInteractorConeRayAngleSegment val))
            {
                return val;
            }
            throw new ArgumentException($"Unexpected joint ID, finger side pair segment: {(jointID, side)}");
        }
    }
}
