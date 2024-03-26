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
    public class HPUIGestureLogicUnified: IHPUIGestureLogic
    {
        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUIGestureEventArgs> hpuiGestureEventArgsPool = new LinkedPool<HPUIGestureEventArgs>(() => new HPUIGestureEventArgs());

        private float tapTimeThreshold, tapDistanceThreshold, interactionSelectionRadius;
        private IHPUIInteractor interactor;

        private float startTime, cumilativeDistance, timeDelta;
        private Vector2 delta, previousPosition, cumilativeDirection;

        private int lowestTargetZIndex = int.MaxValue;
        private int activeInteractablesCount = 0;
        private bool changedInteractable = false;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> activeInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGesture interactorGestureState = HPUIGesture.None;

        /// <summary>
        /// Initializes a new instance of the with the thrshold values.
        /// </summary>
        public HPUIGestureLogicUnified(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold, float interactionSelectionRadius)
        {
            this.interactor = interactor;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.interactionSelectionRadius = interactionSelectionRadius;
            Reset();
        }

        /// <inheritdoc />
        public void OnHoverEntering(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            if (interactorGestureState == HPUIGesture.None)
            {
                interactorGestureState = HPUIGesture.Tap;
                startTime = Time.time;
            }

            if (activeInteractables.ContainsKey(interactable))
            {
                activeInteractables[interactable].active = true;
            }
            else
            {
                float currentTime = Time.time;
                bool inValidWindow = (currentTime - startTime) < tapTimeThreshold;

                HPUIInteractionState state = new HPUIInteractionState(Time.time, interactable.ComputeInteractorPostion(interactor), inValidWindow);
                activeInteractables.Add(interactable, state);
                activeInteractablesCount += 1;

                // If a new higher priority targets is encountered within tap time window, we hand over control to that.
                if (interactable.zOrder < lowestTargetZIndex && inValidWindow)
                {
                    lowestTargetZIndex = interactable.zOrder;
                    activePriorityInteractable = interactable;

                    foreach(HPUIInteractionState interactionState in activeInteractables.Values)
                    {
                        interactionState.startTime = currentTime;
                    }
                }
            }
            ComputeCurrectTrackingInteractable();
        }

        private void ComputeCurrectTrackingInteractable()
        {
            // Any target that is active should be ok for this?
            IHPUIInteractable interactableToTrack = activeInteractables
                .Where(kvp => kvp.Value.active)
                .OrderBy(kvp => kvp.Value.startTime)
                .First().Key;

            if (interactableToTrack != currentTrackingInteractable)
            {
                currentTrackingInteractable = interactableToTrack;
                changedInteractable = true;
            }
        }

        private void ComputeActivePriorityInteractable()
        {
            // Targets not selected within the priority window
            // (defaults to tapdistancethreshold), will not get any
            // events.  For targets selected withing the window, first
            // prioritize the zOrder, then the time.
            IHPUIInteractable interactableToBeActive = activeInteractables
                .Where(kvp => kvp.Key.HandlesGesture(interactorGestureState) && kvp.Value.validTarget)
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => kvp.Value.startTime)
                .FirstOrDefault().Key;

            if (interactableToBeActive != activePriorityInteractable)
            {
                activePriorityInteractable = interactableToBeActive;
            }
        }

        private bool ActiveInteractableCanTriggerEvent()
        {
            return activePriorityInteractable != null && activeInteractables[activePriorityInteractable].minDistanceToInteractor < interactionSelectionRadius;
        }

        /// <inheritdoc />
        public void OnHoverExiting(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            if (activeInteractablesCount == 1)
            {
                HPUIInteractionState state;
                if (activePriorityInteractable != null)
                {
                    state = activeInteractables[activePriorityInteractable];
                }
                else
                {
                    state = HPUIInteractionState.empty;
                }

                switch (interactorGestureState)
                {
                    case HPUIGesture.Tap:
                        using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
                        {
                            ComputeActivePriorityInteractable();
                            tapEventArgs.SetParams(interactor, activePriorityInteractable, state.startPosition + cumilativeDirection);
                            if (ActiveInteractableCanTriggerEvent())
                            {
                                activePriorityInteractable.OnTap(tapEventArgs);
                            }
                            // NOTE: There can be interactables that don't take any events. Even
                            // when that happens, the interactor's events should get triggered.
                            // KLUDGE: This doesn't account for the interactionSelectionRadius
                            interactor.OnTap(tapEventArgs);
                        }
                        break;
                    case HPUIGesture.Gesture:
                        using (hpuiGestureEventArgsPool.Get(out HPUIGestureEventArgs gestureEventArgs))
                        {
                            gestureEventArgs.SetParams(interactor, activePriorityInteractable,
                                                       HPUIGestureState.Stopped, timeDelta, state.startTime, state.startPosition,
                                                       cumilativeDirection, cumilativeDistance, delta);
                            if (ActiveInteractableCanTriggerEvent())
                            {
                                activePriorityInteractable?.OnGesture(gestureEventArgs);
                            }
                            // NOTE: See note when tap gets triggered.
                            interactor.OnGesture(gestureEventArgs);
                        }
                        break;
                }

                Reset();
            }
            else
            {
                if (activeInteractables.ContainsKey(interactable))
                {
                    activeInteractables[interactable].active = false;
                    activeInteractablesCount -= 1;
                    ComputeCurrectTrackingInteractable();
                }
            }
        }

        protected void Reset()
        {
            activeInteractablesCount = 0;
            interactorGestureState = HPUIGesture.None;
            activeInteractables.Clear();
            activePriorityInteractable = null;
            lowestTargetZIndex = int.MaxValue;
            currentTrackingInteractable = null;
            cumilativeDistance = 0;
            cumilativeDirection = Vector2.zero;
        }

        /// <inheritdoc />
        public void Update(IDictionary<IHPUIInteractable, float> distances)
        {
            if (interactorGestureState == HPUIGesture.None)
            {
                return;
            }

            // Update the distances
            foreach(KeyValuePair<IHPUIInteractable, HPUIInteractionState> statePair in activeInteractables)
            {
                if (distances.TryGetValue(statePair.Key, out float distance))
                {
                    if (distance < statePair.Value.minDistanceToInteractor)
                    {
                        statePair.Value.minDistanceToInteractor = distance;
                    }
                }
            }

            // If interactable change, we need to restart tracking, hence skipping a frame
            if (changedInteractable)
            {
                // FIXME: an event should be triggered here?
                previousPosition = currentTrackingInteractable.ComputeInteractorPostion(interactor);
                changedInteractable = false;
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
                                state = activeInteractables[activePriorityInteractable];
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
                            state = activeInteractables[activePriorityInteractable];
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
            public bool active;
            public bool validTarget;
            public float minDistanceToInteractor;

            public HPUIInteractionState(float startTime, Vector2 startPosition, bool validTarget)
            {
                this.startTime = startTime;
                this.startPosition = startPosition;
                this.active = true;
                this.validTarget = validTarget;
                this.minDistanceToInteractor = float.MaxValue;
            }
        }
    }

}
