using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactor.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIInteractor: XRBaseInteractor, IHPUIInteractor
    {
        // TODO move these to an asset?
        [SerializeField]
        private float tapTimeThreshold;
        public float TapTimeThreshold { get => tapTimeThreshold;  set => tapTimeThreshold = value; }

        [SerializeField]
        private float tapDistanceThreshold;
        public float TapDistanceThreshold { get => tapDistanceThreshold; set => tapDistanceThreshold = value; }

        [SerializeField]
        [Tooltip("Event triggered on tap")]
        private HPUITapEvent tapEvent = new HPUITapEvent();

        /// <inheritdoc />
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        [Tooltip("Event triggered on gesture")]
        private HPUIGestureEvent gestureEvent = new HPUIGestureEvent();

        /// <inheritdoc />
        public HPUIGestureEvent GestureEvent { get => gestureEvent; set => gestureEvent = value; }

        [SerializeField]
        [Tooltip("Interation hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interation hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        protected IHPUIGestureLogic gestureLogic;
        private List<IXRInteractable> validTargets = new List<IXRInteractable>();
        private bool justStarted = false;
        private Vector3 lastInteractionPoint;
        private PhysicsScene physicsScene;
        private RaycastHit[] sphereCastHits = new RaycastHit[25];
        private Collider[] overlapSphereHits = new Collider[25];

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
            physicsScene = gameObject.scene.GetPhysicsScene();
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold);
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        protected void OnValidate()
        {
            // When values are changed in inspector, update the values
            if (gestureLogic != null)
            {
                gestureLogic.Dispose();
            }
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold);
        }
#endif

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            justStarted = true;
        }

        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);
            gestureLogic.OnSelectEntering(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            gestureLogic.OnSelectExiting(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            // Following the logic in XRPokeInteractor
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                validTargets.Clear();

                // Hover Check
                Vector3 pokeInteractionPoint = GetAttachTransform(null).position;
                Vector3 overlapStart = lastInteractionPoint;
                Vector3 interFrameEnd = pokeInteractionPoint; // FIXME: Think of getting this of collision points?

                BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out Vector3 normalizedOverlapVector, out float overlapSqrMagnitude, out float overlapDistance);

                // If no movement is recorded.
                // Check if spherecast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
                int numberOfOverlaps;

                if (justStarted || overlapSqrMagnitude < 0.001f)
                {
                    numberOfOverlaps = physicsScene.OverlapSphere(
                        interFrameEnd,
                        InteractionHoverRadius,
                        overlapSphereHits,
                        // FIXME: physics layers should be allowed to be set in inpsector
                        Physics.AllLayers,
                        // FIXME: QueryTriggerInteraction should be allowed to be set in inpsector
                        QueryTriggerInteraction.Ignore);
                }
                else
                {
                    numberOfOverlaps = physicsScene.SphereCast(
                        overlapStart,
                        InteractionHoverRadius,
                        normalizedOverlapVector,
                        sphereCastHits,
                        overlapDistance,
                        // FIXME: physics layers should be allowed to be set in inpsector
                        Physics.AllLayers,
                        // FIXME: QueryTriggerInteraction should be allowed to be set in inpsector
                        QueryTriggerInteraction.Ignore);

                }

                lastInteractionPoint = pokeInteractionPoint;
                justStarted = false;

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    if (interactionManager.TryGetInteractableForCollider(sphereCastHits[i].collider, out var interactable) &&
                        interactable is IXRSelectInteractable selectable &&
                        interactable is IXRHoverInteractable hoverable && hoverable.IsHoverableBy(this))
                    {
                        validTargets.Add(interactable);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                gestureLogic.Update();
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            base.GetValidTargets(targets);

            targets.Clear();
            foreach(IXRInteractable target in validTargets.Select(t => t as IHPUIInteractable).Where(ht => ht != null).OrderBy(ht => ht.zOrder))
            {
                targets.Add(target);
                validTargets.Add(target);
            }
        }

        // NOTE: PokeInteractor has a bug where it doesn't account for the re-prioritization.
        // See: https://forum.unity.com/threads/xrpokeinteractor-m_currentpoketarget-not-respecting-getvalidtargets-and-target-filters.1534039/#post-9571063
        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return validTargets.Contains(interactable) && ProcessSelectFilters(interactable);
        }


        #region IHPUIInteractor interface
        /// <inheritdoc />
        public void OnTap(HPUITapEventArgs args)
        {
            tapEvent?.Invoke(args);
        }

        /// <inheritdoc />
        public void OnGesture(HPUIGestureEventArgs args)
        {
            gestureEvent?.Invoke(args);
        }
        #endregion
    }
}
 
