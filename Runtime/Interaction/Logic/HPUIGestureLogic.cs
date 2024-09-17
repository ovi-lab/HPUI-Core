using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Encapsulates the logic for HPUI gesture interactions.
    /// The interactor dictates which interactable gets the gesture.
    /// </summary>
    public class HPUIGestureLogic: IHPUIGestureLogic
    {
        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUIGestureEventArgs> hpuiGestureEventArgsPool = new LinkedPool<HPUIGestureEventArgs>(() => new HPUIGestureEventArgs());

        private float tapTimeThreshold, tapDistanceThreshold, debounceTimeWindow, debounceStartTime;
        private IHPUIInteractor interactor;

        private float startTime, cumulativeDistance, timeDelta, currentTrackingInteractableHeuristic;
        private Vector2 delta, currentPosition, previousPosition, cumulativeDirection;
        private bool selectionHappenedLastFrame = false,
            useHeuristic = false;
        private int activeInteractables = 0;
        private bool success;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGesture interactorGestureState = HPUIGesture.None;

        /// <summary>
        /// Initializes a new instance of the with the threshold values.
        /// </summary>
        public HPUIGestureLogic(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold, float debounceTimeWindow, bool useHeuristic)
        {
            if (tapTimeThreshold < debounceTimeWindow)
            {
                throw new ArgumentException($"tapTimeThreshold cannot be smaller than the debounceTimeWindow. Got {tapTimeThreshold} for tapTimeThreshold and {debounceTimeWindow} for debounceTimeWindow");
            }
            this.interactor = interactor;
            this.useHeuristic = useHeuristic;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.debounceTimeWindow = debounceTimeWindow;
            Reset();
        }

        /// <inheritdoc />
        public void Update(IDictionary<IHPUIInteractable, HPUIInteractionData> distances)
        {
            if (distances.Count > 0)
                Debug.Log($"=====  {string.Join(", \n", distances.Select(kvp => $"{kvp.Key.transform.name}/{kvp.Value.distance}/{kvp.Value.heuristic}/{kvp.Value.extra}/{kvp.Value.isSelection}"))}  using H:{useHeuristic} ");

            bool updateTrackingInteractable = false;
            bool selectionHappening = false;
            float frameTime = Time.time;

            foreach(IHPUIInteractable interactable in distances.Keys.Union(trackingInteractables.Keys))
            {
                bool isTracked = trackingInteractables.TryGetValue(interactable, out HPUIInteractionState state);
                bool isInFrame = distances.TryGetValue(interactable, out HPUIInteractionData interactionData);

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
                    if (interactionData.distance < state.MinDistanceToInteractor)
                    {
                        state.MinDistanceToInteractor = interactionData.distance;
                    }

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
                try
                {
                    if (debounceStartTime + debounceTimeWindow < frameTime)
                    {
                        switch (interactorGestureState)
                        {
                            case HPUIGesture.Tap:
                                TriggerTapEvent();
                                break;
                            case HPUIGesture.Gesture:
                                TriggerGestureEvent(HPUIGestureState.Stopped);
                                break;
                        }
                    }
                }
                finally
                {
                    Debug.Log($"-- params({activePriorityInteractable?.transform.name}): cumm. dist: {cumulativeDistance}, time delta: {timeDelta}   {interactorGestureState}   {tapTimeThreshold}   {debounceTimeWindow}");
                    debounceStartTime = frameTime;

                    Reset();
                }
                return;
            }
            selectionHappenedLastFrame = selectionHappening;

            if (interactorGestureState == HPUIGesture.None)
            {
                return;
            }

            if (updateTrackingInteractable)
                // (currentTrackingInteractable == null ||
                //  !(trackingInteractables.TryGetValue(currentTrackingInteractable,
                //                                      out HPUIInteractionState currentTrackingInteractableState) &&
                //    currentTrackingInteractableState.active)))
            {
                // TODO: revisit this assumption
                // Any target that is active should be ok for this
                // Giving priority to the ones that was the oldest entered
                // This minimizes the tracking interactable changing
                KeyValuePair<IHPUIInteractable, HPUIInteractionState> interactableDataToTrack = trackingInteractables
                    .Where(kvp => kvp.Value.Active)
                    .OrderBy(kvp => kvp.Value.Heuristic)
                    .First();

                if (interactableDataToTrack.Key != currentTrackingInteractable)
                {
                    currentTrackingInteractableHeuristic = interactableDataToTrack.Value.Heuristic;
                    currentTrackingInteractable = interactableDataToTrack.Key;
                    // If interactable change, we need to restart tracking, hence skipping a frame
                    success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out previousPosition);
                    Debug.Assert(success, $"Current tracking interactable was not hoverd by interactor  {interactor.transform.name}");
                    return;
                }
            }

            success = currentTrackingInteractable.ComputeInteractorPosition(interactor, out currentPosition);
            Debug.Assert(success, $"Current tracking interactable was not hoverd by interactor  {interactor.transform.name}");
            delta = currentPosition - previousPosition;
            timeDelta = frameTime - startTime;
            cumulativeDistance += delta.magnitude;
            cumulativeDirection += delta;

            // NOTE: In all code-paths, the event calls are the last thing.
            // Hence, propagating the exception should not break internal states.
            try
            {
                switch (interactorGestureState)
                {
                    case HPUIGesture.Tap:
                        if (timeDelta > tapTimeThreshold || cumulativeDistance > tapDistanceThreshold)
                        {
                            interactorGestureState = HPUIGesture.Gesture;
                            ComputeActivePriorityInteractable(false);
                            TriggerGestureEvent(HPUIGestureState.Started);
                            Debug.Log($"---- Trigger started on {activePriorityInteractable.transform.name}");
                        }
                        break;
                    case HPUIGesture.Gesture:
                        TriggerGestureEvent(HPUIGestureState.Updated);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown gesture.");
                }
            }
            finally
            {
                previousPosition = currentPosition;
            }
        }

        // NOTE: This gets called only within the tapDistanceThreshold window.
        // Thus using distance as opposed to start time to pick the target that is the most ideal.
        protected void ComputeActivePriorityInteractable(bool usePreviousSelectableState)
        {
            // Targets not selected within the priority window
            // (defaults to tapDistanceThreshold), will not get any
            // events.  For targets selected withing the window, first
            // prioritize the zOrder, then the distance.

            Debug.Log($"---- {string.Join(",\n", trackingInteractables.Select(el => $"{el.Key.transform?.name}/{ el.Key.HandlesGesture(interactorGestureState)} /{el.Value.SelectableInPrevFrames}/{el.Value.Heuristic}"))}");
            Debug.Log($"---- {string.Join(",\n", trackingInteractables.Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) && (usePreviousSelectableState ? kvp.Value.SelectableInPrevFrames : kvp.Value.SelectableTarget)).OrderBy(kvp => kvp.Key.zOrder).ThenBy(kvp => useHeuristic? kvp.Value.Heuristic : kvp.Value.MinDistanceToInteractor).Select(el => $"{el.Key.transform?.name}/{ el.Key.HandlesGesture(interactorGestureState)} /{el.Value.SelectableInPrevFrames}/{el.Value.Heuristic}"))}");

            IHPUIInteractable interactableToBeActive = trackingInteractables
                .Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) &&
                       (usePreviousSelectableState ? kvp.Value.SelectableInPrevFrames: kvp.Value.SelectableTarget))
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => useHeuristic? kvp.Value.Heuristic : kvp.Value.MinDistanceToInteractor)
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

        protected void Reset()
        {
            interactorGestureState = HPUIGesture.None;
            trackingInteractables.Clear();
            activePriorityInteractable = null;
            currentTrackingInteractable = null;
            cumulativeDistance = 0;
            cumulativeDirection = Vector2.zero;
        }

        protected void TriggerTapEvent()
        {
            using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
            {
                ComputeActivePriorityInteractable(true);
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

                try
                {
                    if (activePriorityInteractable != null)
                    {
                        activePriorityInteractable.OnTap(tapEventArgs);
                    }
                }
                finally
                {
                    // NOTE: There can be interactables that don't take any events. Even
                    // when that happens, the interactor's events should get triggered.
                    // KLUDGE: This doesn't account for the interactionSelectionRadius
                    interactor.OnTap(tapEventArgs);
                }
            }
        }

        protected void TriggerGestureEvent(HPUIGestureState gestureState)
        {
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
                                           gestureState, timeDelta, state.StartTime, state.StartPosition,
                                           cumulativeDirection, cumulativeDistance, delta,
                                           currentTrackingInteractable, currentPosition);

                try
                {
                    if (activePriorityInteractable != null)
                    {
                        activePriorityInteractable?.OnGesture(gestureEventArgs);
                    }
                }
                finally
                {
                    // NOTE: See note when tap gets triggered.
                    interactor.OnGesture(gestureEventArgs);
                }
            }
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
            public static HPUIInteractionState empty = new HPUIInteractionState();

            public float Heuristic { get; set; }
            public float MinDistanceToInteractor { get; set; }
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
                this.MinDistanceToInteractor = float.MaxValue;
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
