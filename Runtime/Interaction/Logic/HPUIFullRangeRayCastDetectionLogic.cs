using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// TODO: docs
    /// </summary>
    [Serializable]
    public class HPUIFullRangeRayCastDetectionLogic: HPUIRayCastDetectionBaseLogic
    {
        [SerializeField]
        [Tooltip("The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique")]
        private HPUIInteractorFullRangeAngles fullRangeRayAngles;

        /// <summary>
        /// The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique
        /// </summary>
        public HPUIInteractorFullRangeAngles FullRangeRayAngles { get => fullRangeRayAngles; set => fullRangeRayAngles = value; }

        public HPUIFullRangeRayCastDetectionLogic()
        {}

        public HPUIFullRangeRayCastDetectionLogic(float hoverRadius, HPUIInteractorFullRangeAngles fullRangeAngles)
        {
            this.InteractionHoverRadius = hoverRadius;
            this.fullRangeRayAngles = fullRangeAngles;
        }

        /// <inheritdoc />
        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            Process(interactor, interactionManager, FullRangeRayAngles.angles, validTargets, out hoverEndPoint);
        }

        /// <inheritdoc />
        public void Reset()
        {}
    }
}
