using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// </summary>
    [Serializable]
    public class HPUISphereCastDetectionLogic : IHPUIDetectionLogic
    {
        [SerializeField]
        [Tooltip("Interaction hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interaction hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        [SerializeField]
        [Tooltip("Physics layer mask used for limiting poke sphere overlap.")]
        private LayerMask physicsLayer = Physics.AllLayers;

        /// <summary>
        /// Physics layer mask used for limiting poke sphere overlap.
        /// </summary>
        public LayerMask PhysicsLayer { get => physicsLayer; set => physicsLayer = value; }

        [SerializeField]
        [Tooltip("Determines whether triggers should be collided with.")]
        private QueryTriggerInteraction physicsTriggerInteraction = QueryTriggerInteraction.Ignore;

        /// <summary>
        /// Determines whether triggers should be collided with.
        /// </summary>
        public QueryTriggerInteraction PhysicsTriggerInteraction { get => physicsTriggerInteraction; set => physicsTriggerInteraction = value; }
        [SerializeField]
        [Tooltip("Interaction select radius.")]
        private float interactionSelectionRadius = 0.015f;

        /// <summary>
        /// Interaction selection radius.
        /// </summary>
        public float InteractionSelectionRadius
        {
            get => interactionSelectionRadius;
            set
            {
                interactionSelectionRadius = value;
            }
        }
        
        private PhysicsScene physicsScene;
        private Collider[] overlapSphereHits = new Collider[200];

        public HPUISphereCastDetectionLogic()
        {}

        public HPUISphereCastDetectionLogic(float interactionHoverRadius, float interactionSelectionRadius)
        {
            this.interactionHoverRadius = interactionHoverRadius;
            this.interactionSelectionRadius = interactionSelectionRadius;
        }

        /// <inheritdoc />
        public void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            validTargets.Clear();

            Transform attachTransform = interactor.GetAttachTransform(null);
            Vector3 interactionPoint = attachTransform.position;
            hoverEndPoint = interactionPoint;

            if (physicsScene == null)
            {
                physicsScene = interactor.transform.gameObject.scene.GetPhysicsScene();
            }


            int numberOfOverlaps = physicsScene.OverlapSphere(
                interactionPoint,
                InteractionHoverRadius,
                overlapSphereHits,
                physicsLayer,
                physicsTriggerInteraction);

            float shortestInteractableDist = float.MaxValue;

            for (var i = 0; i < numberOfOverlaps; ++i)
            {
                Collider collider = overlapSphereHits[i];
                if (interactionManager.TryGetInteractableForCollider(collider, out var interactable) &&
                    interactable is IHPUIInteractable hpuiInteractable &&
                    hpuiInteractable.IsHoverableBy(interactor))
                {
                    XRInteractableUtility.TryGetClosestPointOnCollider(interactable, interactionPoint, out DistanceInfo info);
                    float dist = Mathf.Sqrt(info.distanceSqr);
                    validTargets.Add(hpuiInteractable, new HPUIInteractionInfo(dist, dist < InteractionSelectionRadius, info.point, info.collider, dist, null));
                    if (dist < shortestInteractableDist)
                    {
                        hoverEndPoint = info.point;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {}
    }
}
