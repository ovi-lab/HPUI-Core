using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{
    public class EstimateConeRayAngles: MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Corresponding Interactor")]
        private HPUIInteractor interactor;

        [SerializeField]
        [Tooltip("Interactable segment pair")]
        private List<ConeRayAnglesEstimationPair> interactableToSegmentMapping;

        private ConeRayAnglesEstimator estimator;

        public void StartEstimation()
        {
            estimator = new ConeRayAnglesEstimator(interactor, interactableToSegmentMapping);
        }

        public void FinishEstimation(Action<HPUIInteractorConeRayAngles> callback)
        {
            estimator.EstimateConeRayAngles((angles) =>
            {
                interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(interactor.DetectionLogic.InteractionHoverRadius, angles, interactor.GetComponent<XRHandTrackingEvents>());
                callback.Invoke(angles);
            });
        }
    }
}
 
