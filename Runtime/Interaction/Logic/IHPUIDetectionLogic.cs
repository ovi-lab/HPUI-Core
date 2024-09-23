using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction
{
    // TODO: docs
    public interface IHPUIDetectionLogic: IDisposable
    {
        /// <summary>
        /// Interaction hover radius.
        /// </summary>
        public float InteractionHoverRadius { get; set; }

        /// <summary>
        /// Computes and returs a dictionary of iteractables and corresponding interaction data. This data is passed to <see cref="IHPUIGestureLogic"/>
        /// </summary>
        public void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint);
    }
}
