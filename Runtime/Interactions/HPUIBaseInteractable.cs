using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    public class HPUIBaseInteractable: XRSimpleInteractable
    {
        [SerializeField]
        private Handedness handedness;
        public Handedness Handedness
        {
            get => handedness;
            set => handedness = value;
        }
    }
}
