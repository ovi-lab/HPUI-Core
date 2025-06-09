using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Hands;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.Assertions;

namespace ubco.ovilab.HPUI.Components
{
    public class ConeRayAnglesCalibrationRoutine : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Corresponding Interactor")]
        private HPUIInteractor interactor;

        /// <summary>
        /// The interactor used for the estimation.
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

        [SerializeField]
        [Tooltip("Phalange that the interactor is currently being calibrated for")]
        private HPUIInteractorConeRayAngleSegment targetSegment;

        /// <summary>
        /// Phalange that the interactor is currently being calibrated for
        /// </summary>
        public HPUIInteractorConeRayAngleSegment TargetSegment { get => targetSegment; set => targetSegment = value; }

        [SerializeField]
        [Tooltip("If true, will set the detection logic of interactor to HPUIConeRayCastDetectionLogic with generated asset.")]
        private bool setDetectionLogicOnEstimation = false;

        /// <summary>
        /// If true, will set the detection logic of interactor to HPUIConeRayCastDetectionLogic with generated asset.
        /// </summary>
        public bool SetDetectionLogicOnEstimation { get => setDetectionLogicOnEstimation; set => setDetectionLogicOnEstimation = value; }

        [SerializeField]
        [Tooltip("(optional) The hand tracking event to use with HPUIConeRayCastDetectionLogic if SetDetectionLogicOnEstimation is true." +
                 "If this is not set, then will look for XRHandTrackingEvents in the Interactor.")]
        private XRHandTrackingEvents xrHandTrackingEventsForConeDetection;

        /// <summary>
        /// The hand tracking event to use with HPUIConeRayCastDetectionLogic if SetDetectionLogicOnEstimation is true.
        /// If this is not set, then will look for XRHandTrackingEvents in the Interactor.
        /// </summary>
        public XRHandTrackingEvents XRHandTrackingEventsForConeDetection { get => xrHandTrackingEventsForConeDetection; set => xrHandTrackingEventsForConeDetection = value; }

        private ConeRayAnglesCalibrator calibrator;
        private HPUIFullRangeRayCastDetectionLogic fullrangeRaycastDetectionLogicReference;
        private bool initComplete = false;

        private void BeginDataCollectionProcedure()
        {
            if (!Application.isPlaying) return;
            if (SetDetectionLogicOnEstimation)
            {
                Assert.IsTrue(XRHandTrackingEventsForConeDetection != null || Interactor.GetComponent<XRHandTrackingEvents>() != null,
                              "XRHandTrackingEventsForConeDetection is null and Interactor doesn't have an XRHandTrackingEvents component.");
            }

            if (!(Interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic))
            {
                if (fullrangeRaycastDetectionLogicReference == null)
                {
                    throw new ArgumentException("Expected interactor to be configured with HPUIInteractorFullRangeAngles.");
                }

                Interactor.DetectionLogic = fullrangeRaycastDetectionLogicReference as IHPUIDetectionLogic;
            }
            else
            {
                fullrangeRaycastDetectionLogicReference = Interactor.DetectionLogic as HPUIFullRangeRayCastDetectionLogic;
            }

            calibrator = new ConeRayAnglesCalibrator(interactor);
            initComplete = true;
        }

        public void StartDataCollectionForPhalange()
        {
            if (!initComplete) BeginDataCollectionProcedure();
            calibrator.IsCalibrationActive = true;
        }

        public void EndDataCollectionForPhalange()
        {
            calibrator.EndCalibrationForSegment(TargetSegment);
            calibrator.IsCalibrationActive = false;
        }

        public void FinishAndEstimate(Action<HPUIInteractorConeRayAngles> callback)
        {
            Assert.IsTrue(!calibrator.IsCalibrationActive, "Calibration is currently active, end that first!");
            calibrator.EstimateConeRayAngles((angles) =>
            {
                callback.Invoke(angles);
                if (SetDetectionLogicOnEstimation)
                {
                    Interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(
                        Interactor.DetectionLogic.InteractionHoverRadius,
                        angles,
                        XRHandTrackingEventsForConeDetection != null ? XRHandTrackingEventsForConeDetection : Interactor.GetComponent<XRHandTrackingEvents>());
                }
            });
        }
    }
}
