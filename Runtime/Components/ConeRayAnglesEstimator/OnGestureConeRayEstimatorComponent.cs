using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Hands;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.Assertions;

namespace ubco.ovilab.HPUI.Components
{

    /// <summary>
    /// Convinent wrapper component for <see cref="OnGestureConeRayEstimator"/>.
    /// See <see cref="OnGestureConeRayEstimator"/> for more details.
    /// </summary>
    public class OnGestureConeRayEstimatorComponent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Corresponding Interactor")]
        private HPUIInteractor interactor;

        /// <summary>
        /// The interactor used for the esimation.
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

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

        [SerializeField]
        [Tooltip("Interactable segment pairs.")]
        private List<ConeRayAnglesEstimationPair> interactableToSegmentMapping = new();

        /// <summary>
        /// List of interactable to segment mappaing pairs. The list is expected to have all segments.
        /// </summary>
        public List<ConeRayAnglesEstimationPair> InteractableToSegmentMapping { get => interactableToSegmentMapping; set => interactableToSegmentMapping = value; }

        private OnGestureConeRayEstimator estimator;
        private HPUIFullRangeRayCastDetectionLogic fullrangeRaycastDetectionLogicReference;

        /// <summary>
        /// Intiate data collection. If this component was used to generate an asset, and
        /// the detection logic is not a <see cref="HPUIFullRangeRayCastDetectionLogic"/>, the
        /// HPUIInteractorFullRangeAngles before the asset was generated will be set as the
        /// detection logic of the interactor.
        /// </summary>
        public void StartDataCollection()
        {
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
            estimator = new OnGestureConeRayEstimator(Interactor, InteractableToSegmentMapping);
        }

        /// <summary>
        /// Finish data collection and start estimation. When estimation is completed, the
        /// callback is invoked with the generated asset. The generated asset will also be
        /// set as the default logic of the interactor.
        /// </summary>
        public void FinishAndEstimate(Action<HPUIInteractorConeRayAngles> callback)
        {
            estimator.EstimateConeRayAngles((angles) =>
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
