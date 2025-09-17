using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// The selection valid targets in <see cref="HPUIInteractor"/> is done thorugh this interaface.
    /// </summary>
    public interface IHPUIDetectionLogic: IDisposable
    {
        /// <summary>
        /// The interaction hover radius when detecting interactions.
        /// </summary>
        public float InteractionHoverRadius { get; set; }

        /// <summary>
        /// Computes and returns a dictionary of iteractables and corresponding interaction data. This data is passed to <see cref="IHPUIGestureLogic"/>.
        /// </summary>
        public void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint);

        /// <summary>
        /// Resets/initializes the logic.
        /// </summary>
        public void Reset();
    }
}
