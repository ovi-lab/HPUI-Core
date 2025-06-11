using UnityEngine;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// TODO
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
        /// The callback used with the interactable gesture event to track the events.
        /// </summary>
        public void EndCalibrationForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            DataRecords.Add(new ConeRayComputationDataRecord(currentInteractionData, segment));

            currentInteractionData = new();
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void EndDataCollectionForTargetSegment()
        {
            EndCalibrationForSegment(TargetSegment);
        }
    }
}
