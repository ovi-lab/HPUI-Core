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
        public void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, InteractionInfo> validTargets, out Vector3 hoverEndPoint);
    }

    // TODO: docs
    public struct InteractionInfo
    {
        public float distance;
        public Vector3 point;
        public Collider collider;
        public float heuristic;
        public float extra;
        public bool selectionCheck;

        public InteractionInfo(float distance, Vector3 point, Collider collider, float heuristic=0, float extra=0, bool selectionCheck=false) : this()
        {
            this.distance = distance;
            this.point = point;
            this.collider = collider;
            this.heuristic = heuristic;
            this.extra = extra;
            this.selectionCheck = selectionCheck;
        }
    }
}
