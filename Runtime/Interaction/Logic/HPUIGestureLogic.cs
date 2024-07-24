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
        private Vector2 delta, currentPosition, previousPosition, cumilativeDirection;
        private bool selectionHappenedLastFrame = false,
            useHuristic = false;
        private int activeInteractables = 0;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> trackingInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGesture interactorGestureState = HPUIGesture.None;

        /// <summary>
        /// Initializes a new instance of the with the thrshold values.
        /// </summary>
        public HPUIGestureLogic(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold, float interactionSelectionRadius, bool useHuristic)
        {
            this.interactor = interactor;
            this.useHuristic = useHuristic;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.interactionSelectionRadius = interactionSelectionRadius;
            Reset();
        }

        /// <inheritdoc />
        public void Update(IDictionary<IHPUIInteractable, HPUIInteractionData> distances)
        {
            bool updateTrackingInteractable = false;
            bool selectionHappening = false;
            foreach(IHPUIInteractable interactable in distances.Keys.Union(trackingInteractables.Keys))
            {
                bool isTracked = trackingInteractables.TryGetValue(interactable, out HPUIInteractionState state);
                bool isInFrame = distances.TryGetValue(interactable, out HPUIInteractionData interactionData);

                // Target entered hover state
                if (!isTracked || !state.active)
                {
                    if (isTracked)
                    {
                        state.active = true;
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
                    if (interactionData.distance < state.minDistanceToInteractor)
                    {
                        state.minDistanceToInteractor = interactionData.distance;
                    }

                    if (interactionData.huristic < state.huristic)
                    {
                        state.huristic = interactionData.huristic;
                    }

                    if (interactionData.distance < interactionSelectionRadius)
                    {
                        selectionHappening = true;

                        if (interactorGestureState == HPUIGesture.None)
                        {
                            startTime = Time.time;
                            interactorGestureState = HPUIGesture.Tap;
                            updateTrackingInteractable = true;
                        }

                        // Selectable only if within the tapTimeThreshold.
                        if (interactorGestureState == HPUIGesture.Tap)
                        {
                            state.selectableTarget = true;
                        }

                        state.startTime = Time.time;
                        state.startPosition = interactable.ComputeInteractorPosition(interactor);
                    }
                }
                // Target exited hover state
                else
                {
                    state.active = false;
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
                Debug.Log($"-- params: cumm. dist: {cumilativeDistance}, time delta: {timeDelta}");
                try
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
                finally
                {
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
            {
                // TODO: revisit this assumption
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
                    previousPosition = currentTrackingInteractable.ComputeInteractorPosition(interactor);
                    return;
                }
            }

            currentPosition = currentTrackingInteractable.ComputeInteractorPosition(interactor);
            delta = previousPosition - currentPosition;
            timeDelta = Time.time - startTime;
            cumilativeDistance += delta.magnitude;
            cumilativeDirection += delta;

            // NOTE: In all codepaths, the event calls are the last thing.
            // Hence, propergating the exception should not break internal states.
            try
            {
                switch (interactorGestureState)
                {
                    case HPUIGesture.Tap:
                        if (timeDelta > tapTimeThreshold || cumilativeDistance > tapDistanceThreshold)
                        {
                            interactorGestureState = HPUIGesture.Gesture;
                            ComputeActivePriorityInteractable();
                            TriggerGestureEvent(HPUIGestureState.Started);
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

        // NOTE: This gets called only within the tapdistancethreshold window.
        // Thus using distance as opposed to start time to pick the target that is the most ideal.
        protected void ComputeActivePriorityInteractable()
        {
            // Targets not selected within the priority window
            // (defaults to tapdistancethreshold), will not get any
            // events.  For targets selected withing the window, first
            // prioritize the zOrder, then the distance.
            IHPUIInteractable interactableToBeActive = trackingInteractables
                .Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) && kvp.Value.selectableTarget)
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => useHuristic? kvp.Value.huristic : kvp.Value.minDistanceToInteractor)
                .FirstOrDefault().Key;

            if (interactableToBeActive != activePriorityInteractable)
            {
                activePriorityInteractable = interactableToBeActive;
            }
        }

        protected void Reset()
        {
            interactorGestureState = HPUIGesture.None;
            trackingInteractables.Clear();
            activePriorityInteractable = null;
            currentTrackingInteractable = null;
            cumilativeDistance = 0;
            cumilativeDirection = Vector2.zero;
        }

        protected void TriggerTapEvent()
        {
            using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
            {
                ComputeActivePriorityInteractable();
                HPUIInteractionState state;
                if (activePriorityInteractable != null)
                {
                    state = trackingInteractables[activePriorityInteractable];
                }
                else
                {
                    state = HPUIInteractionState.empty;
                }

                tapEventArgs.SetParams(interactor, activePriorityInteractable, state.startPosition + cumilativeDirection);

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
                                           gestureState, timeDelta, state.startTime, state.startPosition,
                                           cumilativeDirection, cumilativeDistance, delta,
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
            public float startTime;
            public Vector2 startPosition;
            public bool active,
                selectableTarget;
            public float minDistanceToInteractor;
            public float huristic;

            public HPUIInteractionState()
            {
                this.startTime = 0;
                this.startPosition = Vector2.zero;
                this.active = true;
                this.selectableTarget = false;
                this.minDistanceToInteractor = float.MaxValue;
                this.huristic = float.MaxValue;
            }
        }
    }

}
