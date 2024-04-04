using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Encapsulates the logic for HPUI gesture interactions.
    /// The interactor dectates which interactable gets the gesture.
    /// </summary>
    public class HPUIGestureLogic: IHPUIGestureLogic
    {
        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUIGestureEventArgs> hpuiGestureEventArgsPool = new LinkedPool<HPUIGestureEventArgs>(() => new HPUIGestureEventArgs());

        private float tapTimeThreshold, tapDistanceThreshold, interactionSelectionRadius;
        private IHPUIInteractor interactor;

        private float startTime, cumilativeDistance, timeDelta;
        private Vector2 delta, previousPosition, cumilativeDirection;

        private int lowestTargetZIndex = int.MaxValue;
        private int activeSelectingInteractablesCount = 0;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGesture interactorGestureState = HPUIGesture.None;

        /// <summary>
        /// Initializes a new instance of the with the thrshold values.
        /// </summary>
        public HPUIGestureLogic(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold, float interactionSelectionRadius)
        {
            this.interactor = interactor;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.interactionSelectionRadius = interactionSelectionRadius;
            Reset();
        }

        // NOTE: This gets called only within the tapdistancethreshold window.
        // Thus using distance as opposed to start time to pick the target that is the most ideal.
        private void ComputeActivePriorityInteractable()
        {
            // Targets not selected within the priority window
            // (defaults to tapdistancethreshold), will not get any
            // events.  For targets selected withing the window, first
            // prioritize the zOrder, then the time.
            IHPUIInteractable interactableToBeActive = trackingInteractables
                .Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) && kvp.Value.selectableTarget)
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => kvp.Value.minDistanceToInteractor)
                .FirstOrDefault().Key;

            if (interactableToBeActive != activePriorityInteractable)
            {
                activePriorityInteractable = interactableToBeActive;
            }
        }

        protected void Reset()
        {
            activeSelectingInteractablesCount = 0;
            interactorGestureState = HPUIGesture.None;
            trackingInteractables.Clear();
            activePriorityInteractable = null;
            lowestTargetZIndex = int.MaxValue;
            currentTrackingInteractable = null;
            cumilativeDistance = 0;
            cumilativeDirection = Vector2.zero;
        }

        // TODO Move this to when selection happens
        // bool inValidWindow = (currentTime - startTime) < tapTimeThreshold;
        // // If a new higher priority targets is encountered within tap time window, we hand over control to that.
        // if (interactable.zOrder < lowestTargetZIndex && inValidWindow)
        // {
        //     lowestTargetZIndex = interactable.zOrder;

        //     foreach(HPUIInteractionState interactionState in trackingInteractables.Values)
        //     {
        //         interactionState.startTime = currentTime;
        //     }
        // }

        /// <inheritdoc />
        public void Update(IDictionary<IHPUIInteractable, float> distances)
        {
            bool updateTrackingInteractable = false;
            foreach(IHPUIInteractable interactable in distances.Keys.Union(trackingInteractables.Keys))
            {
                bool isTracked = trackingInteractables.TryGetValue(interactable, out HPUIInteractionState state);
                bool isInFrame = distances.TryGetValue(interactable, out float distance);

                if (!isTracked || !state.active)
                {

                    // TODO Move this to when selection happens
                    // if (interactorGestureState == HPUIGesture.None)
                    // {
                    //     interactorGestureState = HPUIGesture.Tap;
                    //     startTime = Time.time;
                    // }

                    if (isTracked)
                    {
                        state.active = true;
                    }
                    else
                    {
                        state = new HPUIInteractionState(Time.time, interactable.ComputeInteractorPostion(interactor), false);
                        trackingInteractables.Add(interactable, state);
                    }

                    updateTrackingInteractable = true;
                }

                if (isInFrame)
                {
                    if (distance < state.minDistanceToInteractor)
                    {
                        state.minDistanceToInteractor = distance;

                        if (!state.inSelectRange && distance < tapDistanceThreshold)
                        {
                            state.inSelectRange = true;
                        }
                    }
                }
                else
                {
                    state.active = false;
                    updateTrackingInteractable = true;
                }
            }

            if (updateTrackingInteractable)
            {
                // Any target that is active should be ok for this
                // Giving priority to the ones that was the oldest enetered
                // This minimizes the tracking interactable changing
                IHPUIInteractable interactableToTrack = trackingInteractables
                    .Where(kvp => kvp.Value.active)
                    .OrderBy(kvp => kvp.Value.startTime)
                    .First().Key;

                if (interactableToTrack != currentTrackingInteractable)
                {
                    currentTrackingInteractable = interactableToTrack;
                    // If interactable change, we need to restart tracking, hence skipping a frame
                    previousPosition = currentTrackingInteractable.ComputeInteractorPostion(interactor);
                    return;
                }
            }
            
            if (interactorGestureState == HPUIGesture.None)
            {
                return;
            }

            Vector2 currentPosition = currentTrackingInteractable.ComputeInteractorPostion(interactor);
            delta = previousPosition - currentPosition;
            timeDelta = Time.time - startTime;
            cumilativeDistance += delta.magnitude;
            cumilativeDirection += delta;

            switch(interactorGestureState)
            {
                case HPUIGesture.Tap:
                    if (timeDelta > tapTimeThreshold || cumilativeDistance > tapDistanceThreshold)
                    {
                        interactorGestureState = HPUIGesture.Gesture;
                        ComputeActivePriorityInteractable();
                        using (hpuiGestureEventArgsPool.Get(out HPUIGestureEventArgs gestureEventArgs))
                        {
                            HPUIInteractionState state;
                            if (activePriorityInteractable != null)
                            {
                                state = trackingInteractables[activePriorityInteractable];
                            }
                            else
                            {
                                state = HPUIInteractionState.empty;
                            }
                            gestureEventArgs.SetParams(interactor, activePriorityInteractable,
                                                       HPUIGestureState.Started, timeDelta, state.startTime, state.startPosition,
                                                       cumilativeDirection, cumilativeDistance, delta);
                            if (ActiveInteractableCanTriggerEvent())
                            {
                                activePriorityInteractable?.OnGesture(gestureEventArgs);
                            }
                            // NOTE: See note when tap gets triggered.
                            interactor.OnGesture(gestureEventArgs);
                        }
                    }
                    break;
                case HPUIGesture.Gesture:
                    using (hpuiGestureEventArgsPool.Get(out HPUIGestureEventArgs gestureEventArgs))
                    {
                        HPUIInteractionState state;
                        if (activePriorityInteractable != null)
                        {
                            state = trackingInteractables[activePriorityInteractable];
                        }
                        else
                        {
                            state = HPUIInteractionState.empty;
                        }
                        gestureEventArgs.SetParams(interactor, activePriorityInteractable,
                                                   HPUIGestureState.Updated, timeDelta, state.startTime, state.startPosition,
                                                   cumilativeDirection, cumilativeDistance, delta);
                        if (ActiveInteractableCanTriggerEvent())
                        {
                            activePriorityInteractable?.OnGesture(gestureEventArgs);
                        }
                        // NOTE: See note when tap gets triggered.
                        interactor.OnGesture(gestureEventArgs);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown gesture.");
            }

            previousPosition = currentPosition;
        }

        /// <summary>
        /// Clear cached objects.
        /// </summary>
        public void Dispose()
        {
            Reset();
            hpuiTapEventArgsPool.Dispose();
            hpuiGestureEventArgsPool.Dispose();
        }

        /// <inheritdoc />
        public bool IsPriorityTarget(IHPUIInteractable interactable)
        {
            return interactable == activePriorityInteractable;
        }

        class HPUIInteractionState
        {
            public static HPUIInteractionState empty = new HPUIInteractionState(0, Vector2.zero, false);
            public float startTime;
            public Vector2 startPosition;
            public bool active,
                selectableTarget,
                inSelectRange;
            public float minDistanceToInteractor;

            public HPUIInteractionState(float startTime, Vector2 startPosition, bool selectableTarget)
            {
                this.startTime = startTime;
                this.startPosition = startPosition;
                this.active = true;
                this.selectableTarget = selectableTarget;
                this.minDistanceToInteractor = float.MaxValue;
                this.inSelectRange = false;
            }
        }
    }

}
