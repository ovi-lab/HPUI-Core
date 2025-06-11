using UnityEngine;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Data is collected by specifying which segment all data should be assigned to.
    /// </summary>
    public class GuidedDataCollector : ConeRayDataCollectorBase
    {
        [SerializeField]
        [Tooltip("Phalange that the interactor is currently being calibrated for")]
        private HPUIInteractorConeRayAngleSegment targetSegment;

        /// <summary>
        /// Phalange that the interactor is currently being calibrated for
        /// </summary>
        public HPUIInteractorConeRayAngleSegment TargetSegment { get => targetSegment; set => targetSegment = value; }

        /// <summary>
        /// This will create a <see cref="ConeRayComputationDataRecord"/> for the
        /// segment passed as a parameter.
        /// </summary>
        public void EndCalibrationForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            DataRecords.Add(new ConeRayComputationDataRecord(currentInteractionData, segment));

            currentInteractionData = new();
        }

        /// <summary>
        /// This will create a <see cref="ConeRayComputationDataRecord"/> for the
        /// current <see cref="TargetSegment"/>.
        /// </summary>
        public void EndDataCollectionForTargetSegment()
        {
            EndCalibrationForSegment(TargetSegment);
        }
    }
}
