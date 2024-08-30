using System;
using System.Collections.Generic;

namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIGestureLogic: IDisposable
    {
        // /// <summary>
        // /// To be called by <see cref="IXRSelectInteractor.OnSelectEntering"/> controlling this <see cref="HPUIGestureLogic"/>
        // /// </summary>
        // public void InteractableEntering(IHPUIInteractable interactable);

        // /// <summary>
        // /// To be called by <see cref="IXRSelectInteractor.OnSelectExiting"/> controlling this <see cref="HPUIGestureLogic"/>
        // /// </summary>
        // public void InteractableExiting(IHPUIInteractable interactable);

        /// <summary>
        /// Update method to be called from an interactor. Updates the states of the <see cref="IHPUIInteractable"/> selected by
        /// the <see cref="IXRInteractor"/> that was passed when initializing this <see cref="HPUIGestureLogic"/>.
        /// <param name="distances">A dictionary containing the distance and heuristic values for interactable currently interacting with.</param>
        /// </summary>
        public void Update(IDictionary<IHPUIInteractable, HPUIInteractionData> distances);

        /// <summary>
        /// Returns the interactable passed which has the highest priority.
        /// </summary>
        public bool IsPriorityTarget(IHPUIInteractable interactable);
    }

    /// <summary>
    /// Struct used to pass distance and heuristic data to logic
    /// </summary>
    public struct HPUIInteractionData
    {
        public float distance;
        public float heuristic;
        public float extra;

        public HPUIInteractionData(float distance, float heuristic, float extra=0) : this()
        {
            this.distance = distance;
            this.heuristic = heuristic;
            this.extra = extra;
        }

    }
}
