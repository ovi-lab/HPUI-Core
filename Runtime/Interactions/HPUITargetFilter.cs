using System;
using System.Collections.Generic;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// Target filter that uses only the respective interactables.
    /// </summary>
    public class HPUITargetFilter : XRBaseTargetFilter
    {
        public Handedness handedness;

        public override void Process(IXRInteractor interactor, List<IXRInteractable> targets, List<IXRInteractable> results)
        {
            if (handedness == Handedness.Invalid)
            {
                throw new InvalidOperationException("handedness not correcly set.");
            }

            results.Clear();
            foreach(IXRInteractable target in targets)
            {
                if (target is HPUIBaseInteractable hpuiTarget && hpuiTarget.Handedness == handedness)
                {
                    results.Add(target);
                }
            }
        }
    }
}
