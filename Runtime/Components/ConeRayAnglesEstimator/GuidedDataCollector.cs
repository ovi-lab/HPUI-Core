using UnityEngine;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Data is collected by specifying which segment all data should be assigned to.
    /// </summary>
    public class GuidedDataCollector : RaycastDataCollectorBase
    {
        [SerializeField]
        [Tooltip("Phalange that the interactor is currently being calibrated for")]
        private HPUIInteractorConeRayAngleSegment targetSegment;

        /// <summary>
        /// Phalange that the interactor is currently being calibrated for
        /// </summary>
        public HPUIInteractorConeRayAngleSegment TargetSegment { get => targetSegment; set => targetSegment = value; }

        [SerializeField]
        [Tooltip("Ensures that only one calibration data record is collected for each phalange. Disabling this will allow averaging over multiple calibrations per phalange")]
        private bool uniqueDataRecordPerPhalange = true;

        /// <summary>
        /// Ensures that only one calibration data record is collected for each phalange. Disabling this will allow averaging over multiple calibrations per phalange
        /// </summary>
        public bool UniqueDataRecordPerPhalange { get => uniqueDataRecordPerPhalange; set => uniqueDataRecordPerPhalange = value; }

        /// <summary>
        /// This will create a <see cref="ConeRayComputationDataRecord"/> for the
        /// segment passed as a parameter.
        /// </summary>
        public void EndCalibrationForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            if (uniqueDataRecordPerPhalange)
            {
                foreach (var dataRecord in DataRecords)
                {
                    if (dataRecord.segment == segment)
                    {
                        DataRecords.Remove(dataRecord);
                        break;
                    }
                }
            }

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
