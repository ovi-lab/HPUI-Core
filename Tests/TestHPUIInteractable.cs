using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Core.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core.Tests
{
    class TestHPUIInteractable : IHPUIInteractable
    {
        public Vector2 interactorPosition;
        public bool handlesGesture;
        public System.Action<HPUIGestureEventArgs> onGestureCallback;

        public int gestureCalled = 0;

        public TestHPUIInteractable(int zOrder, bool handlesGesture, Action<HPUIGestureEventArgs> onGestureCallback = null)
        {
            this.zOrder = zOrder;
            this.handlesGesture = handlesGesture;
            if (onGestureCallback != null)
                this.onGestureCallback = onGestureCallback;
            Reset();
        }

        public void Reset()
        {
            this.gestureCalled = 0;
        }

        #region IHPUIInteracttable only
        public int zOrder { get; set; }

        public Vector2 boundsMax { get; set; }

        public Vector2 boundsMin { get; set; }

        bool IHPUIInteractable.ComputeInteractorPosition(IHPUIInteractor interactor, out Vector2 position)
        {
            position = interactorPosition;
            return true;
        }

        bool IHPUIInteractable.HandlesGesture()
        {
            return handlesGesture;
        }

        void IHPUIInteractable.OnGesture(HPUIGestureEventArgs args)
        {
            gestureCalled += 1;
            onGestureCallback?.Invoke(args);
        }

        void IHPUIInteractable.OnInteractableStateEvent(HPUIInteractableStateEventArgs args)
        { }
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

        Transform IXRInteractable.transform => null;

        HoverEnterEvent IXRHoverInteractable.firstHoverEntered => throw new NotImplementedException();

        HoverExitEvent IXRHoverInteractable.lastHoverExited => throw new NotImplementedException();

        HoverEnterEvent IXRHoverInteractable.hoverEntered => throw new NotImplementedException();

        HoverExitEvent IXRHoverInteractable.hoverExited => throw new NotImplementedException();

        List<IXRHoverInteractor> IXRHoverInteractable.interactorsHovering => throw new NotImplementedException();

        bool IXRHoverInteractable.isHovered => throw new NotImplementedException();

        HPUIGestureEvent IHPUIInteractable.GestureEvent => throw new NotImplementedException();

        HPUIInteractableStateEvent IHPUIInteractable.AuxGestureEvent => throw new NotImplementedException();

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
