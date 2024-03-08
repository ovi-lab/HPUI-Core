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
        private IHPUIInteractable lastTapInteractable, lastGestureInteractable;
        private int tapsCount = 0;
        private int gesturesCount = 0;

        void OnTapCallback(HPUITapEventArgs args)
        {
            tapsCount += 1;
            lastTapInteractable = args.interactableObject;

        }
        void OnGestureCallback(HPUIGestureEventArgs args)
        {
            gesturesCount += 1;
            Debug.Log($"{args.interactableObject}");
            lastGestureInteractable = args.interactableObject;
        }

        private void Reset()
        {
            tapsCount = 0;
            gesturesCount = 0;
            lastTapInteractable = null;
            lastGestureInteractable = null;
        }
            
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_SimpleTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);
            // First tap
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold /2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // Second tap
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold /2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 2);
            Assert.AreEqual(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_SimpleGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap and hold
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold * 2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);

            // Move
            logic.OnHoverEntering(i1);
            logic.Update();
            i1.interactorPosition = Vector2.one * 2;
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TapThenGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);
            // First tap
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold /2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // Gesture
            Reset();
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold * 2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_GestureThenTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);
            // Gesture
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold * 2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);

            // tap
            Reset();
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(TapTimeThreshold / 2);
            logic.Update();
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
        }

        [Test]
        public void HPUIGestureLogicUnifiedTest_TwoItem_tap_time()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap 1-2---1-2
            logic.OnHoverEntering(i1);
            logic.OnHoverEntering(i2);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 1-2---2-1
            logic.OnHoverEntering(i1);
            logic.OnHoverEntering(i2);
            logic.Update();
            logic.OnHoverExiting(i2);
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [Test]
        public void HPUIGestureLogicUnifiedTest_TwoItem_tap_zOrder()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap 1-2---1-2
            logic.OnHoverEntering(i1);
            logic.OnHoverEntering(i2);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 2-1---2-1
            logic.OnHoverEntering(i2);
            logic.OnHoverEntering(i1);
            logic.Update();
            logic.OnHoverExiting(i2);
            logic.OnHoverExiting(i1);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_gesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnHoverEntering(i2);
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.Greater(i1.swipCalled, 0);
            Assert.AreEqual(i2.swipCalled, 0);
        }

        // Anything ouside the priority window should not get selected
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_gesture_priority_window()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            // Even though this has lower zOrder, this should not get selected
            logic.OnHoverEntering(i2);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.Greater(i1.swipCalled, 0);
            Assert.AreEqual(i2.swipCalled, 0);
        }

        // When an event is not handled, hand over to next item in the priority list
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_gesture_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            logic.OnHoverEntering(i2);
            // even though this is coming in second, this should get the tap
            logic.OnHoverEntering(i1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);

            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnHoverEntering(i2);
            // even though this is coming in second, this should get the gesture
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.AreEqual(i2.swipCalled, 0);
            Assert.Greater(i1.swipCalled, 0);
        }

        //There can be instances where the event is not hanled by any interactable
        [UnityTest]
        public IEnumerator HPUIGestureLogicUnifiedTest_TwoItem_gesture_no_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            IHPUIGestureLogic logic = new HPUIGestureLogicUnified(new HPUIInteractor(), TapTimeThreshold, TapDistanceThreshold);

            // Tap not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnHoverEntering(i2);
            logic.OnHoverEntering(i1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(gesturesCount, 0);

            // Gesture not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            logic.OnHoverEntering(i2);
            logic.OnHoverEntering(i1);
            logic.Update();
            yield return new WaitForSeconds(1);
            logic.Update();
            logic.OnHoverExiting(i1);
            logic.OnHoverExiting(i2);
            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(gesturesCount, 0);
        }

        class TestHPUIInteractable : IHPUIInteractable
        {
            public Vector2 interactorPosition;
            public bool handlesTap, handlesGesture;
            public System.Action<HPUITapEventArgs> onTapCallback;
            public System.Action<HPUIGestureEventArgs> onGestureCallback;

            public int tapCalled = 0;
            public int swipCalled = 0;

            public TestHPUIInteractable(int zOrder, bool handlesTap, bool handlesGesture, Action<HPUITapEventArgs> onTapCallback = null, Action<HPUIGestureEventArgs> onGestureCallback = null)
            {
                this.zOrder = zOrder;
                this.handlesTap = handlesTap;
                this.handlesGesture = handlesGesture;
                if (onTapCallback != null)
                    this.onTapCallback = onTapCallback;
                if (onGestureCallback != null)
                    this.onGestureCallback = onGestureCallback;
                Reset();
            }

            public void Reset()
            {
                this.tapCalled = 0;
                this.swipCalled = 0;
            }

            #region IHPUIInteracttable only
            public int zOrder { get; set; }

            public Vector2 boundsMax { get; set; }

            public Vector2 boundsMin { get; set; }


            Vector2 IHPUIInteractable.ComputeInteractorPostion(IHPUIInteractor interactor)
            {
                return interactorPosition;
            }

            bool IHPUIInteractable.HandlesGesture(HPUIGesture state)
            {
                switch (state) {
                    case HPUIGesture.Tap:
                        return handlesTap;
                    case HPUIGesture.Gesture:
                        return handlesGesture;
                    default:
                        throw new InvalidOperationException($"Gesture state {state} is not handled");
                }
            }

            void IHPUIInteractable.OnGesture(HPUIGestureEventArgs args)
            {
                swipCalled += 1;
                onGestureCallback?.Invoke(args);
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

            HoverEnterEvent IXRHoverInteractable.firstHoverEntered => throw new NotImplementedException();

            HoverExitEvent IXRHoverInteractable.lastHoverExited => throw new NotImplementedException();

            HoverEnterEvent IXRHoverInteractable.hoverEntered => throw new NotImplementedException();

            HoverExitEvent IXRHoverInteractable.hoverExited => throw new NotImplementedException();

            List<IXRHoverInteractor> IXRHoverInteractable.interactorsHovering => throw new NotImplementedException();

            bool IXRHoverInteractable.isHovered => throw new NotImplementedException();

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

            bool IXRHoverInteractable.IsHoverableBy(IXRHoverInteractor interactor)
            {
                throw new NotImplementedException();
            }

            void IXRHoverInteractable.OnHoverEntering(HoverEnterEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRHoverInteractable.OnHoverEntered(HoverEnterEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRHoverInteractable.OnHoverExiting(HoverExitEventArgs args)
            {
                throw new NotImplementedException();
            }

            void IXRHoverInteractable.OnHoverExited(HoverExitEventArgs args)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}
