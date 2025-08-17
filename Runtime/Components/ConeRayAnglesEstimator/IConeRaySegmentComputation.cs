using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.XR.Hands;

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
    /// Holds all the data collected for a single gesture event.
    /// </summary>
    public struct ConeRayComputationDataRecord
    {
        public List<RaycastDataRecordsContainer> records;
        public HPUIInteractorConeRayAngleSegment segment;

        public ConeRayComputationDataRecord(List<RaycastDataRecordsContainer> records, HPUIInteractorConeRayAngleSegment segment) : this()
        {
            this.records = records;
            this.segment = segment;
        }
    }

    /// <summary>
    /// Container for a list of <see cref="HPUIRayCastDetectionBaseLogic.RaycastDataRecord"/> emited with
    /// <see cref="HPUIRayCastDetectionBaseLogic.raycastData"/>. Also contains the closest
    /// <see cref="FingerSide">side</see> and <see cref="XRHandJoint">joint</see>
    /// </summary>
    public struct RaycastDataRecordsContainer
    {
        public List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecordsList;
        public FingerSide fingerSide;
        public XRHandJointID handJointID;

        public RaycastDataRecordsContainer(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecord, FingerSide fingerSide, XRHandJointID handJointID) : this()
        {
            this.raycastDataRecordsList = raycastDataRecord;
            this.fingerSide = fingerSide;
            this.handJointID = handJointID;
        }
    }
}
