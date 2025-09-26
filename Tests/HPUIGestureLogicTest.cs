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
        private IHPUIInteractable lastGestureInteractable;
        private int gesturesCount = 0;

        void OnGestureCallback(HPUIGestureEventArgs args)
        {
            gesturesCount += 1;
            lastGestureInteractable = args.interactableObject;
        }

        private void Reset()
        {
            gesturesCount = 0;
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
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            HPUIInteractorGestureEventArgs eventArgs;

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);

            yield return new WaitForSeconds(InsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);

            yield return new WaitForSeconds(OutsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i1].State);

            yield return new WaitForSeconds(SimpleWaitTime);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Updated, gestureEvents[i1].State);

            updates.Remove(i1);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Stopped, gestureEvents[i1].State);

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
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
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            HPUIInteractorGestureEventArgs eventArgs;

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates[i1], interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i1].State);

            yield return new WaitForSeconds(InsideDebounceWindow);
            updates.Remove(i1);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.IsEmpty(interactableEvents);
            Assert.IsEmpty(eventArgs.InteractableAuxGestureStates);

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            // Started new gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates[i1], interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i1].State);

            // Exit without start even getting triggered should cancel
            yield return new WaitForSeconds(OutsideDebounceWindow);
            updates.Remove(i1);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.IsEmpty(interactableEvents);
            Assert.IsEmpty(eventArgs.InteractableAuxGestureStates);

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            // Start another gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates[i1], interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i1].State);

            yield return new WaitForSeconds(OutsideDebounceWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableGestureStates.Count, gestureEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableGestureStates[i1], gestureEvents[i1].State);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i1].State);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates[i1], interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingUpdate, interactableEvents[i1].State);

            updates.Remove(i1);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableGestureStates.Count, gestureEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableGestureStates[i1], gestureEvents[i1].State);
            Assert.AreEqual(HPUIGestureState.Stopped, gestureEvents[i1].State);

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);
            Assert.IsEmpty(interactableEvents);
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

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:false, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, interactableEvents.Count);
            Assert.AreEqual(HPUIInteractableState.Hovered, interactableEvents[i1].State);

            yield return new WaitForSeconds(OutsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, interactableEvents.Count);
            Assert.AreEqual(HPUIInteractableState.Hovered, interactableEvents[i1].State);

            updates.Remove(i1);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            // The interaction crosses radius
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:false, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            yield return new WaitForSeconds(OutsideCommitWindow);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsTrue(interactableEvents.ContainsKey(i1));
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_TwoItem()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            TestHPUIInteractable i2 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            HPUIGestureLogic logic = new HPUIGestureLogic(DebounceTimeWindow, GestureCommitDelay);
            logic.SwitchCurrentTrackingInteractableThreshold = 0;
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates[i1], interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i1].State);

            // Smaller heuristic - should be selected as active
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(2, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(HPUIInteractableState.TrackingUpdate, interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.InContact, interactableEvents[i2].State);

            yield return new WaitForSeconds(OutsideCommitWindow);
            // Should pick i2 still, as the lowest was still with i2
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(2, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIInteractableState.TrackingEnded, interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i2].State);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i2].State);

            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, 0);
            Assert.AreEqual(HPUIGestureState.Stopped, gestureEvents[i2].State);


            // Same loop with the tracking change threshold being high
            yield return new WaitForSeconds(OutsideDebounceWindow);
            logic.SwitchCurrentTrackingInteractableThreshold = 1;
            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);

            // Smaller heuristic - should be selected as active
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.AreEqual(HPUIInteractableState.TrackingEnded, interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i2].State);

            yield return new WaitForSeconds(OutsideCommitWindow);
            // Should pick i2 still, as the lowest was still with i2
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.AreEqual(HPUIInteractableState.InContact, interactableEvents[i1].State);
            Assert.AreEqual(HPUIInteractableState.TrackingUpdate, interactableEvents[i2].State);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i2].State);
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

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.001f, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(2, eventArgs.InteractableAuxGestureStates.Count);

            yield return new WaitForSeconds(OutsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableGestureStates.Count, gestureEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableGestureStates.Count);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.InteractableGestureStates[i1]);
        }

        [UnityTest]
        public IEnumerator HPUIGestureLogicTest_CancelOnDebounce()
        {
            Reset();
            TestHPUIInteractable i1 = new TestHPUIInteractable(0, true, OnGestureCallback);
            IHPUIInteractor interactor = new TestHPUIInteractor();
            float _debounceTimeWindow = 0.2f,
                _gestureCommitDelay = 0.1f,
                _outsideCommitInsideDebouceDelay = 0.12f,
                _simpleDelay = 0.25f;

            HPUIGestureLogic logic = new HPUIGestureLogic(_debounceTimeWindow, _gestureCommitDelay);
            Dictionary<IHPUIInteractable, HPUIInteractionInfo> updates = new Dictionary<IHPUIInteractable, HPUIInteractionInfo>();

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            // Firest trigger valid gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            yield return new WaitForSeconds(_simpleDelay);
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, gestureEvents[i1].State);

            // Now trigger a gesture that would get canceled
            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(eventArgs.InteractableAuxGestureStates.Count, interactableEvents.Count);
            Assert.AreEqual(1, eventArgs.InteractableAuxGestureStates.Count);

            yield return new WaitForSeconds(_outsideCommitInsideDebouceDelay);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i1].State);

            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Canceled, gestureEvents[i1].State);
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

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNull(eventArgs);

            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, interactableEvents[i1].State);

            yield return new WaitForSeconds(OutsideCommitWindow);

            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Started, gestureEvents[i1].State);

            // Even though this has lower zOrder and lower heuristic, this should not get selected
            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue - 0.01f, true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Updated, gestureEvents[i1].State);
            Assert.AreEqual(2, interactableEvents.Count);
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

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            // even though this has a higher heuristic, this should get the gesture
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue + 0.1f, true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);

            yield return new WaitForSeconds(OutsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(2, interactableEvents.Count);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.InteractableGestureStates[i1]);

            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.IsEmpty(interactableEvents);
            Assert.AreEqual(1, gestureEvents.Count);
            Assert.AreEqual(gestureEvents.Count, eventArgs.InteractableGestureStates.Count);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.InteractableGestureStates[i1]);
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

            HPUIInteractorGestureEventArgs eventArgs;
            Dictionary<IHPUIInteractable, HPUIGestureEventArgs> gestureEvents = new();
            Dictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> interactableEvents = new();

            updates[i2] = new HPUIInteractionInfo(dummyHeuristicValue, isSelection:true, Vector3.zero, null, 0, null);
            updates[i1] = new HPUIInteractionInfo(dummyHeuristicValue + 0.1f, true, Vector3.zero, null, 0, null);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(2, interactableEvents.Count);
            Assert.IsEmpty(gestureEvents);
            Assert.AreEqual(interactableEvents.Count, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(HPUIInteractableState.TrackingStarted, eventArgs.InteractableAuxGestureStates[i2]);
            Assert.AreEqual(gestureEvents.Count, eventArgs.InteractableGestureStates.Count);

            yield return new WaitForSeconds(OutsideCommitWindow);
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Started, eventArgs.State);
            Assert.AreEqual(2, interactableEvents.Count);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.AreEqual(interactableEvents.Count, eventArgs.InteractableAuxGestureStates.Count);
            Assert.AreEqual(HPUIInteractableState.TrackingUpdate, eventArgs.InteractableAuxGestureStates[i2]);
            Assert.AreEqual(HPUIInteractableState.InContact, eventArgs.InteractableAuxGestureStates[i1]);

            updates.Clear();
            gestureEvents.Clear();
            interactableEvents.Clear();
            eventArgs = logic.ComputeInteraction(interactor, updates, gestureEvents, interactableEvents);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HPUIGestureState.Stopped, eventArgs.State);
            Assert.IsEmpty(gestureEvents);
            Assert.IsEmpty(eventArgs.InteractableGestureStates);
            Assert.IsEmpty(interactableEvents);
            Assert.IsEmpty(eventArgs.InteractableAuxGestureStates);
        }
    }
}
