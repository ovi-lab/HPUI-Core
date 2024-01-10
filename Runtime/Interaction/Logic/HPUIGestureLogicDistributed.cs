using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Encapsulates the logic for HPUI gesture interactions.
    /// </summary>
    public class HPUIGestureLogicDistributed: IHPUIGestureLogic
    {
        private Dictionary<IHPUIInteractable, HPUIInteractionState> states = new Dictionary<IHPUIInteractable, HPUIInteractionState>();
        private float previousTime;

        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(() => new HPUITapEventArgs());
        private LinkedPool<HPUISwipeEventArgs> hpuiSwipeEventArgsPool = new LinkedPool<HPUISwipeEventArgs>(() => new HPUISwipeEventArgs());
        private float tapTimeThreshold, tapDistanceThreshold;
        private IHPUIInteractor interactor;

        /// <summary>
        /// Initializes a new instance of the with the thrshold values.
        /// </summary>
        public HPUIGestureLogicDistributed(IHPUIInteractor interactor, float tapTimeThreshold, float tapDistanceThreshold)
        {
            this.interactor = interactor;
            this.tapTimeThreshold = tapTimeThreshold;
            this.tapDistanceThreshold = tapDistanceThreshold;
            this.previousTime = Time.time;
        }

        /// <summary>
        /// To be called by <see cref="IXRSelectInteractor.OnSelectEntering"/> controlling this <see cref="HPUIGestureLogic"/>
        /// </summary>
        public void OnSelectEntering(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            HPUIInteractionState state = GenericPool<HPUIInteractionState>.Get();
            state.SetParams(HPUIGestureState.Tap,
                            Time.time,
                            interactable.ComputeInteractorPostion(interactor));
            states.Add(interactable, state);
        }

        /// <summary>
        /// To be called by <see cref="IXRSelectInteractor.OnSelectExiting"/> controlling this <see cref="HPUIGestureLogic"/>
        /// </summary>
        public void OnSelectExiting(IHPUIInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            if (states.Remove(interactable, out HPUIInteractionState state))
            {
                switch (state.gestureState)
                {
                    case HPUIGestureState.Tap:
                        using (hpuiTapEventArgsPool.Get(out HPUITapEventArgs tapEventArgs))
                        {
                            tapEventArgs.SetParams(interactor, interactable);
                            interactable.OnTap(tapEventArgs);
                            interactor.OnTap(tapEventArgs);
                        }
                        break;
                    case HPUIGestureState.Swipe:
                        using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                        {
                            swipeEventArgs.SetParams(interactor, interactable,
                                                     HPUISwipeState.Stopped, Time.time - state.startTime, state.startTime, state.startPosition,
                                                     state.cumilativeDirection, state.cumilativeDistance, state.delta);
                            interactable.OnSwipe(swipeEventArgs);
                            interactor.OnSwipe(swipeEventArgs);

                        }
                        break;
                }
                GenericPool<HPUIInteractionState>.Release(state);
            }
        }

        /// <summary>
        /// Updat method to be called from an interactor. Updates the states of the <see cref="IHPUIInteractable"/> selected by
        /// the <see cref="IXRInteractor"/> that was passed when initializing this <see cref="HPUIGestureLogic"/>.
        /// </summary>
        public void Update()
        {
            float currentTime = Time.time;
            foreach (var interactable in interactor.interactablesSelected)
            {
                if (interactable is IHPUIInteractable hpuiInteractable)
                {
                    Vector2 currentPosition = hpuiInteractable.ComputeInteractorPostion(interactor);
                    HPUIInteractionState state = states[hpuiInteractable];
                    float timeDelta = currentTime - state.startTime;
                    state.delta = currentPosition - state.previousPosition;
                    state.cumilativeDirection += state.delta;
                    state.cumilativeDistance += state.delta.magnitude;

                    switch (state.gestureState)
                    {
                        case HPUIGestureState.Tap:
                            if (timeDelta > tapTimeThreshold || state.cumilativeDirection.magnitude > tapDistanceThreshold)
                            {
                                state.gestureState = HPUIGestureState.Swipe;
                                using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                                {
                                    swipeEventArgs.SetParams(interactor, hpuiInteractable,
                                                             HPUISwipeState.Started, timeDelta, state.startTime, state.startPosition,
                                                             state.cumilativeDirection, state.cumilativeDistance, state.delta);
                                    hpuiInteractable.OnSwipe(swipeEventArgs);
                                    interactor.OnSwipe(swipeEventArgs);
                                }
                            }
                            break;
                        case HPUIGestureState.Swipe:
                            using (hpuiSwipeEventArgsPool.Get(out HPUISwipeEventArgs swipeEventArgs))
                            {
                                swipeEventArgs.SetParams(interactor, hpuiInteractable,
                                                         HPUISwipeState.Updated, timeDelta, state.startTime, state.startPosition,
                                                         state.cumilativeDirection, state.cumilativeDistance, state.delta);
                                hpuiInteractable.OnSwipe(swipeEventArgs);
                                interactor.OnSwipe(swipeEventArgs);
                            }
                            break;
                        case HPUIGestureState.Custom:
                            // TODO: custom gestures
                            throw new NotImplementedException();
                    }

                    state.previousPosition = currentPosition;
                }
            }
            previousTime = currentTime;
        }

        /// <summary>
        /// Clear cached objects.
        /// </summary>
        public void Dispose()
        {
            hpuiTapEventArgsPool.Dispose();
            hpuiSwipeEventArgsPool.Dispose();
            states.Clear();
        }

        class HPUIInteractionState
        {
            public HPUIGestureState gestureState;
            public float startTime;
            public Vector2 startPosition;
            public Vector2 previousPosition;
            public Vector2 delta;
            public Vector2 cumilativeDirection;
            public float cumilativeDistance;

            public HPUIInteractionState SetParams(HPUIGestureState gestureState, float startTime, Vector2 startPosition)
            {
                this.gestureState = gestureState;
                this.startTime = startTime;
                this.startPosition = startPosition;
                this.previousPosition = startPosition;
                return this;
            }
        }
    }

}
