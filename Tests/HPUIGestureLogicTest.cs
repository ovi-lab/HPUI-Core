using System.Collections;
using UnityEngine.TestTools;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace ubco.ovilab.HPUI.Tests
{
    public class HPUIGestureLogicTest
    {
        const float TapTimeThreshold = 0.4f;
        const float DebounceTimeWindow = 0.1f;
        const int TapDistanceThreshold = 1;
        const float SelectionRadius = 0.01f;
        const float OutsideSelectionRadius = 0.02f;
        const float InsideSelectionRadius = 0.005f;
        const float OutsideTapTime = 0.6f;
        const float InsideTapTime = 0.2f;
        const float OutsideDebounceWindow = 0.11f;
        const float InsideDebounceWindow = 0.05f;
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
        public IEnumerator HPUIGestureLogicTest_SimpleTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // First tap
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideTapTime);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // Second tap
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideTapTime);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 2);
            Assert.AreEqual(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleTap_DebnouceIgnore()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // First tap
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideTapTime);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // Second tap inside debounce time window
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideDebounceWindow);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // thrid tap outside debounce time window, which is also inside tap time window
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 2);
            Assert.AreEqual(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // Tap and hold
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);

            // Move
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            i1.interactorPosition = Vector2.one * 2;
            logic.Update(updates);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
        }

        // Testing the interaction radius
        [Test]
        public void HPUIGestureLogicTest_OneItem_InteractionRadius()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, false, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // The interaction doesn't reach radius.
            Reset();
            i1.Reset();

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates.Remove(i1);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(gesturesCount, 0);

            // The interaction crosses radius
            Reset();
            i1.Reset();

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TapThenGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // First tap
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideTapTime);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);

            // Gesture
            Reset();

            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_GestureThenTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // Gesture
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);

            // tap
            Reset();
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(InsideTapTime);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_tap_time()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // Tap 1-2---1-2
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 1-2---2-1
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates[i2] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_tap_zOrder()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // Tap 1-2---1-2
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates[i1] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(OutsideSelectionRadius, 0, OutsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            Reset();
            i1.Reset();
            i2.Reset();

            // Tap 2-1---2-1
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);
            Assert.AreEqual(i1.tapCalled, 1);
            Assert.AreEqual(i2.tapCalled, 0);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.Greater(i1.gestureCalled, 0);
            Assert.AreEqual(i2.gestureCalled, 0);
        }

        // Anything outside the priority window should not get selected
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_priority_window()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);
            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            // Even though this has lower zOrder, this should not get selected
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(1);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.Greater(i1.gestureCalled, 0);
            Assert.AreEqual(i2.gestureCalled, 0);
        }

        // When an event is not handled, hand over to next item in the priority list
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            // even though this is coming in second, this should get the tap
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 1);
            Assert.AreEqual(gesturesCount, 0);
            Assert.AreEqual(lastTapInteractable, i1);

            Reset();
            i1.Reset();
            i2.Reset();

            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius); 
           // even though this is coming in second, this should get the gesture
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.Greater(gesturesCount, 0);
            Assert.AreEqual(lastGestureInteractable, i1);
            Assert.AreEqual(i2.gestureCalled, 0);
            Assert.Greater(i1.gestureCalled, 0);
        }

        //There can be instances where the event is not handled by any interactable
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_no_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            HPUIGestureLogic logic = new HPUIGestureLogic(new TestHPUIInteractor(), TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow, false);
            Dictionary<IHPUIInteractable, HPUIInteractionData> updates = new Dictionary<IHPUIInteractable, HPUIInteractionData>();

            // Tap not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();

            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(gesturesCount, 0);

            // Gesture not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            updates[i1] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            updates[i2] = new HPUIInteractionData(InsideSelectionRadius, 0, InsideSelectionRadius < SelectionRadius);
            logic.Update(updates);

            yield return new WaitForSeconds(OutsideTapTime);
            logic.Update(updates);

            updates.Clear();
            logic.Update(updates);

            Assert.AreEqual(tapsCount, 0);
            Assert.AreEqual(gesturesCount, 0);
        }
    }
}
