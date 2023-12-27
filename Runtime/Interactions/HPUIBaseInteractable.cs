using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    [RequireComponent(typeof(XRPokeFilter))]
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
