using System;
using System.Collections.Generic;

namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIDetectInteractables: IDisposable
    {
        /// <summary>
        /// Computes and returs a dictionary of iteractables and corresponding interaction data. This data is passed to <see cref="IHPUIGestureLogic"/>
        /// </summary>
        public IDictionary<IHPUIInteractable, HPUIInteractionData> DetectedInteractables();
    }
}
