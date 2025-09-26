using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    public interface IHPUIGestureLogic : IDisposable
    {
        /// <summaryinteractableEvents>
        /// Update method to be called from an interactor. Updates the states of the <see cref="IHPUIInteractable"/> selected by
        /// the <see cref="IHPUIInteractor"/>.
        /// <param name="interactor">The interactor to use when processing the distances.</param>
        /// <param name="distances">A dictionary containing the distance and heuristic values for interactable currently interacting with.</param>
        /// <param name="interactableEvents">The dictionary that is populated with the interactables and the corresponding events args to be fired.</param>
        /// <param name="interactableEvents">The dictionary that is populated with the interactables and the corresponding auxiliary events args to be fired.</param>
        /// </summary>
        public HPUIInteractorGestureEventArgs ComputeInteraction(IHPUIInteractor interactor,
                                                                 IDictionary<IHPUIInteractable, HPUIInteractionInfo> distances,
                                                                 IDictionary<IHPUIInteractable, HPUIGestureEventArgs> interactableEvents,
                                                                 IDictionary<IHPUIInteractable, HPUIInteractableStateEventArgs> auxInteractableEvents);

        /// <summary>
        /// Resets/initializes the logic.
        /// </summary>
        public void Reset();
    }

    /// <summary>
    /// Struct used to pass distance and heuristic data to logic
    /// </summary>
    public struct HPUIInteractionInfo
    {
        public float heuristic;
        public bool isSelection;
        public Vector3 point;
        public Collider collider;
        public float distanceValue;
        public object extra;

        public HPUIInteractionInfo(float heuristic, bool isSelection, Vector3 point, Collider collider, float distanceValue, object extra) : this()
        {
            this.heuristic = heuristic;
            this.isSelection = isSelection;
            this.point = point;
            this.collider = collider;
            this.distanceValue = distanceValue;
            this.extra = extra;
        }
    }
}
