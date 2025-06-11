using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI
{
    public interface IConeRaySegmentComputation
    {
        /// <summary>
        /// For a given segment, computes the List of <see cref="HPUIInteractorRayAngle">.
        /// </summary>
        /// <param name="segment">
        ///   The <see cref="HPUIInteractorConeRayAngleSegment"/> for which cone
        ///   angles are being computed.
        /// </param>
        List<HPUIInteractorRayAngle> EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment, IEnumerable<ConeRayComputationDataRecord> interactionRecords);
    }

    /// <summary>
    /// Segments of the cone estimation. Corresponds to the fields of <see cref="HPUIInteractorConeRayAngles"/>
    /// </summary>
    public enum HPUIInteractorConeRayAngleSegment
    {
        IndexDistalSegment = 0,
        IndexIntermediateSegment = 1,
        IndexProximalSegment = 2,
        MiddleDistalSegment = 3,
        MiddleIntermediateSegment = 4,
        MiddleProximalSegment = 5,
        RingDistalSegment = 6,
        RingIntermediateSegment = 7,
        RingProximalSegment = 8,
        LittleDistalSegment = 9,
        LittleIntermediateSegment = 10,
        LittleProximalSegment = 11,
    }

    /// <summary>
    /// Holds all the data collected for a single gesture event.
    /// </summary>
    public struct ConeRayComputationDataRecord
    {
        public List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records;
        public HPUIInteractorConeRayAngleSegment segment;

        public ConeRayComputationDataRecord(List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records, HPUIInteractorConeRayAngleSegment segment) : this()
        {
            this.records = records;
            this.segment = segment;
        }
    }
}
