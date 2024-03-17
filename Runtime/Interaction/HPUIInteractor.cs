using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactor. Selects/hovers only the closest interactable for a given zOrder.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIInteractor: XRBaseInteractor, IHPUIInteractor
    {
        public enum HPUIInteractorVisuals
        {
            /// <summary>
            /// No visuls are shown
            /// </summary>
            None,
            /// <summary>
            /// Show the individual raycasts for the hover detection.
            /// </summary>
            Ray,
            /// <summary>
            /// Show the sphere for the hover.
            /// </summary>
            HoverSphere,
            /// <summary>
            /// Show the sphere for the selection.
            /// </summary>
            SelectSphere
        }

        // TODO move these to an asset?
        [Tooltip("The time threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapTimeThreshold;
        /// <summary>
        /// The time threshold at which an interaction would be treated as a gesture.
        /// That is, if the interactor is in contact with an
        /// interactable for more than this threshold, it would be
        /// treated as as gesture.
        /// </summary>
        public float TapTimeThreshold { get => tapTimeThreshold;  set => tapTimeThreshold = value; }

        [Tooltip("The distance threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapDistanceThreshold;
        /// <summary>
        /// The distance threshold at which an interaction would be treated as a gesture.
        /// That is, if the interactor has moved more than this value
        /// after coming into contact with a interactable, it would be
        /// treated as as gesture.
        /// </summary>
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

        [SerializeField]
        [Tooltip("Interation select radius.")]
        private float interactionSelectionRadius = 0.015f;

        /// <summary>
        /// Interation selection radius.
        /// </summary>
        public float InteractionSelectionRadius { get => interactionSelectionRadius; set => interactionSelectionRadius = value; }

        [SerializeField]
        [Tooltip("If true, select only happens for the target with highest priority.")]
        private bool selectOnlyPriorityTarget = true;

        /// <summary>
        /// If true, select only happens for the target with highest priority.
        /// </summary>
        public bool SelectOnlyPriorityTarget { get => selectOnlyPriorityTarget; set => selectOnlyPriorityTarget = value; }

        [SerializeField]
        [Tooltip("Set which visuals to use.")]
        private HPUIInteractorVisuals visuals = HPUIInteractorVisuals.None;

        /// <summary>
        /// The visuals that are shown with the interactor.
        /// </summary>
        public HPUIInteractorVisuals Visuals { get => visuals; set {
                UpdateVisuals();
                visuals = value;
            }
        }

        protected IHPUIGestureLogic gestureLogic;
        private Dictionary<IHPUIInteractable, CollisionInfo> validTargets = new Dictionary<IHPUIInteractable, CollisionInfo>();
        private bool justStarted = false;
        private Vector3 lastInteractionPoint;
        private PhysicsScene physicsScene;
        private RaycastHit[] sphereCastHits = new RaycastHit[200];
        private Collider[] overlapSphereHits = new Collider[200];
        private GameObject visualsObject;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
            physicsScene = gameObject.scene.GetPhysicsScene();
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold, InteractionSelectionRadius);
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
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold, InteractionSelectionRadius);

            UpdateVisuals();
        }
#endif

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            justStarted = true;
        }

        /// <inheritdoc />
        protected override void OnHoverEntering(HoverEnterEventArgs args)
        {
            base.OnHoverEntering(args);
            gestureLogic.OnHoverEntering(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        protected override void OnHoverExiting(HoverExitEventArgs args)
        {
            base.OnHoverExiting(args);
            gestureLogic.OnHoverExiting(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            // Following the logic in XRPokeInteractor
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                validTargets.Clear();

                Transform attachTransform = GetAttachTransform(null);
                Vector3 interactionPoint = attachTransform.position;

                List<Vector3> directions = new List<Vector3>();

                for (int x = -45; x < 45; x = x + 15)
                {
                    for (int z = -45; z < 45; z = z + 15)
                    {
                        Vector3 direction = Quaternion.Euler(x, 0, z) * attachTransform.up;
                        bool validInteractable = false;
                        if (Physics.Raycast(interactionPoint,
                                            direction,
                                            out RaycastHit hitInfo,
                                            InteractionHoverRadius,
                                            // FIXME: physics layers should be allowed to be set in inpsector
                                            Physics.AllLayers,
                                            // FIXME: QueryTriggerInteraction should be allowed to be set in inpsector
                                            QueryTriggerInteraction.Ignore))
                        {
                            if (interactionManager.TryGetInteractableForCollider(hitInfo.collider, out var interactable) &&
                                interactable is IHPUIInteractable hpuiInteractable &&
                                hpuiInteractable.IsHoverableBy(this))
                            {
                                validInteractable = true;
                                if (validTargets.TryGetValue(hpuiInteractable, out CollisionInfo info))
                                {
                                    if (hitInfo.distance < info.distance)
                                    {
                                        validTargets[hpuiInteractable] = new CollisionInfo(hitInfo.distance, hitInfo.point);
                                    }
                                }
                                else
                                {
                                    validTargets.Add(hpuiInteractable, new CollisionInfo(hitInfo.distance, hitInfo.point));
                                }
                            }
                        }

                        if (Visuals == HPUIInteractorVisuals.Ray)
                        {
                            Color rayColor = validInteractable ? Color.green : Color.red;
                            Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * InteractionHoverRadius, rayColor);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                gestureLogic.Update(validTargets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.distance));
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            base.GetValidTargets(targets);

            targets.Clear();
            IEnumerable<IHPUIInteractable> filteredValidTargets = validTargets
                .Where(kvp => (kvp.Key is IHPUIInteractable))
                .GroupBy(kvp => kvp.Key.zOrder)
                .Select(g => g
                        .OrderBy(kvp => kvp.Value.distance)
                        .First()
                        .Key)
                .OrderBy(ht => ht.zOrder);
            targets.AddRange(filteredValidTargets);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            bool canSelect = validTargets.TryGetValue(interactable as IHPUIInteractable, out CollisionInfo info) &&
                info.distance < interactionSelectionRadius &&
                ProcessSelectFilters(interactable);
            return canSelect && (!SelectOnlyPriorityTarget || gestureLogic.IsPriorityTarget(interactable as IHPUIInteractable));
        }

        /// <summary>
        /// Update the visuals being showne. Called when the <see cref="Visuals"/> is updated or the component is updated in inspector.
        /// </summary>
        protected void UpdateVisuals()
        {
            switch(visuals)
            {
                case HPUIInteractorVisuals.HoverSphere:
                case HPUIInteractorVisuals.SelectSphere:
                    if (visualsObject == null)
                    {
                        visualsObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        visualsObject.transform.SetParent(GetAttachTransform(null), false);
                        if (visualsObject.TryGetComponent<Collider>(out Collider debugCollider))
                        {
                            Destroy(debugCollider);
                        }
                    }
                    string name;
                    float radius;
                    if (visuals == HPUIInteractorVisuals.HoverSphere)
                    {
                        name = "Hover";
                        radius = InteractionHoverRadius;
                    }
                    else
                    {
                        name = "Select";
                        radius = InteractionSelectionRadius;
                    }

                    visualsObject.name = $"[HPUI] Visual - {name}: " + this;
                    visualsObject.transform.localScale = Vector3.one * radius;
                    break;
                default:
                    if (visualsObject != null)
                    {
                        Destroy(visualsObject);
                    }
                    break;
            }
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

        /// <inheritdoc />
        public Vector3 GetCollisionPoint(IHPUIInteractable interactable)
        {
            if (validTargets.TryGetValue(interactable, out CollisionInfo info))
            {
                return info.point;
            }
            return GetAttachTransform(interactable).position;
        }
        #endregion

        struct CollisionInfo
        {
            public float distance;
            public Vector3 point;

            public CollisionInfo(float distance, Vector3 point) : this()
            {
                this.distance = distance;
                this.point = point;
            }

        }
    }
}
 
