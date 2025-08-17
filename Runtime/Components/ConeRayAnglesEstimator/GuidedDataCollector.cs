using System;
using System.Collections.Generic;
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
        public HPUIInteractorConeRayAngleSegment TargetSegment
        {
            get => targetSegment;
            set
            {
                if (orderOfCalibration != null && orderOfCalibration.Count > 0)
                {
                    if (orderOfCalibration.Contains(value))
                    {
                        targetSegment = value;
                    }
                    else
                    {
                        Debug.LogError($"Attempted to set TargetSegment to a value not in OrderOfCalibration: {value}");
                    }
                }
                else
                {
                    targetSegment = value;
                }
            }
        }

        [SerializeField]
        [Tooltip("Ensures that only one calibration data record is collected for each phalange. Disabling this will allow averaging over multiple calibrations per phalange")]
        private bool uniqueDataRecordPerPhalange = true;

        /// <summary>
        /// Ensures that only one calibration data record is collected for each phalange. Disabling this will allow averaging over multiple calibrations per phalange
        /// </summary>
        public bool UniqueDataRecordPerPhalange { get => uniqueDataRecordPerPhalange; set => uniqueDataRecordPerPhalange = value; }

        [SerializeField]
        [Tooltip("Defines a custom order of calibration. If auto-move to next phalange is enabled, the target segment will automatically move to the next segment when you pause data collection for the current segment.")]
        private List<HPUIInteractorConeRayAngleSegment> orderOfCalibration;

        /// <summary>
        /// Defines a custom order of calibration. If auto-move
        /// to next phalange is enabled, the target segment will
        /// automatically move to the next segment when you pause
        /// data collection for the current segment.
        /// </summary>
        public List<HPUIInteractorConeRayAngleSegment> OrderOfCalibration => orderOfCalibration;

        private int currentPhalangeIndex;

        /// <summary>
        /// This will create a <see cref="ConeRayComputationDataRecord"/> for the
        /// segment passed as a parameter.
        /// </summary>
        public void EndCalibrationForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            if (uniqueDataRecordPerPhalange)
            {
                foreach (ConeRayComputationDataRecord dataRecord in DataRecords)
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
        /// current <see cref="TargetSegment"/>. Also pauses the data collector
        /// to adjust target segment for the next data record.
        /// </summary>
        public void EndDataCollectionForTargetSegment()
        {
            EndCalibrationForSegment(TargetSegment);
            PauseDataCollection = true;
        }

        /// <summary>
        /// Resumes data collection. To be used after
        /// `EndDataCollectionForTargetSegment`
        /// </summary>
        public void StartDataCollectionForNextTargetSegment()
        {
            PauseDataCollection = false;
        }

        public void StepThroughCustomPhalanges(int amt = 1)
        {
            currentPhalangeIndex = (currentPhalangeIndex + amt) % OrderOfCalibration.Count;
            if (currentPhalangeIndex < 0)
            {
                currentPhalangeIndex += OrderOfCalibration.Count;
            }
            HPUIInteractorConeRayAngleSegment currentTargetSegment = OrderOfCalibration[currentPhalangeIndex];
            TargetSegment = currentTargetSegment;
        }

        public void StepThroughAllPhalanges(int amt = 1)
        {
            int phalangeCount = Enum.GetNames(typeof(HPUIInteractorConeRayAngleSegment)).Length;
            int targetSegmentIndex = Array.IndexOf(Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)), TargetSegment);
            if (amt > 0)
            {
                if (targetSegmentIndex < phalangeCount - 1)
                {
                    TargetSegment = (HPUIInteractorConeRayAngleSegment)targetSegmentIndex + amt;
                }
                else
                {
                    TargetSegment = 0;
                }
            }
            else
            {
                if (targetSegmentIndex == 0)
                {
                    TargetSegment = (HPUIInteractorConeRayAngleSegment)phalangeCount - 1;
                }
                else
                {
                    TargetSegment = (HPUIInteractorConeRayAngleSegment)targetSegmentIndex + amt;
                }
            }
        }
    }
}
