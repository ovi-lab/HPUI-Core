using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    public enum HPUIGesture
    {
        None,
        Gesture
    }

    public enum HPUIGestureState
    {
        Started, Updated, Stopped, Invalid, Canceled
    }

    #region events classes
    /// <summary>
    /// Event class that reports hover events
    /// </summary>
    public class HPUIHoverUpdateEvent : UnityEvent<HPUIHoverUpdateEventArgs>
    { }

    // NOTE: This is used instead of the IXRHoverStrength* interfaces as those
    // interfaces don't report the data being exposed here.
    /// <summary>
    /// Event data associated with a Hover update on HPUI
    /// </summary>
    public class HPUIHoverUpdateEventArgs
    {
        /// <summary>
        /// Instantiate hover event.
        /// </summary>
        public HPUIHoverUpdateEventArgs(IHPUIInteractor interactorObject, Vector3 hoverPoint, Vector3 attachPoint)
        {
            this.interactorObject = interactorObject;
            this.hoverPoint = hoverPoint;
            this.attachPoint = attachPoint;
        }

        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public IHPUIInteractor interactorObject { get; set; }

        /// <summary>
        /// The endpoint of the hover. This is generally the centroid when
        /// the interactor is hovering on targets.
        /// </summary>
        public Vector3 hoverPoint { get; set; }

        /// <summary>
        /// The location of the attachTransform.
        /// <seealso cref="XRBaseInteractor.GetAttachTransform"/>
        /// </summary>
        public Vector3 attachPoint { get; set; }
    }

    /// <summary>
    /// Base event class for gesture events
    /// </summary>
    public class HPUIInteractionEvent<T> : UnityEvent<T> where T : BaseInteractionEventArgs
    {
        protected int eventsCount = 0;

        /// <summary>
        /// Get total number of listeners.
        /// </summary>
        public int GetAllEventsCount()
        {
            return eventsCount + GetPersistentEventCount();
        }

        /// <inheritdoc />
        public new void AddListener(UnityAction<T> call)
        {
            base.AddListener(call);
            eventsCount++;
        }

        /// <inheritdoc />
        public new void RemoveListener(UnityAction<T> call)
        {
            base.RemoveListener(call);
            eventsCount--;
            RemoveAllListeners();
        }

        /// <inheritdoc />
        public new void RemoveAllListeners()
        {
            eventsCount = 0;
        }
    }

    [Serializable]
    public class HPUIGestureEvent : HPUIInteractionEvent<HPUIGestureEventArgs>
    { }

    /// <summary>
    /// Event data associated with a gesture interaction on HPUI
    /// </summary>
    public class HPUIGestureEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IXRSelectInteractor interactorObject
        {
            get => (IHPUIInteractor)base.interactorObject;
            set => base.interactorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IHPUIInteractable interactableObject
        {
            get => (IHPUIInteractable)base.interactableObject;
            set => base.interactableObject = value;
        }

        /// <summary>
        /// The position of the interaction on the plane of the
        /// interactable relative to its position.
        /// </summary>
        public virtual Vector2 Position { get; private set; }

        // TODO: Documentation
        public HPUIGestureState State { get; private set; }
        public float TimeDelta { get; private set; }
        public float StartTime { get; private set; }
        public Vector2 StartPosition { get; private set; }
        public Vector2 CumulativeDirection { get; private set; }
        public float CumulativeDistance { get; private set; }
        public Vector2 DeltaDirection { get; private set; }
        public IHPUIInteractable CurrentTrackingInteractable { get; private set; }
        public Vector2 CurrentTrackingInteractablePoint { get; private set; }

        public HPUIGestureEventArgs(IHPUIInteractor interactor, IHPUIInteractable interactable, HPUIGestureState state, float timeDelta, float startTime,
                                    Vector2 startPosition, Vector2 cumulativeDirection, float cumulativeDistance, Vector2 deltaDirection,
                                    IHPUIInteractable currentTrackingInteractable, Vector2 currentTrackingInteractablePoint)
        {
            Position = startPosition + cumulativeDirection;
            State = state;
            TimeDelta = timeDelta;
            StartTime = startTime;
            StartPosition = startPosition;
            CumulativeDirection = cumulativeDirection;
            CumulativeDistance = cumulativeDistance;
            DeltaDirection = deltaDirection;
            CurrentTrackingInteractable = currentTrackingInteractable;
            CurrentTrackingInteractablePoint = currentTrackingInteractablePoint;
        }
    }

    /// <summary>
    /// Event related to deformable continuous surface.
    /// <seealso cref="HPUIGeneratedContinuousInteractable"/>
    /// <seealso cref="DeformableSurface"/>
    /// <seealso cref="DeformableSurfaceCollidersManager"/>
    /// </summary>
    [Serializable]
    public class HPUIContinuousSurfaceEvent : UnityEvent<HPUIContinuousSurfaceCreatedEventArgs>
    { }

    /// <summary>
    /// Event args for HPUIContinuousSurfaceEvent
    /// </summary>
    public class HPUIContinuousSurfaceCreatedEventArgs
    {
        /// <summary>
        /// The interactable object related to the continuous surface
        /// </summary>
        public IHPUIInteractable interactableObject;

        public HPUIContinuousSurfaceCreatedEventArgs(IHPUIInteractable interactableObject)
        {
            this.interactableObject = interactableObject;
        }
    }

    #endregion
}
