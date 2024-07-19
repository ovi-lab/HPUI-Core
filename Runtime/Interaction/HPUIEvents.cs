using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Interaction
{
    public enum HPUIGesture
    {
        None,
        Tap, Gesture
        // TODO: Custom gestures?
    }

    public enum HPUIGestureState
    {
        Started, Updated, Stopped, Invalid
    }

    #region events classes
    /// <summary>
    /// Event class that reports hover events
    /// </summary>
    public class HPUIHoverUpdateEvent : UnityEvent<HPUIHoverUpdateEventArgs>
    {}

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
        /// The location of the attach transform.
        /// <seealso cref="XRBaseInteractor.GetAttachTransform"/>
        /// </summary>
        public Vector3 attachPoint { get; set; }
    }

    /// <summary>
    /// Base event class for tap/gesture events
    /// </summary>
    public class HPUIInteractionEvent<T>: UnityEvent<T> where T: HPUIInteractionEventArgs
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

    /// <summary>
    /// Event data associated with an gesture interaction on HPUI
    /// </summary>
    public class HPUIInteractionEventArgs: BaseInteractionEventArgs
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

        public virtual void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable, Vector2 position)
        {
            interactorObject = interactor;
            interactableObject = interactable;
            Position = position;
        }
    }

    [Serializable]
    public class HPUITapEvent: HPUIInteractionEvent<HPUITapEventArgs>
    {}

    /// <summary>
    /// Event data associated with an tap gesture interaction on HPUI
    /// </summary>
    public class HPUITapEventArgs: HPUIInteractionEventArgs
    {}

    [Serializable]
    public class HPUIGestureEvent: HPUIInteractionEvent<HPUIGestureEventArgs>
    {}

    /// <summary>
    /// Event data associated with a gesture interaction on HPUI
    /// </summary>
    public class HPUIGestureEventArgs: HPUIInteractionEventArgs
    {
        // TODO: Documentation
        public HPUIGestureState State { get; private set; }
        public float TimeDelta { get; private set; }
        public float StartTime { get; private set; }
        public Vector2 StartPosition { get; private set; }
        public Vector2 CumilativeDirection { get; private set; }
        public float CumilativeDistance { get; private set; }
        public Vector2 DeltaDirection { get; private set; }

        public override void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable, Vector2 position)
        {
            throw new InvalidOperationException("Call overloaded method!");
        }

        public void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable, HPUIGestureState state, float timeDelta, float startTime,
                              Vector2 startPosition, Vector2 cumilativeDirection, float cumilativeDistance, Vector2 deltaDirection)
        {
            base.SetParams(interactor, interactable, startPosition + cumilativeDirection);
            State = state;
            TimeDelta = timeDelta;
            StartTime = startTime;
            StartPosition = startPosition;
            CumilativeDirection = cumilativeDirection;
            CumilativeDistance = cumilativeDistance;
            DeltaDirection = deltaDirection;
        }
    }

    /// <summary>
    /// Event related to deformable continuous surface.
    /// <seealso cref="HPUIContinuousInteractable"/>
    /// <seealso cref="DeformableSurface"/>
    /// <seealso cref="DeformableSurfaceCollidersManager"/>
    /// </summary>
    [Serializable]
    public class HPUIContinuousSurfaceEvent: UnityEvent<HPUIContinuousSurfaceCreatedEventArgs>
    {}

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
