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
        [Tooltip("The HPUIInteractorPill asset to use for pill technique")]
        private HPUIInteractorPill pill;

        /// <summary>
        /// The <see cref="HPUIInteractorPill"/> asset to use for pill technique
        /// </summary>
        public HPUIInteractorPill Pill { get => pill; set => pill = value; }

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
            if (Pill == null)
            {
                Debug.LogError($"The `Pill` asset is not set!");
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            Process(interactor, interactionManager, Pill.angles, validTargets, out hoverEndPoint);
        }
    }
}
