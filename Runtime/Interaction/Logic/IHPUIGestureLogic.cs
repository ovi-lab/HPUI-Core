using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIGestureLogic: IDisposable
    {
        /// <summary>
        /// Update method to be called from an interactor. Updates the states of the <see cref="IHPUIInteractable"/> selected by
        /// the <see cref="IXRInteractor"/> that was passed when initializing this <see cref="HPUIGestureLogic"/>.
        /// <param name="interactor">The interactor to use when processing the distances.</param>
        /// <param name="distances">A dictionary containing the distance and heuristic values for interactable currently interacting with.</param>
        /// </summary>
        public void Update(IHPUIInteractor interactor, IDictionary<IHPUIInteractable, HPUIInteractionInfo> distances);

        /// <summary>
        /// Returns the interactable passed which has the highest priority.
        /// </summary>
        public bool IsPriorityTarget(IHPUIInteractable interactable);

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
