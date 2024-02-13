using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactor.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIInteractor: XRPokeInteractor, IHPUIInteractor
    {
        public new Handedness handedness;

        // TODO move these to an asset?
        [SerializeField]
        private float tapTimeThreshold;
        public float TapTimeThreshold { get => tapTimeThreshold;  set => tapTimeThreshold = value; }

        [SerializeField]
        private float tapDistanceThreshold;
        public float TapDistanceThreshold { get => tapDistanceThreshold; set => tapDistanceThreshold = value; }

        [SerializeField]
        private HPUITapEvent tapEvent = new HPUITapEvent();

        /// <summary>
        /// Event triggered on tap
        /// </summary>
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        private HPUIGestureEvent gestureEvent = new HPUIGestureEvent();

        /// <summary>
        /// Event triggered on gesture
        /// </summary>
        public HPUIGestureEvent GestureEvent { get => gestureEvent; set => gestureEvent = value; }

        protected IHPUIGestureLogic gestureLogic;
        private List<IXRInteractable> validTargets = new List<IXRInteractable>();

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold);
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        protected void OnValidate()
        {
            // When values are changed in inspector, update the values
            if (gestureLogic != null)
            {
                gestureLogic.Dispose();
            }
            gestureLogic = new HPUIGestureLogicUnified(this, TapTimeThreshold, TapDistanceThreshold);
        }
#endif

        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);
            gestureLogic.OnSelectEntering(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            gestureLogic.OnSelectExiting(args.interactableObject as IHPUIInteractable);
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                gestureLogic.Update();
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            base.GetValidTargets(targets);
            if (handedness == Handedness.Invalid)
            {
                throw new InvalidOperationException("handedness not correcly set.");
            }

            List<IXRInteractable> recievedTargets = ListPool<IXRInteractable>.Get();
            recievedTargets.AddRange(targets.Distinct());

            targets.Clear();
            validTargets.Clear();
            foreach(IXRInteractable target in recievedTargets.Select(t => t as IHPUIInteractable).Where(ht => ht != null).OrderBy(ht => ht.zOrder))
            {
                targets.Add(target);
                validTargets.Add(target);
            }

            // TODO check if an interactable with lower z order is selected. If so cancel it.

            ListPool<IXRInteractable>.Release(recievedTargets);
        }

        // NOTE: PokeInteractor has a bug where it doesn't account for the re-prioritization.
        // See: https://forum.unity.com/threads/xrpokeinteractor-m_currentpoketarget-not-respecting-getvalidtargets-and-target-filters.1534039/#post-9571063
        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return validTargets.Contains(interactable) && ProcessSelectFilters(interactable);
        }


        #region IHPUIInteractor interface
        /// <inheritdoc />
        public void OnTap(HPUITapEventArgs args)
        {
            tapEvent?.Invoke(args);
        }

        /// <inheritdoc />
        public void OnGesture(HPUIGestureEventArgs args)
        {
            gestureEvent?.Invoke(args);
        }
        #endregion
    }
}
 
