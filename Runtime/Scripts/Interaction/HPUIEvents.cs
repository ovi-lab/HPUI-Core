using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Represents the high-level lifecycle states of a gesture delivered by the HPUI interactor.
    /// These are the primary states communicated to interactables to indicate the progress of a gesture:
    /// - Canceled: The gesture was aborted before completion (e.g., due to an error or interruption).
    /// - None: No gesture action to be reported (typically used during the commit delay window).
    /// - Started: The first gesture-state that is fired after the commit delay elapses.
    /// - Updated: The gesture is ongoing and provides updated positional/delta data.
    /// - Stopped: The gesture has completed and stopped normally.
    /// </summary>
    public enum HPUIGestureState
    {
        /// <summary>
        /// The gesture was canceled before it could complete. This can occur due to errors,
        /// or early interruption resulting in a cancellation.
        /// </summary>
        Canceled,

        /// <summary>
        /// No gesture should be reported. Used for intermediary frames (for example, during the commit delay)
        /// when a selection is happening but the gesture has not yet committed.
        /// </summary>
        None,

        /// <summary>
        /// The gesture has just started (the commit delay has elapsed and the gesture is now active).
        /// This is the first confirmed gesture state after commit.
        /// </summary>
        Started,

        /// <summary>
        /// The gesture is actively ongoing; subsequent updates to the gesture (position, delta, etc.)
        /// should be reported with this state.
        /// </summary>
        Updated,

        /// <summary>
        /// The gesture has finished successfully and has stopped.
        /// </summary>
        Stopped
    }

    /// <summary>
    /// Represents per-interactable states related to hovering, contact, and tracking.
    /// These states are reported alongside the main gesture state to inform each interactable of
    /// its local status (e.g., hovered, in contact, currently being tracked).
    /// </summary>
    public enum HPUIInteractableState
    {
        /// <summary>
        /// The interactor is in contact with the interactable (selection/press is active),
        /// but the interactable is not the current tracking target.
        /// When one of tracker states (TrackingStarted, TrackingEnded, TrackingUpdate) is reported,
        /// they imply the interactor is in contact with that interactable.
        /// </summary>
        InContact,

        /// <summary>
        /// The interactor is merely hovering over the interactable with no selection in progress.
        /// </summary>
        Hovered,

        /// <summary>
        /// The interactable has become the current tracking target and tracking for this interactable has started.
        /// </summary>
        TrackingStarted,

        /// <summary>
        /// The interactable was previously the tracking target but has just stopped being tracked.
        /// </summary>
        TrackingEnded,

        /// <summary>
        /// The interactable is the current tracking target and should receive position/delta updates.
        /// </summary>
        TrackingUpdate
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
    public class HPUIInteractionEvent<T> : UnityEvent<T>
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

    [Serializable]
    public class HPUIInteractableStateEvent : HPUIInteractionEvent<HPUIInteractableStateEventArgs>
    { }

    [Serializable]
    public class HPUIInteractorGestureEvent : HPUIInteractionEvent<HPUIInteractorGestureEventArgs>
    { }

    public abstract class BaseHPUIGestureEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public IXRSelectInteractor interactorObject { get; set; }

        /// <summary>
        /// The position of the interaction on the plane of the
        /// interactable relative to its position.
        /// </summary>
        public virtual Vector2 Position { get; protected set; }

        /// <summary>
        /// The time elapsed, in seconds, since the gesture started for the current gesture evaluation
        /// (typically computed as the current frame time minus the gesture start time).
        /// </summary>
        public float TimeDelta { get; protected set; }

        /// <summary>
        /// The timestamp (Time.time) when the current gesture began. This represents the reference
        /// start time used to compute TimeDelta and other duration-based measurements.
        /// </summary>
        public float StartTime { get; protected set; }

        /// <summary>
        /// The initial position, in the local coordinate space of the tracked interactable, where
        /// the gesture began. This is the anchor point used for computing deltas and cumulative metrics.
        /// </summary>
        public Vector2 StartPosition { get; protected set; }

        /// <summary>
        /// The cumulative movement vector accumulated since the gesture started. This is the
        /// vector sum of all per-frame DeltaDirection values and can be used to determine overall
        /// directionality of the gesture.
        /// </summary>
        public Vector2 CumulativeDirection { get; protected set; }

        /// <summary>
        /// The total distance traveled since the gesture started, computed as the sum of the
        /// magnitudes of all per-frame movement deltas.
        /// </summary>
        public float CumulativeDistance { get; protected set; }

        /// <summary>
        /// The movement delta (vector) computed for the current frame relative to the previous
        /// frame's tracked position. This represents the most recent instantaneous change in position.
        /// </summary>
        public Vector2 DeltaDirection { get; protected set; }

        public BaseHPUIGestureEventArgs(IHPUIInteractor interactor,
                                        float timeDelta, float startTime,
                                        Vector2 startPosition, Vector2 cumulativeDirection, float cumulativeDistance,
                                        Vector2 deltaDirection)
        {
            interactorObject = interactor;
            Position = startPosition + cumulativeDirection;
            TimeDelta = timeDelta;
            StartTime = startTime;
            StartPosition = startPosition;
            CumulativeDirection = cumulativeDirection;
            CumulativeDistance = cumulativeDistance;
            DeltaDirection = deltaDirection;
        }
    }

    /// <summary>
    /// Event data associated with gesture interaction on HPUI fired by the
    /// interactor. Provides a summary of the interactables in the interaction.
    /// </summary>
    public class HPUIInteractorGestureEventArgs : BaseHPUIGestureEventArgs
    {
        /// <summary>
        /// The interactables recieving gesture events.
        /// </summary>
        public IReadOnlyDictionary<IHPUIInteractable, HPUIGestureState> InteractableGestureStates { get; protected set; }

        /// <summary>
        /// The interactables recieving aux gesture events.
        /// </summary>
        public IReadOnlyDictionary<IHPUIInteractable, HPUIInteractableState> InteractableAuxGestureStates { get; protected set; }

        /// <summary>
        /// The state of the interactable.
        /// </summary>
        public HPUIGestureState State { get; private set; }

        public HPUIInteractorGestureEventArgs(IHPUIInteractor interactor, HPUIGestureState state,
                                              IReadOnlyDictionary<IHPUIInteractable, HPUIGestureState> interactableGestureStates,
                                              IReadOnlyDictionary<IHPUIInteractable, HPUIInteractableState> interactableAuxGestureStates,
                                              float timeDelta, float startTime,
                                              Vector2 startPosition, Vector2 cumulativeDirection, float cumulativeDistance, Vector2 deltaDirection) :
            base(interactor, timeDelta, startTime, startPosition, cumulativeDirection, cumulativeDistance, deltaDirection)
        {
            InteractableGestureStates = interactableGestureStates;
            InteractableAuxGestureStates = interactableAuxGestureStates;
            State = state;
        }
    }

    /// <summary>
    /// Event data associated with a auxiliary gesture interaction on HPUI
    /// </summary>
    public class HPUIInteractableStateEventArgs : BaseInteractionEventArgs
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

        /// <summary>
        /// The auxiliary state of the interactable.
        /// </summary>
        public HPUIInteractableState State { get; private set; }

        public HPUIInteractableStateEventArgs(IHPUIInteractor interactor, IHPUIInteractable interactable, Vector2 position, HPUIInteractableState state)
        {
            interactorObject = interactor;
            interactableObject = interactable;
            Position = position;
            State = state;
        }
    }

    /// <summary>
    /// Event data associated with a gesture interaction on HPUI
    /// </summary>
    public class HPUIGestureEventArgs : BaseHPUIGestureEventArgs
    {
        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public IHPUIInteractable interactableObject { get; set; }

        /// <summary>
        /// The state of the interactable.
        /// </summary>
        public HPUIGestureState State { get; private set; }

        public HPUIGestureEventArgs(IHPUIInteractor interactor, IHPUIInteractable interactable,
                                    HPUIGestureState state,
                                    float timeDelta, float startTime,
                                    Vector2 startPosition, Vector2 cumulativeDirection, float cumulativeDistance, Vector2 deltaDirection) :
            base(interactor, timeDelta, startTime, startPosition, cumulativeDirection, cumulativeDistance, deltaDirection)
        {
            interactableObject = interactable;
            State = state;
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
