using System;

namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIGestureLogic: IDisposable
    {
        /// <summary>
        /// To be called by <see cref="IXRSelectInteractor.OnSelectEntering"/> controlling this <see cref="HPUIGestureLogic"/>
        /// </summary>
        public void OnHoverEntering(IHPUIInteractable interactable);

        /// <summary>
        /// To be called by <see cref="IXRSelectInteractor.OnSelectExiting"/> controlling this <see cref="HPUIGestureLogic"/>
        /// </summary>
        public void OnHoverExiting(IHPUIInteractable interactable);

        /// <summary>
        /// Update method to be called from an interactor. Updates the states of the <see cref="IHPUIInteractable"/> selected by
        /// the <see cref="IXRInteractor"/> that was passed when initializing this <see cref="HPUIGestureLogic"/>.
        /// </summary>
        public void Update();

        /// <summary>
        /// Returns the interactable passed is has the highest priority.
        /// </summary>
        public bool IsPriorityTarget(IHPUIInteractable interactable);
    }
}
