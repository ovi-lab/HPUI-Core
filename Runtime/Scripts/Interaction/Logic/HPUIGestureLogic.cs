using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// Encapsulates the logic for HPUI gesture interactions.
    /// The interactor dictates which interactable gets the gesture.
    /// </summary>
    [Serializable]
    public class HPUIGestureLogic : IHPUIGestureLogic
    {
        protected enum LogicState
        {
            NoGesture,
            AwaitingCommit,
            Gesturing
        }

        [Tooltip("After a gesture completes, within this time window, no new gestures will be triggered.")]
        [SerializeField]
        private float debounceTimeWindow;
        /// <summary>
        /// After a gesture completes, within this time window (in seconds), no
        /// new gestures will be triggered.  Should be less than <see cref="TapTimeThreshold"/>.
        /// </summary>
        public float DebounceTimeWindow
        {
            get => debounceTimeWindow;
            set
            {
                debounceTimeWindow = value;
            }
        }

        [Tooltip("Wait for this many seconds before fixing the interactable that would recieve the events.")]
        [SerializeField]
        private float gestureCommitDelay;
        /// <summary>
        /// Wait for this many seconds before fixing the interactable that would recieve the events.
        /// During this time window, the gesture state would be <see cref="HPUIGestureState.PreCommit"/>.
        /// </summary>
        public float GestureCommitDelay
        {
            get => gestureCommitDelay;
            set
            {
                gestureCommitDelay = value;
            }
        }

        [Tooltip("The ratio at which the current tracking interactable should be recomputed. The higher the value, the easier it is to switch")]
        [Range(0f, 1f), SerializeField] private float switchCurrentTrackingInteractableThreshold = 0.15f;

        /// <summary>
        /// The ratio at which the current tracking interactable should be recomputed.
        /// The higher the value, the easier it is to switch
        /// </summary>
        public float SwitchCurrentTrackingInteractableThreshold
        {
            get => switchCurrentTrackingInteractableThreshold;
            set
            {
                switchCurrentTrackingInteractableThreshold = value;
            }
        }

        private float startTime, cumulativeDistance, timeDelta, currentTrackingInteractableHeuristic, debounceStartTime;
        private Vector2 delta, currentPosition, previousPosition, cumulativeDirection;
        private bool selectionHappenedLastFrame = false;
        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();
        private Dictionary<IHPUIInteractable, Vector2> cachedPositionsOnInteractable = new Dictionary<IHPUIInteractable, Vector2>();
        private LogicState interactorGestureState = LogicState.NoGesture;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public HPUIGestureLogic()
        { }

        /// <summary>
        /// Initializes a new instance of the with the threshold values.
        /// </summary>
        public HPUIGestureLogic(float debounceTimeWindow, float gestureCommitDelay)
        {
            UpdateThresholds(debounceTimeWindow, gestureCommitDelay);
        }

        /// <summary>
        /// Update the threshold values used.
        /// </summary>
        public void UpdateThresholds(float debounceTimeWindow, float gestureCommitDelay)
        {
            this.debounceTimeWindow = debounceTimeWindow;
            this.gestureCommitDelay = gestureCommitDelay;
        }

        /// <inheritdoc />
        public HPUIGestureEventArgs ComputeInteraction(IHPUIInteractor interactor, IDictionary<IHPUIInteractable, HPUIInteractionInfo> distances, out IHPUIInteractable priorityInteractable)
        {
            HPUIGestureEventArgs gestureEventArgs = null;
            bool updateTrackingInteractable = false;
            bool selectionHappening = false;
            bool success;
            float frameTime = Time.time;
            // Default value
            priorityInteractable = activePriorityInteractable;
            cachedPositionsOnInteractable.Clear();

            // Phase 1: Update/add interactables present this frame
            foreach (KeyValuePair<IHPUIInteractable, HPUIInteractionInfo> kvp in distances)
            {
                IHPUIInteractable interactable = kvp.Key;
                HPUIInteractionInfo interactionInfo = kvp.Value;

                if (!trackingInteractables.TryGetValue(interactable, out HPUIInteractionState state))
                {
                    state = new HPUIInteractionState();
                    trackingInteractables.Add(interactable, state);
                }
                // Target entered hover state
                if (!state.Active)
                {
                    state.SetActive();

                    if (currentTrackingInteractable != null)
                    {
                        updateTrackingInteractable = true;
                    }
                }

                if (interactionInfo.heuristic < state.LowestHeuristicValue)
                {
                    state.LowestHeuristicValue = interactionInfo.heuristic;
                }

                if (currentTrackingInteractable == interactable)
                {
                    currentTrackingInteractableHeuristic = interactionInfo.heuristic;
                }

                state.CurrentHeuristicValue = interactionInfo.heuristic;

                if (interactionInfo.isSelection)
                {
                    selectionHappening = true;

                    if (interactorGestureState == LogicState.NoGesture)
                    {
                        startTime = frameTime;
                        interactorGestureState = LogicState.AwaitingCommit;
                        updateTrackingInteractable = true;
                        // Forcing the current interactable to be reset.
                        currentTrackingInteractable = null;
                    }

                    if (interactorGestureState == LogicState.AwaitingCommit)
                    {
                        state.SetSelectable();

                        state.StartTime = frameTime;
                        success = interactable.ComputeInteractorPosition(interactor, out Vector2 startPosition);
                        state.StartPosition = startPosition;
                        if (!success)
                        {
                            return ErrorReset("IsSelection but no position on interactable - something went wrong. Resetting to recover.",
                                              interactor);
                        }
                        cachedPositionsOnInteractable.Add(interactable, startPosition);
                    }
                }
            }

            // Phase 2: Mark those that exited hover as not active and compute potential tracking/active interactable
            IHPUIInteractable interactableToTrack = null;
            HPUIInteractionState interactableToTrackState = null;
            float bestHeuristicForTracking = float.PositiveInfinity;

            IHPUIInteractable interactableToBeActive = null;
            int bestZOrder = int.MaxValue;
            float bestHeuristicForActive = float.PositiveInfinity;

            foreach (KeyValuePair<IHPUIInteractable, HPUIInteractionState> kvp in trackingInteractables)
            {
                IHPUIInteractable interactable = kvp.Key;
                HPUIInteractionState state = kvp.Value;

                if (!distances.ContainsKey(kvp.Key) && state.Active)
                {
                    state.SetNotActive(frameTime);
                    if (interactable == currentTrackingInteractable)
                    {
                        updateTrackingInteractable = true;
                    }
                }

                // Determine tracking interactable
                if (state.Active && state.CurrentHeuristicValue < bestHeuristicForTracking)
                {
                    bestHeuristicForTracking = state.CurrentHeuristicValue;
                    interactableToTrack = interactable;
                    interactableToTrackState = state;
                }

                // Determine active interactable
                // Targets not selected within the priority window (see
                // gestureCommitDelay), will not get any events.  For targets selected
                // withing the window, first prioritize the zOrder, then the
                // HeuristicValue.
                if (interactable.HandlesGesture() && state.SelectableTarget) // or state.SelectableInPrevFrames
                {
                    int z = interactable.zOrder;
                    float lowestHeuristicValue = state.LowestHeuristicValue;

                    // Choose lower zOrder first; if equal, choose lower heuristic
                    if (interactableToBeActive == null || z < bestZOrder || (z == bestZOrder && lowestHeuristicValue < bestHeuristicForActive))
                    {
                        interactableToBeActive = interactable;
                        bestZOrder = z;
                        bestHeuristicForActive = lowestHeuristicValue;
                    }
                }
            }

            // Phase 3: Early exit conditions met? Setup events and exit.
            // Selection exited
            if (!selectionHappening)
            {
                if (selectionHappenedLastFrame)
                {
                    if (debounceStartTime + debounceTimeWindow < frameTime &&
                        // Start was never fired. The gesture ended just on the threshold. Hence cancel!
                        interactorGestureState == LogicState.Gesturing)
                    {
                        gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Stopped);

                        // We update this only if it was a valid gesture/tap
                        debounceStartTime = frameTime;
                    }
                    // If a gesture had started within the debounce window, trigger a cancel event
                    else
                    {
                        gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Canceled);
                    }
                    selectionHappenedLastFrame = false;
                    Reset();
                    return gestureEventArgs;
                }
                else
                {
                    return null;
                }
            }

            // Phase 4: Ensure correct interactable is used for tracking and active
            if (interactableToTrack == null)
            {
                return ErrorReset("Early exit condition should have been fired! Resetting and returning null",
                                  interactor);
            }

            if (interactableToTrack != currentTrackingInteractable)
            {
                float heuristicRatio = Mathf.Infinity;
                if (currentTrackingInteractable != null)
                {
                    heuristicRatio = (interactableToTrackState.CurrentHeuristicValue /
                                      (currentTrackingInteractableHeuristic + 1e-6f)); // Avoiding dividing by zero
                }
                else
                {
                    updateTrackingInteractable = true;
                }

                if (!updateTrackingInteractable && heuristicRatio < switchCurrentTrackingInteractableThreshold)
                {
                    updateTrackingInteractable = true;
                }
            }

            // Phase 5: Compute parametrs and setup events appropriately
            bool resetPositionDeltaComputation = interactableToTrack != currentTrackingInteractable;
            if (updateTrackingInteractable)
            {
                currentTrackingInteractableHeuristic = interactableToTrackState.CurrentHeuristicValue;
                currentTrackingInteractable = interactableToTrack;
            }

            if (currentTrackingInteractable == null)
            {
                return ErrorReset("Current tracking interactable was null. Resetting to recover.",
                                  interactor);
            }

            if (!cachedPositionsOnInteractable.TryGetValue(currentTrackingInteractable, out currentPosition))
            {
                success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out currentPosition);

                if (!success)
                {
                    return ErrorReset("Current tracking interactable was not hovered by interactor! Resetting to recover.",
                                      interactor);
                }
                cachedPositionsOnInteractable.Add(currentTrackingInteractable, currentPosition);
            }

            if (resetPositionDeltaComputation)
            {
                previousPosition = currentPosition; // start computation from new point
            }

            timeDelta = frameTime - startTime;

            if (timeDelta < GestureCommitDelay)
            {
                if (interactableToBeActive != null && interactableToBeActive != activePriorityInteractable)
                {
                    if (!cachedPositionsOnInteractable.TryGetValue(interactableToBeActive, out Vector2 newCurrentPosition))
                    {
                        if (!interactableToBeActive.ComputeInteractorPosition(interactor, out newCurrentPosition))
                        {
                            return ErrorReset("Priority interactable chosen dones't have interaction point! Resetting to recover.",
                                              interactor);
                        }
                    }
                    currentPosition = newCurrentPosition;
                    currentTrackingInteractable = activePriorityInteractable = interactableToBeActive;
                }
                else if (interactableToBeActive == null)
                {
                    // activePriorityInteractable can be null. A gesture happened, but no one there to recieve it.
                    activePriorityInteractable = null;
                }
            }

            // Updating parameters before firing event
            delta = currentPosition - previousPosition;
            cumulativeDistance += delta.magnitude;
            cumulativeDirection += delta;
            priorityInteractable = activePriorityInteractable;

            if (timeDelta < GestureCommitDelay)
            {
                gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.CommitPending);
            }
            else if (interactorGestureState == LogicState.AwaitingCommit)
            {
                interactorGestureState = LogicState.Gesturing;
                gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Started);
            }
            else
            {
                gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Updated);
            }

            // Updating parameters after firing event
            selectionHappenedLastFrame = selectionHappening;
            previousPosition = currentPosition;

            return gestureEventArgs;
        }

        /// <inheritdoc />
        public void Reset()
        {
            debounceStartTime = Time.time;
            interactorGestureState = LogicState.NoGesture;
            trackingInteractables.Clear();
            activePriorityInteractable = null;
            currentTrackingInteractable = null;
            cumulativeDistance = 0;
            cumulativeDirection = Vector2.zero;
        }

        /// <summary>
        /// Error log the message and rest the logic state. Depending on the logicState, return the appropriate gestureEventArgs. Also sets the selectionHappenedLastFrame to false.
        /// </summary>
        protected HPUIGestureEventArgs ErrorReset(string message, IHPUIInteractor interactor)
        {
            Debug.LogError(message);
            HPUIGestureEventArgs gestureEventArgs;
            if (interactorGestureState != LogicState.NoGesture)
            {
                gestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Canceled);
            }
            else
            {
                // FIXME: This ever happens?
                gestureEventArgs = null;
            }
            selectionHappenedLastFrame = false;
            Reset();
            return gestureEventArgs;
        }

        protected HPUIGestureEventArgs PopulateGestureEventArgs(IHPUIInteractor interactor, HPUIGestureState gestureState)
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
            return new HPUIGestureEventArgs(interactor, activePriorityInteractable,
                                            gestureState, timeDelta, state.StartTime, state.StartPosition,
                                            cumulativeDirection, cumulativeDistance, delta,
                                            currentTrackingInteractable, currentPosition);
        }

        /// <summary>
        /// Clear cached objects.
        /// </summary>
        public void Dispose()
        {
            Reset();
        }

        class HPUIInteractionState
        {
            public static HPUIInteractionState empty = new HPUIInteractionState();

            public float LowestHeuristicValue { get; set; }
            public float CurrentHeuristicValue { get; set; }
            public Vector2 StartPosition { get; set; }
            public float StartTime { get; set; }
            public bool Active { get; private set; }
            public bool SelectableTarget { get; private set; }
            public bool SelectableInPrevFrames { get; private set; }

            public HPUIInteractionState()
            {
                this.StartTime = 0;
                this.StartPosition = Vector2.zero;
                this.Active = true;
                this.SelectableTarget = false;
                this.LowestHeuristicValue = float.MaxValue;
                this.CurrentHeuristicValue = 0;
            }

            public void SetSelectable()
            {
                SelectableTarget = true;
                SelectableInPrevFrames = true;
            }

            public void SetActive()
            {
                Active = true;
            }

            public void SetNotActive(float frameTime)
            {
                SelectableTarget = false;
                Active = false;
            }
        }
    }
}
