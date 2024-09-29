using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Encapsulates the logic for HPUI gesture interactions.
    /// The interactor dictates which interactable gets the gesture.
    /// </summary>
    [Serializable]
    public class HPUIGestureLogic: IHPUIGestureLogic
    {
        [Tooltip("The time threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapTimeThreshold;
        /// <summary>
        /// The time threshold (in seconds) at which an interaction would be
        /// treated as a gesture.  That is, if the interactor is in contact with
        /// an interactable for more than this threshold, it would be treated as a
        /// gesture.
        /// </summary>
        public float TapTimeThreshold
        {
            get => tapTimeThreshold;
            set
            {
                tapTimeThreshold = value;
            }
        }

        [Tooltip("The distance threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapDistanceThreshold;
        /// <summary>
        /// The distance threshold (in Unity units) at which an interaction would
        /// be treated as a gesture.  That is, if the interactor has moved more
        /// than this value after coming into contact with an interactable, it
        /// would be treated as a gesture.
        /// </summary>
        public float TapDistanceThreshold
        {
            get => tapDistanceThreshold;
            set
            {
                tapDistanceThreshold = value;
            }
        }

        [Tooltip("After a gesture completes, within this time window, not new gestures will be triggered.")]
        [SerializeField]
        private float debounceTimeWindow;
        /// <summary>
        /// After a gesture completes, within this time window (in seconds), not
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

        private float startTime, cumulativeDistance, timeDelta, currentTrackingInteractableHeuristic, debounceStartTime;
        private Vector2 delta, currentPosition, previousPosition, cumulativeDirection;
        private int activeInteractables = 0;
        private bool success,
            selectionHappenedLastFrame = false;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGesture interactorGestureState = HPUIGesture.None;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public HPUIGestureLogic()
        {}

        /// <summary>
        /// Initializes a new instance of the with the threshold values.
        /// </summary>
        public HPUIGestureLogic(float tapTimeThreshold, float tapDistanceThreshold, float debounceTimeWindow)
        {
            UpdateThresholds(tapTimeThreshold, tapDistanceThreshold, debounceTimeWindow);
        }

        /// <summary>
        /// Update the threshold values used.
        /// </summary>
        public void UpdateThresholds(float tapTimeThreshold, float tapDistanceThreshold, float debounceTimeWindow)
        {
            if (tapTimeThreshold < debounceTimeWindow)
            {
                throw new ArgumentException($"tapTimeThreshold({tapTimeThreshold}) cannot be smaller than the debounceTimeWindow ({debounceTimeWindow})");
            }
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.debounceTimeWindow = debounceTimeWindow;
        }

        /// <inheritdoc />
        public void ComputeInteraction(IHPUIInteractor interactor, IDictionary<IHPUIInteractable, HPUIInteractionInfo> distances, out HPUIGesture gesture, out IHPUIInteractable priorityInteractable, HPUITapEventArgs tapArgsToPopulate, HPUIGestureEventArgs gestureArgsToPopulate)
        {
            bool updateTrackingInteractable = false;
            bool selectionHappening = false;
            float frameTime = Time.time;
            // Default values
            gesture = HPUIGesture.None;
            priorityInteractable = activePriorityInteractable;

            foreach(IHPUIInteractable interactable in distances.Keys.Union(trackingInteractables.Keys))
            {
                bool isTracked = trackingInteractables.TryGetValue(interactable, out HPUIInteractionState state);
                bool isInFrame = distances.TryGetValue(interactable, out HPUIInteractionInfo interactionData);

                // Target entered hover state
                if (!isTracked || !state.Active)
                {
                    if (isTracked)
                    {
                        state.SetActive();
                    }
                    else
                    {
                        state = new HPUIInteractionState();
                        trackingInteractables.Add(interactable, state);
                    }

                    activeInteractables++;
                    updateTrackingInteractable = true;
                }

                if (isInFrame)
                {
                    if (interactionData.heuristic < state.Heuristic)
                    {
                        state.Heuristic = interactionData.heuristic;
                    }

                    if (!updateTrackingInteractable && currentTrackingInteractable != interactable && interactionData.heuristic < currentTrackingInteractableHeuristic)
                    {
                        updateTrackingInteractable = true;
                    }

                    if (interactionData.isSelection)
                    {
                        selectionHappening = true;

                        if (interactorGestureState == HPUIGesture.None)
                        {
                            startTime = frameTime;
                            interactorGestureState = HPUIGesture.Tap;
                            updateTrackingInteractable = true;
                            // Forcing the current interactable to be reset.
                            currentTrackingInteractable = null;
                        }

                        // Selectable only if within the tapTimeThreshold.
                        if (interactorGestureState == HPUIGesture.Tap)
                        {
                            state.SetSelectable();

                            state.StartTime = frameTime;
                            success = interactable.ComputeInteractorPosition(interactor, out Vector2 startPosition);
                            state.StartPosition = startPosition;
                            Debug.Assert(success, $"Current tracking interactable ({interactable.transform?.name}) was not hoverd by interactor  {interactor.transform?.name}");
                        }
                    }
                }
                // Target exited hover state
                else
                {
                    state.SetNotActive(frameTime);
                    activeInteractables--;
                    updateTrackingInteractable = true;
                }
            }

            // Selection exited
            // NOTE: If a gesture happened just on the threshold of
            // the tap (i.e., time/distance just went over threshold
            // and selected existed) it will be treated as a tap.
            if (selectionHappenedLastFrame && !selectionHappening)
            {
                selectionHappenedLastFrame = false;
                if (debounceStartTime + debounceTimeWindow < frameTime)
                {
                    switch (interactorGestureState)
                    {
                        case HPUIGesture.Tap:
                            PopulateTapEventArgs(interactor, tapArgsToPopulate);
                            gesture = HPUIGesture.Tap;
                            break;
                        case HPUIGesture.Gesture:
                            PopulateGestureEventArgs(interactor, HPUIGestureState.Stopped, gestureArgsToPopulate);
                            gesture = HPUIGesture.Gesture;
                            break;
                    }
                }
                debounceStartTime = frameTime;
                priorityInteractable = activePriorityInteractable;
                Reset();
                return;
            }
            selectionHappenedLastFrame = selectionHappening;

            if (interactorGestureState == HPUIGesture.None)
            {
                return;
            }

            if (updateTrackingInteractable)
            {
                KeyValuePair<IHPUIInteractable, HPUIInteractionState> interactableDataToTrack = trackingInteractables
                    .Where(kvp => kvp.Value.Active)
                    .OrderBy(kvp => kvp.Value.Heuristic)
                    .First();

                if (interactableDataToTrack.Key != currentTrackingInteractable)
                {
                    currentTrackingInteractableHeuristic = interactableDataToTrack.Value.Heuristic;
                    currentTrackingInteractable = interactableDataToTrack.Key;
                    success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out previousPosition);
                    Debug.Assert(success, $"Current tracking interactable was not hoverd by interactor  {interactor.transform.name}");
                }
            }

            success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out currentPosition);
            Debug.Assert(success, $"Current tracking interactable was not hoverd by interactor  {interactor.transform.name}");
            delta = currentPosition - previousPosition;
            timeDelta = frameTime - startTime;
            cumulativeDistance += delta.magnitude;
            cumulativeDirection += delta;

            switch (interactorGestureState)
            {
                case HPUIGesture.Tap:
                    if (timeDelta > tapTimeThreshold || cumulativeDistance > tapDistanceThreshold)
                    {
                        interactorGestureState = HPUIGesture.Gesture;
                        ComputeActivePriorityInteractable(interactor, false);
                        PopulateGestureEventArgs(interactor, HPUIGestureState.Started, gestureArgsToPopulate);
                        gesture = HPUIGesture.Gesture;
                    }
                    break;
                case HPUIGesture.Gesture:
                    PopulateGestureEventArgs(interactor, HPUIGestureState.Updated, gestureArgsToPopulate);
                    gesture = HPUIGesture.Gesture;
                    break;
                default:
                    throw new InvalidOperationException("Unknown gesture.");
            }

            priorityInteractable = activePriorityInteractable;
            previousPosition = currentPosition;
        }

        /// <inheritdoc />
        public void Reset()
        {
            interactorGestureState = HPUIGesture.None;
            trackingInteractables.Clear();
            activePriorityInteractable = null;
            currentTrackingInteractable = null;
            cumulativeDistance = 0;
            cumulativeDirection = Vector2.zero;
        }

        // NOTE: This gets called only within the tapDistanceThreshold window.
        // Thus using distance as opposed to start time to pick the target that is the most ideal.
        protected void ComputeActivePriorityInteractable(IHPUIInteractor interactor, bool usePreviousSelectableState)
        {
            // Targets not selected within the priority window
            // (defaults to tapDistanceThreshold), will not get any
            // events.  For targets selected withing the window, first
            // prioritize the zOrder, then the distance.
            IHPUIInteractable interactableToBeActive = trackingInteractables
                .Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) &&
                       (usePreviousSelectableState ? kvp.Value.SelectableInPrevFrames: kvp.Value.SelectableTarget))
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => kvp.Value.Heuristic)
                .FirstOrDefault().Key;

            if (interactableToBeActive != activePriorityInteractable)
            {
                currentTrackingInteractable = activePriorityInteractable = interactableToBeActive;
                if (currentTrackingInteractable.ComputeInteractorPosition(interactor, out Vector2 newCurrentPosition))
                {
                    currentPosition = newCurrentPosition;
                }
            }
        }

        protected void PopulateTapEventArgs(IHPUIInteractor interactor, HPUITapEventArgs tapEventArgs)
        {
            ComputeActivePriorityInteractable(interactor, true);
            HPUIInteractionState state;
            if (activePriorityInteractable != null)
            {
                state = trackingInteractables[activePriorityInteractable];
            }
            else
            {
                state = HPUIInteractionState.empty;
            }

            tapEventArgs.SetParams(interactor, activePriorityInteractable, state.StartPosition + cumulativeDirection);
        }

        protected void PopulateGestureEventArgs(IHPUIInteractor interactor, HPUIGestureState gestureState, HPUIGestureEventArgs gestureEventArgs)
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

            public float Heuristic { get; set; }
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
                this.Heuristic = float.MaxValue;
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
