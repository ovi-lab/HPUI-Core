using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIBaseInteractable: XRBaseInteractable, IHPUIInteractable
    {
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
        /// /// <seealso cref="ComputeInteractorPosition"/>
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

        /// <inheritdoc />
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        private HPUIGestureEvent gestureEvent = new HPUIGestureEvent();

        /// <inheritdoc />
        public HPUIGestureEvent GestureEvent { get => gestureEvent; set => gestureEvent = value; }

        #region overrides
        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            selectMode = InteractableSelectMode.Single;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            ComputeSurfaceBounds();
        }
        #endregion


        #region surface point calculations
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
            boundsMax = ComputeTargetPointOnTransformXZPlane(colliderBounds.max, interactableTransform);
            boundsMin = ComputeTargetPointOnTransformXZPlane(colliderBounds.min, interactableTransform);
        }

        /// <summary>
        /// Compute the projection of the target point on the XZ plane of the a given transform.
        /// the returned Vector2 - (x, z) on the xz-plane.
        /// </summary>
        protected Vector2 ComputeTargetPointOnTransformXZPlane(Vector3 targetPoint, Transform interactableTransform)
        {

            Plane xzPlane = new Plane(interactableTransform.up, interactableTransform.position);

            Vector3 pointOnXZPlane = xzPlane.ClosestPointOnPlane(targetPoint);

            // InverseTransformPoint without taking scale.
            Matrix4x4 worldToLocalMatrix = Matrix4x4.TRS(interactableTransform.position, interactableTransform.rotation, Vector3.one).inverse;
            pointOnXZPlane = worldToLocalMatrix.MultiplyPoint3x4(pointOnXZPlane);
            return new Vector2(pointOnXZPlane.x, pointOnXZPlane.z);
        }
        #endregion

        #region IHPUIInteractable interface
        /// <inheritdoc />
        public virtual bool ComputeInteractorPosition(IHPUIInteractor interactor, out Vector2 position)
        {
            if (interactor.GetDistanceInfo(this, out DistanceInfo info))
            {
                position = ComputeTargetPointOnTransformXZPlane(info.point, GetAttachTransform(interactor));
                return true;
            }
            position = Vector2.zero;
            return false;
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
                    return TapEvent.GetAllEventsCount() > 0;
                }
                case HPUIGesture.Gesture: {
                    return GestureEvent.GetAllEventsCount() > 0;
                }
                default:
                    throw new InvalidOperationException($"Gesture state {state} is not handled by {typeof(HPUIBaseInteractable)}");
            }
        }
        #endregion
    }
}
