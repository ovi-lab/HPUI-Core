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
        const float dummyHeuristicValue = 0.01f;
        const float OutsideTapTime = 0.6f;
        const float InsideTapTime = 0.2f;
        const float OutsideDebounceWindow = 0.11f;
        const float InsideDebounceWindow = 0.05f;
        private IHPUIInteractable winningTapInteractable, lastGestureInteractable;
        private int tapsCount = 0;
        private int gesturesCount = 0;

        void OnTapCallback(HPUITapEventArgs args)
        {
            tapsCount += 1;
            winningTapInteractable = args.interactableObject;

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
            winningTapInteractable = null;
            lastGestureInteractable = null;
        }
            
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // First tap
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            Assert.AreEqual(HPUIGesture.Tap, gesture);

            // Second tap
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);

            updates.Remove(i1);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            Assert.AreEqual(HPUIGesture.Tap, gesture);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleTap_DebounceIgnore()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // First tap
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            Assert.AreEqual(HPUIGesture.Tap, gesture);

            // Second tap inside debounce time window
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideDebounceWindow);

            updates.Remove(i1);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            // thrid tap outside debounce time window, which is also inside tap time window
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            updates.Remove(i1);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            Assert.AreEqual(HPUIGesture.Tap, gesture);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // Tap and hold
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);

            updates.Remove(i1);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);

            // Move
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);
            i1.interactorPosition = Vector2.right * (TapDistanceThreshold * 1.01f);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);

            updates.Remove(i1);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
        }

        // Testing the interaction radius
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_OneItem_InteractionRadius()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, false, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // The interaction doesn't reach radius.
            Reset();
            i1.Reset();

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);

            updates.Remove(i1);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            // The interaction crosses radius
            Reset();
            i1.Reset();

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);
            yield return new WaitForSeconds(InsideTapTime);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TapThenGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // First tap
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);

            // Gesture
            Reset();

            updates[i1] = new HPUIInteractionInfo(InsideSelectionRadius, InsideSelectionRadius < SelectionRadius, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);

            argsToUpdate = new HPUIGestureEventArgs();
            updates[i1] = new HPUIInteractionInfo(OutsideSelectionRadius, OutsideSelectionRadius < SelectionRadius, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_GestureThenTap()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // Gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);

            // tap
            Reset();
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_tap_time()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // Tap 1-2---1-2
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(winningTapInteractable, null);
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            Assert.AreEqual(winningTapInteractable, i2);
            Assert.AreEqual(HPUIGesture.Tap, gesture);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            Reset();
            i1.Reset();
            i2.Reset();
            // Tap 1-2---2-1
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.1f, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(winningTapInteractable, null);
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(winningTapInteractable, i2);
            Assert.AreEqual(HPUIGesture.Tap, gesture);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_tap_zOrder()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // Tap 1-2---1-2
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, false, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
            Assert.AreEqual(i1, winningTapInteractable);

            yield return new WaitForSeconds(OutsideDebounceWindow);

            Reset();
            i1.Reset();
            i2.Reset();

            // Tap 2-1---2-1
            updates.Clear();
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.01f, true, Vector3.zero, null, 0, null);
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideDebounceWindow);
            updates.Clear();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
            Assert.AreEqual(i1, winningTapInteractable);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue - 0.01f, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);

            yield return new WaitForSeconds(InsideDebounceWindow);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Updated, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);

            updates.Clear();
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);
        }

        // Anything outside the priority window should not get selected
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_priority_window()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(1, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);

            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);
            Assert.AreEqual(i1, winningTapInteractable);

            // Even though this has lower zOrder and lower heuristic, this should not get selected
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.01f, true, Vector3.zero, null, 0, null);
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Updated, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);

            yield return new WaitForSeconds(1);
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(i1, winningTapInteractable);

            updates.Clear();
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
        }

        // When an event is not handled, hand over to next item in the priority list
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, true, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            // even though this has a higher heuristic, this should get the tap
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue + 0.1f, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(InsideTapTime);
            updates.Clear();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
            Assert.AreEqual(i1, winningTapInteractable);

            Reset();
            i1.Reset();
            i2.Reset();

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null); 
           // even though this is coming in second, this should get the gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);

            updates.Clear();
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
            Assert.AreEqual(i1, argsToUpdate.interactableObject);
            Assert.AreEqual(i1, winningTapInteractable);
        }

        //There can be instances where the event is not handled by any interactable
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_no_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, false, OnTapCallback, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(TapTimeThreshold, TapDistanceThreshold, DebounceTimeWindow);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            // Tap not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out HPUIGesture gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            updates.Clear();
            HPUITapEventArgs tapArgsToPopulate = new HPUITapEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, tapArgsToPopulate, new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.Tap, gesture);
            Assert.AreEqual(null, tapArgsToPopulate.interactableObject);
            Assert.AreEqual(null, winningTapInteractable);
            Assert.AreNotEqual(null, tapArgsToPopulate.interactorObject);

            // Gesture not handled by any interactable
            Reset();
            i1.Reset();
            i2.Reset();
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, true, Vector3.zero, null, 0, null);
            logic.ComputeInteraction(interactor, updates, out gesture, out IHPUIInteractable _, new HPUITapEventArgs(), new HPUIGestureEventArgs());
            Assert.AreEqual(HPUIGesture.None, gesture);

            yield return new WaitForSeconds(OutsideTapTime);
            HPUIGestureEventArgs argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Started, argsToUpdate.State);
            Assert.AreEqual(null, argsToUpdate.interactableObject);
            Assert.AreEqual(null, winningTapInteractable);
            Assert.AreNotEqual(null, tapArgsToPopulate.interactorObject);

            updates.Clear();
            argsToUpdate = new HPUIGestureEventArgs();
            logic.ComputeInteraction(interactor, updates, out gesture, out winningTapInteractable, new HPUITapEventArgs(), argsToUpdate);
            Assert.AreEqual(HPUIGesture.Gesture, gesture);
            Assert.AreEqual(HPUIGestureState.Stopped, argsToUpdate.State);
            Assert.AreEqual(null, argsToUpdate.interactableObject);
            Assert.AreEqual(null, winningTapInteractable);
            Assert.AreNotEqual(null, tapArgsToPopulate.interactorObject);
        }
    }
}
