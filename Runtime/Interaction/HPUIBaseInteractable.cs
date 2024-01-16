using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIBaseInteractable: XRBaseInteractable, IHPUIInteractable
    {
        [Space()]
        [Header("HPUI Configurations")]
        [SerializeField]
        private Handedness handedness;
        public Handedness Handedness
        {
            get => handedness;
            set => handedness = value;
        }

        [SerializeField]
        private Collider boundsCollider;

        /// <summary>
        /// The collider used to compute the bounds of the interactable.
        /// /// <seealso cref="ComputeInteractorPostion"/>
        /// </summary>
        public Collider BoundsCollider { get => boundsCollider; set => boundsCollider = value; }

        [SerializeField]
        private int _zOrder;

        /// <inheritdoc />
        public int zOrder { get => _zOrder; set => _zOrder = value; }

        /// <inheritdoc />
        public virtual Vector2 boundsMax { get; protected set; }

        /// <inheritdoc />
        public virtual Vector2 boundsMin { get; protected set; }

        [SerializeField]
        private HPUITapEvent tapEvent = new HPUITapEvent();

        /// <summary>
        /// Event triggered on tap
        /// </summary>
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        private HPUIGestureEvent gestureEvent = new HPUIGestureEvent();

        /// <summary>
        /// Event triggered on gesture
        /// </summary>
        public HPUIGestureEvent GestureEvent { get => gestureEvent; set => gestureEvent = value; }

        #region overrides
        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            getDistanceOverride = GetDistanceOverride;
            selectMode = InteractableSelectMode.Single;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            ComputeSurfaceBounds();
        }
        #endregion

        /// <summary>
        /// Compute and store the surface bounds to compute point on surface
        /// </summary>
        protected virtual void ComputeSurfaceBounds()
        {
            if (BoundsCollider == null)
            {
                BoundsCollider = colliders[0];
                Debug.LogWarning($"boundsCollider is not set. Using {BoundsCollider.name}'s collider.");
            }

            Bounds colliderBounds = BoundsCollider.bounds;
            Transform interactableTransform = GetAttachTransform(null);
            boundsMax = ComputeTargetPointOnInteractablePlane(colliderBounds.max, interactableTransform);
            boundsMin = ComputeTargetPointOnInteractablePlane(colliderBounds.min, interactableTransform);
        }

        protected DistanceInfo GetDistanceOverride(IXRInteractable interactable, Vector3 position)
        {
            XRInteractableUtility.TryGetClosestPointOnCollider(interactable, position, out DistanceInfo info);
            return info;
        }

        /// <summary>
        /// Compute the projection of the target point on the XZ plane of the a given transform.
        /// the returned Vector2 - (x, z) on the xz-plane.
        /// </summary>
        protected Vector2 ComputeTargetPointOnInteractablePlane(Vector3 targetPoint, Transform interactableTransform)
        {

            Plane xzPlane = new Plane(interactableTransform.up, interactableTransform.position);

            Vector3 pointOnXZPlane = xzPlane.ClosestPointOnPlane(targetPoint);

            // InverseTransformPoint without taking scale.
            Matrix4x4 worldToLocalMatrix = Matrix4x4.TRS(interactableTransform.position, interactableTransform.rotation, Vector3.one).inverse;
            pointOnXZPlane = worldToLocalMatrix.MultiplyPoint3x4(pointOnXZPlane);
            return new Vector2(pointOnXZPlane.x, pointOnXZPlane.z);
        }

        #region IHPUIInteractable interface
        /// <inheritdoc />
        public virtual Vector2 ComputeInteractorPostion(IXRInteractor interactor)
        {
            Vector3 closestPointOnCollider = GetDistanceOverride(this, interactor.GetAttachTransform(this).position).point;
            Vector2 pointOnPlane = ComputeTargetPointOnInteractablePlane(closestPointOnCollider, GetAttachTransform(interactor));
            return pointOnPlane;
        }

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
        public bool HandlesGesture(HPUIGesture state)
        {
            switch (state) {
                case HPUIGesture.Tap: {
                    return TapEvent.GetPersistentEventCount() > 0;
                }
                case HPUIGesture.Gesture: {
                    return GestureEvent.GetPersistentEventCount() > 0;
                }
                default:
                    throw new InvalidOperationException($"Gesture state {state} is not handled by {typeof(HPUIBaseInteractable)}");
            }
        }
        #endregion
    }
}
