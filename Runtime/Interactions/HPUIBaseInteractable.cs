using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIBaseInteractable: XRBaseInteractable
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

        //TODO: make following configs an asset
        [Tooltip("If true, would process gestures on the interactable.")]
        public bool allowSurfaceGestures = false;

        public float tapTimeThreshold = 0.3f;
        public float tapDistanceThreshold = 0.001f;
        public float swipeTimeThreshold = 0.4f;
        public float swipeVelocityThreshold = 0.4f;

        public Collider boundsCollider;

        [SerializeField]
        private HPUITapEvent tapEvent = new HPUITapEvent();

        /// <summary>
        /// Event triggered on tap
        /// </summary>
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        private HPUISwipeEvent swipeEvent = new HPUISwipeEvent();

        /// <summary>
        /// Event triggered on swipe
        /// </summary>
        public HPUISwipeEvent SwipeEvent { get => swipeEvent; set => swipeEvent = value; }

        [SerializeField]
        private HPUISlideEvent slideEvent = new HPUISlideEvent();

        /// <summary>
        /// Event triggered on slide
        /// </summary>
        public HPUISlideEvent SlideEvent { get => slideEvent; set => slideEvent = value; }

        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUISwipeEventArgs> hpuiSwipeEventArgsPool = new LinkedPool<HPUISwipeEventArgs>(() => new HPUISwipeEventArgs());
        private LinkedPool<HPUISlideEventArgs> hpuiSlideEventArgsPool = new LinkedPool<HPUISlideEventArgs>(() => new HPUISlideEventArgs());

        private Dictionary<IXRInteractor, HPUIInteractionState> states = new Dictionary<IXRInteractor, HPUIInteractionState>();
        private float previousTime;

        private Vector2 surfaceBounds, surfaceOrigin;

        #region overrides
        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            getDistanceOverride = GetDistanceOverride;
        }

        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);
            HPUIInteractionState state = InitializeState(GenericPool<HPUIInteractionState>.Get(), args.interactorObject);
            states.Add(args.interactorObject, state);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            if (states.Remove(args.interactorObject, out HPUIInteractionState state))
            {
                switch (state.gestureState)
                {
                    case HPUIGestureState.Tap:
                        using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
                        {
                            TapEvent?.Invoke(tapEventArgs);
                        }
                        break;
                    case HPUIGestureState.Swipe:
                        using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                        {
                            // TODO set params?
                            SwipeEvent?.Invoke(swipeEventArgs);
                        }
                        break;
                }
                GenericPool<HPUIInteractionState>.Release(state);
            }
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            previousTime = Time.time;

            ComputeSurfaceBounds();
        }
        #endregion

        /// <summary>
        /// Compute and store the surface bounds to compute point on surface
        /// </summary>
        protected virtual void ComputeSurfaceBounds()
        {
            if (boundsCollider == null)
            {
                boundsCollider = colliders[0];
                Debug.LogWarning($"boundsCollider is not set. Using {boundsCollider.name}'s collider.");
            }

            Bounds colliderBounds = boundsCollider.bounds;
            Transform interactableTransform = GetAttachTransform(null);
            surfaceOrigin = ComputeTargetPointOnInteractablePlane(colliderBounds.center
                                                           - interactableTransform.right.normalized * colliderBounds.extents.x
                                                           - interactableTransform.forward.normalized * colliderBounds.extents.z,
                                                           interactableTransform);

            surfaceBounds = ComputeTargetPointOnInteractablePlane(colliderBounds.center
                                                           + interactableTransform.right.normalized * colliderBounds.extents.x
                                                           + interactableTransform.forward.normalized * colliderBounds.extents.z,
                                                           interactableTransform) - surfaceOrigin;
        }

        /// <inheritdoc />
        protected void Update()
        {
            float currentTime = Time.time;
            foreach(var interactor in interactorsSelecting)
            {
                Vector2 currentPosition = ComputeInteractorPostion(interactor);
                HPUIInteractionState state = states[interactor];
                float timeDiff = currentTime - state.startTime;
                Vector2 direction =  - state.startPosition;

                switch(state.gestureState)
                {
                    case HPUIGestureState.Tap:
                        if (timeDiff > tapTimeThreshold || direction.magnitude > tapDistanceThreshold)
                        {
                            goto case HPUIGestureState.Swipe;
                        }
                        break;
                    case HPUIGestureState.Swipe:
                        if (timeDiff > swipeTimeThreshold || ComputeVelocity(direction, currentTime - previousTime) > swipeVelocityThreshold)
                        {
                            state.gestureState = HPUIGestureState.Slide;
                            goto case HPUIGestureState.Slide;
                        }
                        state.gestureState = HPUIGestureState.Swipe;
                        break;
                    case HPUIGestureState.Slide:
                        using (hpuiSlideEventArgsPool.Get(out HPUISlideEventArgs slideEventArgs))
                        {
                            // TODO: set params
                            SlideEvent?.Invoke(slideEventArgs);
                        }
                        break;
                    case HPUIGestureState.Custom:
                        // TODO: custom gestures
                        throw new NotImplementedException();
                }

                state.previousPosition = currentPosition;
            }
            previousTime = currentTime;
        }

        protected DistanceInfo GetDistance(IXRInteractor interactor)
        {
            return GetDistanceOverride(this, interactor.GetAttachTransform(this).transform.position);
        }

        protected DistanceInfo GetDistanceOverride(IXRInteractable interactable, Vector3 position)
        {
            XRInteractableUtility.TryGetClosestPointOnCollider(interactable, position, out DistanceInfo info);
            return info;
        }

        protected HPUIInteractionState InitializeState(HPUIInteractionState state, IXRInteractor interactor)
        {
            return state.SetParams(HPUIGestureState.Tap, Time.time, ComputeInteractorPostion(interactor), GetDistance(interactor));
        }

        /// <summary>
        /// Get the projection of the interactors position on the xz plane of this interactable, normalized.
        /// the returned Vector2 - (x, z) on the xz-plane.
        /// (0, 0) would be the bounds min on the surface & (1, 1) the bounds max on the surface.
        /// </summary>
        protected virtual Vector2 ComputeInteractorPostion(IXRInteractor interactor)
        {
            Vector3 closestPointOnCollider = GetDistanceOverride(this, interactor.GetAttachTransform(this).position).point;
            Vector2 pointOnPlane = ComputeTargetPointOnInteractablePlane(closestPointOnCollider, GetAttachTransform(interactor));
            return (pointOnPlane - surfaceOrigin) / surfaceBounds;
        }

        /// <summary>
        /// Compute the projection of the target point on the XZ plane of the a given transform.
        /// the returned Vector2 - (x, z) on the xz-plane.
        /// </summary>
        protected Vector2 ComputeTargetPointOnInteractablePlane(Vector3 targetPoint, Transform interactableTransform)
        {

            Plane xzPlane = new Plane(interactableTransform.up, interactableTransform.position);

            Vector3 pointOnXZPlane = xzPlane.ClosestPointOnPlane(targetPoint);
            pointOnXZPlane = transform.InverseTransformPoint(pointOnXZPlane);
            return new Vector2(pointOnXZPlane.x, pointOnXZPlane.z);
        }

        protected float ComputeVelocity(Vector2 directionVector, float timeDiff)
        {
            return directionVector.magnitude / timeDiff;
        }

        protected class HPUIInteractionState
        {
            public HPUIGestureState gestureState;
            public float startTime;
            public Vector2 startPosition;
            public Vector2 previousPosition;
            public DistanceInfo distanceInfo;

            public HPUIInteractionState SetParams(HPUIGestureState gestureState, float startTime, Vector2 startPosition, DistanceInfo distanceInfo)
            {
                this.gestureState = gestureState;
                this.startTime = startTime;
                this.startPosition = startPosition;
                this.previousPosition = startPosition;
                this.distanceInfo = distanceInfo;
                return this;
            }
        }
    }

    public enum HPUIGestureState
    {
        Tap, Swipe, Slide,
        Custom // TODO: Custom gestures?
    }

    #region events classes
    public class HPUIGestureEvent: UnityEvent<HPUIGestureEventArgs>
    {}

    /// <summary>
    /// Event data associated with an gesture interaction on HPUI
    /// </summary>
    public class HPUIGestureEventArgs: BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IXRSelectInteractor interactorObject
        {
            get => (IXRSelectInteractor)base.interactorObject;
            set => base.interactorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new HPUIBaseInteractable interactableObject
        {
            get => (HPUIBaseInteractable)base.interactableObject;
            set => base.interactableObject = value;
        }
    }

    [Serializable]
    public class HPUITapEvent: UnityEvent<HPUITapEventArgs>
    {}

    /// <summary>
    /// Event data associated with an tap gesture interaction on HPUI
    /// </summary>
    public class HPUITapEventArgs: HPUIGestureEventArgs
    {}

    [Serializable]
    public class HPUISwipeEvent: UnityEvent<HPUISwipeEventArgs>
    {}

    /// <summary>
    /// Event data associated with an swipe gesture interaction on HPUI
    /// </summary>
    public class HPUISwipeEventArgs: HPUIGestureEventArgs
    {}

    [Serializable]
    public class HPUISlideEvent: UnityEvent<HPUISlideEventArgs>
    {}

    /// <summary>
    /// Event data associated with an slide gesture interaction on HPUI
    /// </summary>
    public class HPUISlideEventArgs: HPUIGestureEventArgs
    {}
    #endregion
}
