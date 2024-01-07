using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core
{
    public enum HPUIGestureState
    {
        Tap, Swipe,
        Custom // TODO: Custom gestures?
    }

    public enum HPUISwipeState
    {
        Started, Updated, Stopped, Invalid
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

        public virtual void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable)
        {
            interactorObject = interactor;
            interactableObject = interactable;
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
    {
        public HPUISwipeState State { get; private set; }
        public float TimeDelta { get; private set; }
        public float StartTime { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 CurrentPosition { get; private set; }
        public Vector3 Direction { get; private set; }
        public Vector3 DeltaDirection { get => CurrentPosition - PreviousPosition; }

        public override void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable)
        {
            throw new InvalidOperationException("Call overloaded method!");
        }

        public void SetParams(IHPUIInteractor interactor, IHPUIInteractable interactable, HPUISwipeState state, float timeDelta, float startTime, Vector3 startPosition, Vector3 previousPosition, Vector3 currentPosition, Vector3 direction)
        {
            base.SetParams(interactor, interactable);
            State = state;
            TimeDelta = timeDelta;
            StartTime = startTime;
            StartPosition = startPosition;
            PreviousPosition = previousPosition;
            CurrentPosition = currentPosition;
            Direction = direction;
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
