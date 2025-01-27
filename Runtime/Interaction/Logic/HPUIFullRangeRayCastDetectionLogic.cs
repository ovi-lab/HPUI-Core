using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Detects which interactable is being selected with raycasts based on the the <see cref="FullRangeRayAngles"/>.
    /// The heuristic assigned to the interactable is based on the number of rays that makes contact with the interactable
    /// and the distances to it..
    /// </summary>
    [Serializable]
    public class HPUIFullRangeRayCastDetectionLogic: HPUIRayCastDetectionBaseLogic
    {
        [SerializeField]
        [Tooltip("The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique")]
        private HPUIInteractorFullRangeAngles fullRangeRayAngles;
        private List<Vector3> rightProcessedAngles = new();
        private List<Vector3> leftProcessedAngles = new();
        private bool isProcessedAnglesPopulated = false;
        /// <summary>
        /// The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique
        /// </summary>
        public HPUIInteractorFullRangeAngles FullRangeRayAngles { get => fullRangeRayAngles; set => fullRangeRayAngles = value; }

        public HPUIFullRangeRayCastDetectionLogic()
        {}

        public HPUIFullRangeRayCastDetectionLogic(float hoverRadius, HPUIInteractorFullRangeAngles fullRangeAngles)
        {
            rightProcessedAngles = new();
            this.InteractionHoverRadius = hoverRadius;
            this.fullRangeRayAngles = fullRangeAngles;
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

            if (!isProcessedAnglesPopulated)
            {
                foreach (HPUIInteractorRayAngle angleData in FullRangeRayAngles.angles)
                {
                    rightProcessedAngles.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, false));
                    leftProcessedAngles.Add(HPUIInteractorRayAngle.GetDirection(angleData.X, angleData.Z, true));
                }
                isProcessedAnglesPopulated = true;
            }
            
            List<Vector3> processedAngles = interactor.handedness == InteractorHandedness.Right ? rightProcessedAngles : leftProcessedAngles;
            Process(interactor, interactionManager, FullRangeRayAngles.angles, validTargets, out hoverEndPoint, processedAngles);
        }
    }
}
