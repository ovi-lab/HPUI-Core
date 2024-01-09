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
        // private Dictionary<IHPUIInteractable, HPUIInteractionState> states = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUISwipeEventArgs> hpuiSwipeEventArgsPool = new LinkedPool<HPUISwipeEventArgs>(() => new HPUISwipeEventArgs());

        private float tapTimeThreshold, tapDistanceThreshold;
        private IHPUIInteractor interactor;

        private float startTime, cumilativeDistance;
        private Vector2 previousPosition, cumilativeDelta;

        private int lowestTargetZIndex = int.MaxValue;
        private int activeInteractablesCount = 0;
        private bool changedInteractable = false;

        private IHPUIInteractable activePriorityInteractable, currentTrackingInteractable;
        private Dictionary<IHPUIInteractable, HPUIInteractionState> activeInteractables = new Dictionary<IHPUIInteractable, HPUIInteractionState>();

        private HPUIGestureState interactorGestureState = HPUIGestureState.None;

        /// <summary>
        /// Initializes a new instance of the with the thrshold values.
        /// </summary>
        public HPUIGestureLogicUnified(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold)
        {
            this.interactor = interactor;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            Reset();
        }

        /// <inheritdoc />
        public void OnSelectEntering(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            activeInteractablesCount += 1;

            if (interactorGestureState == HPUIGestureState.None)
            {
                interactorGestureState = HPUIGestureState.Tap;
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

                HPUIInteractionState state = new HPUIInteractionState(Time.time, inValidWindow);
                activeInteractables.Add(interactable, state);

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
            // Any target that is active should be ok for this.
            IHPUIInteractable interactableToTrack = activeInteractables
                .Where(kvp => kvp.Value.active)
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
                .Where(kvp => kvp.Key.HandlesGestureState(interactorGestureState) && kvp.Value.validTarget)
                .OrderBy(kvp => kvp.Key.zOrder)
                .ThenBy(kvp => kvp.Value.startTime)
                .FirstOrDefault().Key;

            if (interactableToBeActive != activePriorityInteractable)
            {
                activePriorityInteractable = interactableToBeActive;
            }
        }

        /// <inheritdoc />
        public void OnSelectExiting(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            activeInteractablesCount -= 1;

            if (activeInteractablesCount == 0)
            {
                switch (interactorGestureState)
                {
                    case HPUIGestureState.Tap:
                        using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
                        {
                            ComputeActivePriorityInteractable();
                            tapEventArgs.SetParams(interactor, activePriorityInteractable);
                            activePriorityInteractable?.OnTap(tapEventArgs);
                            interactor.OnTap(tapEventArgs);
                        }
                        break;
                    case HPUIGestureState.Swipe:
                        using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                        {
                            // TODO
                            swipeEventArgs.interactorObject = interactor;
                            swipeEventArgs.interactableObject = activePriorityInteractable;
                            // swipeEventArgs.SetParams(interactor, activePriorityInteractable,
                            //                          HPUISwipeState.Stopped, Time.time - state.startTime, state.startTime, state.startPosition,
                            //                          state.previousPosition, state.previousPosition, state.previousPosition - state.startPosition);

                            activePriorityInteractable?.OnSwipe(swipeEventArgs);
                            interactor.OnSwipe(swipeEventArgs);

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
                    ComputeCurrectTrackingInteractable();
                }
            }
        }

        protected void Reset()
        {
            activeInteractablesCount = 0;
            interactorGestureState = HPUIGestureState.None;
            activeInteractables.Clear();
            activePriorityInteractable = null;
            lowestTargetZIndex = int.MaxValue;
            currentTrackingInteractable = null;
            cumilativeDistance = 0;
            cumilativeDelta = Vector2.zero;
        }

        /// <inheritdoc />
        public void Update()
        {
            if (interactorGestureState == HPUIGestureState.None)
            {
                return;
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
            Vector2 delta = previousPosition - currentPosition;
            float timeDelta = Time.time - startTime;
            cumilativeDistance += delta.magnitude;
            cumilativeDelta += delta;

            switch(interactorGestureState)
            {
                case HPUIGestureState.Tap:
                    if (timeDelta > tapTimeThreshold || cumilativeDistance > tapDistanceThreshold)
                    {
                        interactorGestureState = HPUIGestureState.Swipe;
                        ComputeActivePriorityInteractable();
                        using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                        {
                            // TODO
                            swipeEventArgs.interactorObject = interactor;
                            swipeEventArgs.interactableObject = activePriorityInteractable;
                            // swipeEventArgs.SetParams(interactor, activePriorityInteractable,
                            //                          HPUISwipeState.Started, timeDelta, state.startTime, state.startPosition, state.previousPosition,
                            //                          currentPosition, direction);
                            activePriorityInteractable?.OnSwipe(swipeEventArgs);
                            interactor.OnSwipe(swipeEventArgs);
                        }
                    }
                    break;
                case HPUIGestureState.Swipe:
                    using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                    {
                        // TODO
                        swipeEventArgs.interactorObject = interactor;
                        swipeEventArgs.interactableObject = activePriorityInteractable;
                        // swipeEventArgs.SetParams(interactor, activePriorityInteractable,
                        //                          HPUISwipeState.Updated, timeDelta, state.startTime, state.startPosition, state.previousPosition,
                        //                          currentPosition, direction);
                        activePriorityInteractable?.OnSwipe(swipeEventArgs);
                        interactor.OnSwipe(swipeEventArgs);
                    }
                    break;
                case HPUIGestureState.Custom:
                    // TODO: custom gestures
                    throw new NotImplementedException();
            }

        }

        /// <summary>
        /// Clear cached objects.
        /// </summary>
        public void Dispose()
        {
            Reset();
            hpuiTapEventArgsPool.Dispose();
            hpuiSwipeEventArgsPool.Dispose();
        }

        class HPUIInteractionState
        {
            public float startTime;
            public bool active;
            public bool validTarget;

            public HPUIInteractionState(float startTime, bool validTarget)
            {
                this.startTime = startTime;
                this.active = true;
                this.validTarget = validTarget;
            }
        }
    }

}
