using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Hands;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Components
{
    /// <summary>
    /// Convinent wrapper component for <see cref="ConeRayAnglesEstimator"/>.
    /// See <see cref="ConeRayAnglesEstimator"/> for more details.
    /// </summary>
    public class EstimateConeRayAngles: MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Corresponding Interactor")]
        private HPUIInteractor interactor;

        /// <summary>
        /// The interactor used for the esimation.
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

        [SerializeField]
        [Tooltip("Interactable segment pairs.")]
        private List<ConeRayAnglesEstimationPair> interactableToSegmentMapping = new();

        /// <summary>
        /// List of interactable to segment mappaing pairs. The list is expected to have all segments.
        /// </summary>
        public List<ConeRayAnglesEstimationPair> InteractableToSegmentMapping { get => interactableToSegmentMapping; set => interactableToSegmentMapping = value; }

        private ConeRayAnglesEstimator estimator;
        private HPUIFullRangeRayCastDetectionLogic fullrangeRaycastDetectionLogicReference;

        /// <summary>
        /// Intiate data collection. If this component was used to generate an asset, and
        /// the detection logic is not a <see cref="HPUIFullRangeRayCastDetectionLogic"/>, the
        /// HPUIInteractorFullRangeAngles before the asset was generated will be set as the
        /// detection logic of the interactor.
        /// </summary>
        public void StartDataCollection()
        {
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
            estimator = new ConeRayAnglesEstimator(Interactor, InteractableToSegmentMapping);
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
                Interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(Interactor.DetectionLogic.InteractionHoverRadius, angles, Interactor.GetComponent<XRHandTrackingEvents>());
                callback.Invoke(angles);
            });
        }
    }
}
 
