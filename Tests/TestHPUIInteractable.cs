using System;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Tests
{
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
            switch (state)
            {
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

        HPUITapEvent IHPUIInteractable.TapEvent => throw new NotImplementedException();

        HPUIGestureEvent IHPUIInteractable.GestureEvent => throw new NotImplementedException();

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
