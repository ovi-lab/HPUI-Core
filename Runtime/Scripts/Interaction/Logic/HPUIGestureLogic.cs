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
        /// new gestures will be triggered.
        /// </summary>
        public float DebounceTimeWindow
        {
            get => debounceTimeWindow;
            set
            {
                debounceTimeWindow = value;
            }
        }

        [Tooltip("Wait for this many seconds before fixing the interactable that would receive the events.")]
        [SerializeField]
        private float gestureCommitDelay;
        /// <summary>
        /// Wait for this many seconds before fixing the interactable that would receive the events.
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

        [SerializeField, Tooltip("If enabled, will always report position in interactable state events. Otherwise, position is reported only during Tracking related events.")]
        private bool alwaysReportPositionInStateEvents = false;

        /// <summary>
        /// If enabled, will always report position in interactable state events. Otherwise, position is reported only during Tracking related events.
        /// The position data when this is disabled will be Vector2.zero.
        /// </summary>
        public bool AlwaysReportPositionInStateEvents { get => alwaysReportPositionInStateEvents; set => alwaysReportPositionInStateEvents = value; }

        private float startTime, cumulativeDistance, timeDelta, currentTrackingInteractableHeuristic, debounceStartTime;
        private Vector2 delta, currentPosition, previousPosition, cumulativeDirection;
        private bool selectionHappenedLastFrame = false;
        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new();
        private Dictionary<IHPUIInteractable, Vector2> cachedPositionsOnInteractable = new();
        private Dictionary<IHPUIInteractable, HPUIGestureState> gestureEventStates = new();
        private Dictionary<IHPUIInteractable, HPUIInteractableState> interactableEventStates = new();
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
        public HPUIInteractorGestureEventArgs ComputeInteraction(IHPUIInteractor interactor,
                                                                 IDictionary<IHPUIInteractable, HPUIInteractionInfo> distances,
                                                                 IDictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents,
                                                                 IDictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents)
        {
            HPUIInteractorGestureEventArgs interactorGestureEventArgs = null;
            bool updateTrackingInteractable = false;
            bool selectionHappening = false;
            bool success;
            float frameTime = Time.time;

            cachedPositionsOnInteractable.Clear();

            // TODO: Revise to use pools
            gestureEventStates.Clear();
            interactableEventStates.Clear();

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
                                              interactor, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
                        }
                        cachedPositionsOnInteractable.Add(interactable, startPosition);
                    }
                }

                HPUIInteractableState auxState;
                if (currentTrackingInteractable == interactable)
                {
                    auxState = HPUIInteractableState.TrackingUpdate;
                }
                else if (interactionInfo.isSelection)
                {
                    auxState = HPUIInteractableState.InContact;
                }
                else
                {
                    auxState = HPUIInteractableState.Hovered;
                }

                interactableEventStates.Add(interactable, auxState);
            }

            // Phase 2: Mark those that exited hover as not active and compute potential tracking/active interactable
            IHPUIInteractable interactableToTrack = null;
            HPUIInteractionState interactableToTrackState = null;
            float bestHeuristicForTracking = float.PositiveInfinity;

            IHPUIInteractable interactableToBeActive = null;
            int bestZOrder = int.MaxValue;
            float bestHeuristicForActive = float.PositiveInfinity;

            timeDelta = frameTime - startTime;
            bool inPreCommitWindow = timeDelta < GestureCommitDelay;
            bool stateIsAwaitCommit = interactorGestureState == LogicState.AwaitingCommit;

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

                // Has to only happens once per gesture, guarding to avoid unnecessary work
                if (!inPreCommitWindow && stateIsAwaitCommit)
                {
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
            }

            // before this active interactable should be null!
            // NOTE: activePriorityInteractable can end up being null. A gesture happened, but no one there to recieve it.
            if (interactableToBeActive != null)
            {
                updateTrackingInteractable = true;
                // The interactable to track should be the active interactable
                interactableToTrack = activePriorityInteractable = interactableToBeActive;
            }

            // Phase 3: Early exit conditions met? Setup events and exit.
            // Selection exited
            if (!selectionHappening)
            {
                if (selectionHappenedLastFrame)
                {
                    if (debounceStartTime + debounceTimeWindow < frameTime &&
                        // If not gesturing, start was never fired. The gesture had ended just on the threshold. Hence that should result in cancel!
                        interactorGestureState == LogicState.Gesturing)
                    {
                        interactorGestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Stopped, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);

                        // We update this only if it was a valid gesture
                        debounceStartTime = frameTime;
                    }
                    // If a gesture had started within the debounce window, trigger a cancel event
                    else
                    {
                        interactorGestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Canceled, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
                    }
                    selectionHappenedLastFrame = false;
                    Reset();
                    return interactorGestureEventArgs;
                }
                else if (interactableEventStates.Count > 0)
                {
                    return PopulateGestureEventArgs(interactor, HPUIGestureState.None, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
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
                                  interactor, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }

            if (interactableToTrack != currentTrackingInteractable)
            {
                float heuristicRatio = float.PositiveInfinity;
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


            // Phase 5: Compute tracking interactable if applicable
            bool resetTrackingInteractable = interactableToTrack != currentTrackingInteractable;
            if (resetTrackingInteractable && updateTrackingInteractable)
            {
                interactableEventStates[interactableToTrack] = HPUIInteractableState.TrackingStarted;
                if (currentTrackingInteractable != null)
                {
                    interactableEventStates[currentTrackingInteractable] = HPUIInteractableState.TrackingEnded;
                }

                currentTrackingInteractableHeuristic = interactableToTrackState.CurrentHeuristicValue;
                currentTrackingInteractable = interactableToTrack;
            }

            if (currentTrackingInteractable == null)
            {
                return ErrorReset("Current tracking interactable was null. Resetting to recover.",
                                  interactor, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }

            if (!cachedPositionsOnInteractable.TryGetValue(currentTrackingInteractable, out currentPosition))
            {
                success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out currentPosition);

                if (!success)
                {
                    return ErrorReset("Current tracking interactable was not hovered by interactor! Resetting to recover.",
                                      interactor, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
                }
                cachedPositionsOnInteractable.Add(currentTrackingInteractable, currentPosition);
            }

            if (resetTrackingInteractable)
            {
                previousPosition = currentPosition; // start computation from new point
            }

            // Phase 6: Update parameters and fire events
            delta = currentPosition - previousPosition;
            cumulativeDistance += delta.magnitude;
            cumulativeDirection += delta;

            if (inPreCommitWindow)
            {
                interactorGestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.None, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }
            else if (stateIsAwaitCommit)
            {
                interactorGestureState = LogicState.Gesturing;
                interactorGestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Started, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }
            else
            {
                interactorGestureEventArgs = PopulateGestureEventArgs(interactor, HPUIGestureState.Updated, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }

            // Updating parameters after firing event
            selectionHappenedLastFrame = selectionHappening;
            previousPosition = currentPosition;

            return interactorGestureEventArgs;
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
        /// Error log the message and reset the logic state. Depending on the logicState, return the appropriate gestureEventArgs. Also sets the selectionHappenedLastFrame to false.
        /// </summary>
        protected HPUIInteractorGestureEventArgs ErrorReset(string message, IHPUIInteractor interactor,
                                                            IDictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents,
                                                            IDictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents,
                                                            IDictionary<IHPUIInteractable, HPUIGestureState> gestureEventStates,
                                                            IDictionary<IHPUIInteractable, HPUIInteractableState> interactableEventStates)

        {
            Debug.LogError(message);
            HPUIInteractorGestureEventArgs interactorGestureEventArgs;
            if (interactorGestureState != LogicState.NoGesture)
            {
                interactorGestureEventArgs = PopulateGestureEventArgs(interactor,
                                                                      HPUIGestureState.Canceled,
                                                                      gestureEvents, interactableEvents,
                                                                      gestureEventStates, interactableEventStates,
                                                                      isError: true);
            }
            else
            {
                // FIXME: This ever happens?
                interactorGestureEventArgs = null;
            }
            selectionHappenedLastFrame = false;
            Reset();
            return interactorGestureEventArgs;
        }

        protected HPUIInteractorGestureEventArgs PopulateGestureEventArgs(IHPUIInteractor interactor,
                                                                          HPUIGestureState gestureState, // This logic handler only fires one gesture event per frame
                                                                          IDictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents,
                                                                          IDictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents,
                                                                          IDictionary<IHPUIInteractable, HPUIGestureState> gestureEventStates,
                                                                          IDictionary<IHPUIInteractable, HPUIInteractableState> interactableEventStates,
                                                                          bool isError = false)
        {
            HPUIInteractionState state;
            if (activePriorityInteractable != null)
            {
                state = trackingInteractables[activePriorityInteractable];
                if (interactorGestureState == LogicState.Gesturing)
                {
                    gestureEventStates[activePriorityInteractable] = gestureState;
                    gestureEvents[activePriorityInteractable] = new HPUIGestureEventArgs(interactor, activePriorityInteractable, gestureState,
                                                                                              timeDelta, state.StartTime, state.StartPosition,
                                                                                              cumulativeDirection, cumulativeDistance, delta);
                }
            }
            else
            {
                state = HPUIInteractionState.empty;
            }

            bool errorReset = false;

            foreach ((IHPUIInteractable interactable, HPUIInteractableState auxState) in interactableEventStates)
            {
                Vector2 auxPosition;

                if (alwaysReportPositionInStateEvents || auxState == HPUIInteractableState.TrackingUpdate || auxState == HPUIInteractableState.TrackingStarted || auxState == HPUIInteractableState.TrackingEnded)
                {
                    if (!cachedPositionsOnInteractable.TryGetValue(interactable, out auxPosition))
                    {
                        bool success = interactable.ComputeInteractorPosition(interactor, out auxPosition);
                        if (!success)
                        {
                            auxPosition = Vector2.zero;
                            if (!isError)
                            {
                                errorReset = true;
                            }
                        }
                        else
                        {
                            cachedPositionsOnInteractable.Add(interactable, auxPosition);
                        }
                    }
                }
                else
                {
                    auxPosition = Vector2.zero;
                }

                interactableEvents[interactable] = new HPUIInteractableStateEventArgs(interactor, interactable, auxPosition, auxState);
            }

            if (errorReset)
            {
                return ErrorReset("IsSelection but no position on interactable - something went wrong. Resetting to recover.",
                                  interactor, gestureEvents, interactableEvents, gestureEventStates, interactableEventStates);
            }

            return new HPUIInteractorGestureEventArgs(interactor, gestureState,
                                                      (IReadOnlyDictionary<IHPUIInteractable, HPUIGestureState>)gestureEventStates,
                                                      (IReadOnlyDictionary<IHPUIInteractable, HPUIInteractableState>)interactableEventStates,
                                                      timeDelta, state.StartTime, state.StartPosition,
                                                      cumulativeDirection, cumulativeDistance, delta);
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
