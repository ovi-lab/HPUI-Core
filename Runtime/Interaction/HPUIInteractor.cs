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
        private HPUISwipeEvent swipeEvent = new HPUISwipeEvent();

        /// <summary>
        /// Event triggered on swipe
        /// </summary>
        public HPUISwipeEvent SwipeEvent { get => swipeEvent; set => swipeEvent = value; }


        protected HPUIGestureLogic gestureLogic;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
            gestureLogic = new HPUIGestureLogic(this, TapTimeThreshold, TapDistanceThreshold);
        }

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
            recievedTargets.AddRange(targets);

            targets.Clear();
            foreach(IXRInteractable target in recievedTargets.Select(t => t as IHPUIInteractable).Where(ht => ht != null).OrderBy(ht => ht.zOrder))
            {
                targets.Add(target);
            }
            ListPool<IXRInteractable>.Release(recievedTargets);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return ProcessSelectFilters(interactable);
        }


        #region IHPUIInteractor interface
        /// <inheritdoc />
        public void OnTap(HPUITapEventArgs args)
        {
            tapEvent?.Invoke(args);
        }

        /// <inheritdoc />
        public void OnSwipe(HPUISwipeEventArgs args)
        {
            swipeEvent?.Invoke(args);
        }
        #endregion
    }
}
 
