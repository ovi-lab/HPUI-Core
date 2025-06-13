using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <inheritdoc/>
    [Serializable]
    public class HPUIPillDetectionLogic : HPUIRayCastDetectionBaseLogic
    {
        [SerializeField]
        [Tooltip("The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique")]
        private HPUIInteractorPill pill;

        /// <summary>
        /// The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique
        /// </summary>
        public HPUIInteractorPill FullRangeRayAngles { get => pill; set => pill = value; }

        public HPUIPillDetectionLogic()
        { }

        public HPUIPillDetectionLogic(float hoverRadius, HPUIInteractorPill pill)
        {
            this.InteractionHoverRadius = hoverRadius;
            this.pill = pill;
        }

        /// <inheritdoc />
        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            if (FullRangeRayAngles == null)
            {
                Debug.LogError($"The `FullRangeRayAngles` asset is not set!");
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            Process(interactor, interactionManager, FullRangeRayAngles.angles, validTargets, out hoverEndPoint);
        }
    }
}
