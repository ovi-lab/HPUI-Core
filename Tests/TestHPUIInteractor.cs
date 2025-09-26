using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Core.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core.Tests
{
    class TestHPUIInteractor : IHPUIInteractor
    {
        private GameObject tempObj;

        public TestHPUIInteractor()
        {
            tempObj = new GameObject();
        }

        ~TestHPUIInteractor()
        {
            GameObject.DestroyImmediate(tempObj);
        }

        HPUIInteractorGestureEvent IHPUIInteractor.GestureEvent => throw new NotImplementedException();

        HPUIHoverUpdateEvent IHPUIInteractor.HoverUpdateEvent => throw new NotImplementedException();

        SelectEnterEvent IXRSelectInteractor.selectEntered => throw new NotImplementedException();

        SelectExitEvent IXRSelectInteractor.selectExited => throw new NotImplementedException();

        List<IXRSelectInteractable> IXRSelectInteractor.interactablesSelected => throw new NotImplementedException();

        IXRSelectInteractable IXRSelectInteractor.firstInteractableSelected => throw new NotImplementedException();

        bool IXRSelectInteractor.hasSelection => throw new NotImplementedException();

        bool IXRSelectInteractor.isSelectActive => throw new NotImplementedException();

        bool IXRSelectInteractor.keepSelectedTargetValid => throw new NotImplementedException();

        HoverEnterEvent IXRHoverInteractor.hoverEntered => throw new NotImplementedException();

        HoverExitEvent IXRHoverInteractor.hoverExited => throw new NotImplementedException();

        List<IXRHoverInteractable> IXRHoverInteractor.interactablesHovered => throw new NotImplementedException();

        bool IXRHoverInteractor.hasHover => throw new NotImplementedException();

        bool IXRHoverInteractor.isHoverActive => throw new NotImplementedException();

        InteractionLayerMask IXRInteractor.interactionLayers => throw new NotImplementedException();

        InteractorHandedness IXRInteractor.handedness => throw new NotImplementedException();

        Transform IXRInteractor.transform => tempObj.transform;

        event Action<InteractorRegisteredEventArgs> IXRInteractor.registered
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

        event Action<InteractorUnregisteredEventArgs> IXRInteractor.unregistered
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

        #region HPUI methods
        bool IHPUIInteractor.GetDistanceInfo(IHPUIInteractable interactable, out DistanceInfo distanceInfo)
        {
            distanceInfo = new DistanceInfo();
            return true;
        }
        #endregion

        #region XRI methods
        bool IXRHoverInteractor.CanHover(IXRHoverInteractable interactable)
        {
            throw new NotImplementedException();
        }

        bool IXRSelectInteractor.CanSelect(IXRSelectInteractable interactable)
        {
            throw new NotImplementedException();
        }

        Pose IXRSelectInteractor.GetAttachPoseOnSelect(IXRSelectInteractable interactable)
        {
            throw new NotImplementedException();
        }

        Transform IXRInteractor.GetAttachTransform(IXRInteractable interactable)
        {
            throw new NotImplementedException();
        }

        Pose IXRSelectInteractor.GetLocalAttachPoseOnSelect(IXRSelectInteractable interactable)
        {
            throw new NotImplementedException();
        }

        void IXRInteractor.GetValidTargets(List<IXRInteractable> targets)
        {
            throw new NotImplementedException();
        }

        bool IXRHoverInteractor.IsHovering(IXRHoverInteractable interactable)
        {
            throw new NotImplementedException();
        }

        bool IXRSelectInteractor.IsSelecting(IXRSelectInteractable interactable)
        {
            throw new NotImplementedException();
        }

        void IXRHoverInteractor.OnHoverEntered(HoverEnterEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRHoverInteractor.OnHoverEntering(HoverEnterEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRHoverInteractor.OnHoverExited(HoverExitEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRHoverInteractor.OnHoverExiting(HoverExitEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRInteractor.OnRegistered(InteractorRegisteredEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRSelectInteractor.OnSelectEntered(SelectEnterEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRSelectInteractor.OnSelectEntering(SelectEnterEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRSelectInteractor.OnSelectExited(SelectExitEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRSelectInteractor.OnSelectExiting(SelectExitEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRInteractor.OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            throw new NotImplementedException();
        }

        void IXRInteractor.PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            throw new NotImplementedException();
        }

        void IXRInteractor.ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
