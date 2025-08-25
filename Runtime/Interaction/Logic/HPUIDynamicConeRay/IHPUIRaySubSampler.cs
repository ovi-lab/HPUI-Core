using System;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIRaySubSampler : IDisposable
    {
        /// <summary>
        /// Uses the position of the interactor and information from <see cref="HandJointEstimatedData"/> to 
        /// subsample a set of rays from interactor data scriptables such as <see cref="HPUIInteractorFullRangeAngles"/>
        /// or <see cref="HPUIInteractorConeRayAngles"/>
        /// </summary>
        /// <param name="interactorObject">Transform of the interactor</param>
        /// <param name="estimatedData">Current frame <see cref="HandJointEstimatedData"/> information</param>
        /// <returns>Subsampled list of HPUI Interactor Ray Angles</returns>
        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData);
    }
}
