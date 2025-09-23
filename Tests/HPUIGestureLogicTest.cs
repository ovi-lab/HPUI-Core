using System.Collections;
using UnityEngine.TestTools;
using ubco.ovilab.HPUI.Core.Interaction;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace ubco.ovilab.HPUI.Core.Tests
{
    public class HPUIGestureLogicTest
    {
        const float DebounceTimeWindow = 0.1f;
        const float GestureCommitDelay = 0.1f;
        const float SelectionRadius = 0.01f;
        const float OutsideSelectionRadius = 0.02f;
        const float InsideSelectionRadius = 0.005f;
        const float dummyHeuristicValue = 0.01f;
        const float InsideDebounceWindow = 0.05f;
        const float OutsideDebounceWindow = 0.11f;
        const float InsideCommitWindow = 0.05f;
        const float OutsideCommitWindow = 0.11f;
        const float SimpleWaitTime = OutsideDebounceWindow * 2;
        private IHPUIInteractable winningInteractable, lastGestureInteractable;
        private int gesturesCount = 0;

        void OnGestureCallback(HPUIGestureEventArgs args)
        {
            gesturesCount += 1;
            lastGestureInteractable = args.interactableObject;
        }

        private void Reset()
        {
            gesturesCount = 0;
            winningInteractable = null;
            lastGestureInteractable = null;
        }
            
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleGesture()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);

            yield return new WaitForSeconds(InsideCommitWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);

            yield return new WaitForSeconds(OutsideCommitWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);

            yield return new WaitForSeconds(SimpleWaitTime);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Updated, eventArgs.State);

            updates.Remove(i1);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.State);

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_SimpleGesture_DebounceIgnore()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);

            yield return new WaitForSeconds(InsideDebounceWindow);
            updates.Remove(i1);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Canceled, eventArgs.State);

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            // Started new gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);

            // Exit without start even getting triggered should cancel
            yield return new WaitForSeconds(OutsideDebounceWindow);
            updates.Remove(i1);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Canceled, eventArgs.State);

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            // Start another gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);

            yield return new WaitForSeconds(OutsideDebounceWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);

            updates.Remove(i1);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.State);

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);
        }

        // Testing the interaction radius
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_OneItem_InteractionRadius()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:false, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            yield return new WaitForSeconds(OutsideCommitWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates.Remove(i1);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            // The interaction crosses radius
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:false, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);
            yield return new WaitForSeconds(OutsideCommitWindow);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i2, winningInteractable);

            updates.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Canceled, eventArgs.State);

            yield return new WaitForSeconds(OutsideCommitWindow);
            i1.Reset();
            i2.Reset();

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.1f, true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i2, winningInteractable);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_zOrder()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(1, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);

            updates.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Canceled, eventArgs.State);
            return null;
        }

        // Anything outside the priority window should not get selected
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_priority_window()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(1, true, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            eventArgs = logic.ComputeInteraction(interactor, updates, out IHPUIInteractable _);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);
            Assert.AreEqual(i1, eventArgs.interactableObject);

            yield return new WaitForSeconds(OutsideCommitWindow);

            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);
            Assert.AreEqual(i1, eventArgs.interactableObject);

            // Even though this has lower zOrder and lower heuristic, this should not get selected
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.01f, true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Updated, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);
        }

        // When an event is not handled, hand over to next item in the priority list
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            // even though this has a higher heuristic, this should get the gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue + 0.1f, true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);

            yield return new WaitForSeconds(OutsideCommitWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);

            updates.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.State);
            Assert.AreEqual(i1, winningInteractable);
        }

        //There can be instances where the event is not handled by any interactable
        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem_gesture_no_handle_events()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, false, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, false, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIGestureEventArgs eventArgs;

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue + 0.1f, true, Vector3.zero, null, 0, null);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.CommitPending, eventArgs.State);
            Assert.AreEqual(null, winningInteractable);
            Assert.AreEqual(null, eventArgs.interactableObject);

            yield return new WaitForSeconds(OutsideCommitWindow);
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);
            Assert.AreEqual(null, winningInteractable);
            Assert.AreEqual(null, eventArgs.interactableObject);

            updates.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, out winningInteractable);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.State);
            Assert.AreEqual(null, winningInteractable);
            Assert.AreEqual(null, eventArgs.interactableObject);
        }
    }
}
