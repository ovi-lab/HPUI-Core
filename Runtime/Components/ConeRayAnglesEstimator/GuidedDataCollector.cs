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
        public HPUIInteractorConeRayAngleSegment TargetSegment { get => targetSegment; set => targetSegment = value; }

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
        public List<HPUIInteractorConeRayAngleSegment> OrderOfCalibration { get => orderOfCalibration; }

        [SerializeField]
        [Tooltip("Automatically move to next segment. If order of calibration is populated, then this will move to the next item in the order, and cycle back upon finishing. Else, it will move through the full phalanges list")]
        private bool autoMoveToNextPhalange = true;

        /// <summary>
        /// Automatically move to next segment. If order 
        /// of calibration is populated, then this will 
        /// move to the next item in the order, and cycle 
        /// back upon finishing. Else, it will move through 
        /// the full phalanges list 
        /// </summary>
        /// <param name="parameterName">Parameter description.</param>
        /// <returns>Type and description of the returned object.</returns>
        /// <example>Write me later.</example>
        public bool AutoMoveToNextPhalange => autoMoveToNextPhalange;

        private int currentPhalangeIndex;

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

        /// <summary>
        /// Set's the target segment to be calibrated.
        /// See <see cref="GuidedDataCollectorEditor.StepThroughOrderOfCalibrationPhalanges"/> 
        /// for an example on using this during calibration procedures.
        /// </summary>
        /// <param name="parameterName">Parameter description.</param>
        /// <returns>Type and description of the returned object.</returns>
        /// <example>Write me later.</example>
        public void StepToTargetPhalange(HPUIInteractorConeRayAngleSegment targetSegment)
        {
            this.targetSegment = targetSegment;
        }
    }
}
