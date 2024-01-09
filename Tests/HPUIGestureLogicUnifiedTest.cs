using System.Collections;
using UnityEngine.TestTools;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine;
using System;
using NUnit.Framework;

namespace ubco.ovilab.HPUI.Tests
{
    public class HPUIGestureLogicUnifiedTest
    {
        const float TapTimeThreshold = 0.4f;
        const int TapDistanceThreshold = 1;
        private IHPUIInteractable lastTapInteractable, lastSwipeInteractable;
        private int tapsCount = 0;
        private int swipesCount = 0;

        void OnTapCallback(HPUITapEventArgs args)
        {
            tapsCount += 1;
            lastTapInteractable = args.interactableObject;

        }
        void OnSwipeCallback(HPUISwipeEventArgs args)
        {
            swipesCount += 1;
            Debug.Log($"{args.interactableObject}");
            lastSwipeInteractable = args.interactableObject;
        }

        private void Reset()
        {
            tapsCount = 0;
            swipesCount = 0;
            lastTapInteractable = null;
            lastSwipeInteractable = null;
        }
            
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_SimpleTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);
            // First tap
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold /2);
            logic.Update();
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);

            // Second tap
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold /2);
            logic.Update();
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 2);
            Assert.AreEqual(swipesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_SimpleSwipe()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap and hold
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold * 2);
            logic.Update();
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(swipesCount, 0);

            // Move
            logic.OnSelectEntering(i1);
            logic.Update();
            i1.interactorPosition = Vector2.one * 2;
            logic.Update();
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(swipesCount, 0);
        }

        [Test]
        public void HPUIGestureLogicUnifiedTest_TwoItem_tap_time()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap 1-2---1-2
            logic.OnSelectEntering(i1);
            logic.OnSelectEntering(i2);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 1-2---2-1
            logic.OnSelectEntering(i1);
            logic.OnSelectEntering(i2);
            logic.Update();
            logic.OnSelectExiting(i2);
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [Test]
        public void HPUIGestureLogicUnifiedTest_TwoItem_tap_zOrder()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap 1-2---1-2
            logic.OnSelectEntering(i1);
            logic.OnSelectEntering(i2);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 2-1---2-1
            logic.OnSelectEntering(i2);
            logic.OnSelectEntering(i1);
            logic.Update();
            logic.OnSelectExiting(i2);
            logic.OnSelectExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_swipe()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnSelectEntering(i2);
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(swipesCount, 0);
            Assert.AreEqual(lastSwipeInteractable, i1);
            Assert.Greater(i1.swipCalled, 0);
            Assert.AreEqual(i2.swipCalled, 0);
        }

        // Anything ouside the priority window should not get selected
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_swipe_priority_window()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            // Even though this has lower zOrder, this should not get selected
            logic.OnSelectEntering(i2);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(swipesCount, 0);
            Assert.AreEqual(lastSwipeInteractable, i1);
            Assert.Greater(i1.swipCalled, 0);
            Assert.AreEqual(i2.swipCalled, 0);
        }

        // When an event is not handled, hand over to next item in the priority list
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_swipe_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnSelectEntering(i2);
            // even though this is coming in second, this should get the tap
            logic.OnSelectEntering(i1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(swipesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);

            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnSelectEntering(i2);
            // even though this is coming in second, this should get the swipe
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(swipesCount, 0);
            Assert.AreEqual(lastSwipeInteractable, i1);
            Assert.AreEqual(i2.swipCalled, 0);
            Assert.Greater(i1.swipCalled, 0);
        }

        //There can be instances where the event is not hanled by any interactable
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_swipe_no_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnSwipeCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnSwipeCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnSelectEntering(i2);
            logic.OnSelectEntering(i1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(swipesCount, 0);

            // Swipe not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnSelectEntering(i2);
            logic.OnSelectEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnSelectExiting(i1);
            logic.OnSelectExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(swipesCount, 0);
        }

        class TestHPUIInteractable : IHPUIInteractable
        {
            public Vector2 interactorPosition;
            public bool handlesTap, handlesSwipe;
            public System.Action<HPUITapEventArgs> onTapCallback;
            public System.Action<HPUISwipeEventArgs> onSwipeCallback;

            public int tapCalled = 0;
            public int swipCalled = 0;

            public TestHPUIInteractable(int zOrder, bool handlesTap, bool handlesSwipe, Action<HPUITapEventArgs> onTapCallback = null, Action<HPUISwipeEventArgs> onSwipeCallback = null)
            {
                this.zOrder = zOrder;
                this.handlesTap = handlesTap;
                this.handlesSwipe = handlesSwipe;
                if (onTapCallback != null)
                    this.onTapCallback = onTapCallback;
                if (onSwipeCallback != null)
                    this.onSwipeCallback = onSwipeCallback;
                Reset();
            }

            public void Reset()
            {
                this.tapCalled = 0;
                this.swipCalled = 0;
            }

            #region IHPUIInteracttable only
            public int zOrder { get; set; }

            Vector2 IHPUIInteractable.ComputeInteractorPostion(IXRInteractor interactor)
            {
                return interactorPosition;
            }

            bool IHPUIInteractable.HandlesGestureState(HPUIGestureState state)
            {
                switch (state) {
                    case HPUIGestureState.Tap:
                        return handlesTap;
                    case HPUIGestureState.Swipe:
                        return handlesSwipe;
                    default:
                        throw new InvalidOperationException($"Gesture state {state} is not handled");
                }
            }

            void IHPUIInteractable.OnSwipe(HPUISwipeEventArgs args)
            {
                swipCalled += 1;
                onSwipeCallback?.Invoke(args);
            }

            void IHPUIInteractable.OnTap(HPUITapEventArgs args)
            {
                tapCalled += 1;
                onTapCallback?.Invoke(args);
            }
            #endregion

            #region Implement all other interfaces
            SelectEnterEvent IXRSelectInteractable.firstSelectEntered => throw new NotImplementedException();

            SelectExitEvent IXRSelectInteractable.lastSelectExited => throw new NotImplementedException();

            SelectEnterEvent IXRSelectInteractable.selectEntered => throw new NotImplementedException();

            SelectExitEvent IXRSelectInteractable.selectExited => throw new NotImplementedException();

            List<IXRSelectInteractor> IXRSelectInteractable.interactorsSelecting => throw new NotImplementedException();

            IXRSelectInteractor IXRSelectInteractable.firstInteractorSelecting => throw new NotImplementedException();

            bool IXRSelectInteractable.isSelected => throw new NotImplementedException();

            InteractableSelectMode IXRSelectInteractable.selectMode => throw new NotImplementedException();

            InteractionLayerMask IXRInteractable.interactionLayers => throw new NotImplementedException();

            List<Collider> IXRInteractable.colliders => throw new NotImplementedException();

            Transform IXRInteractable.transform => throw new NotImplementedException();

            event Action<InteractableRegisteredEventArgs> IXRInteractable.registered
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }

            event Action<InteractableUnregisteredEventArgs> IXRInteractable.unregistered
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }

            Pose IXRSelectInteractable.GetAttachPoseOnSelect(IXRSelectInteractor interactor)
            {
                throw new NotImplementedException();
            }

            Transform IXRInteractable.GetAttachTransform(IXRInteractor interactor)
            {
                throw new NotImplementedException();
            }

            float IXRInteractable.GetDistanceSqrToInteractor(IXRInteractor interactor)
            {
                throw new NotImplementedException();
            }

            Pose IXRSelectInteractable.GetLocalAttachPoseOnSelect(IXRSelectInteractor interactor)
            {
                throw new NotImplementedException();
            }

            bool IXRSelectInteractable.IsSelectableBy(IXRSelectInteractor interactor)
            {
                throw new NotImplementedException();
            }

            void IXRInteractable.OnRegistered(InteractableRegisteredEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRSelectInteractable.OnSelectEntered(SelectEnterEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRSelectInteractable.OnSelectEntering(SelectEnterEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRSelectInteractable.OnSelectExited(SelectExitEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRSelectInteractable.OnSelectExiting(SelectExitEventArgs args)
            {
                throw new NotImplementedException();
            }
            void IXRInteractable.OnUnregistered(InteractableUnregisteredEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRInteractable.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}
