using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

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
