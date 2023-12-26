using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core
{
    /// <summary>
    /// Base HPUI interactable.
    /// </summary>
    [RequireComponent(typeof(JointFollower))]
    [RequireComponent(typeof(XRPokeFilter))]
    public class HPUIBaseInteractable: XRSimpleInteractable
    {
        private JointFollower jointFollower;
        private Handedness handedness;
        public Handedness Handedness
        {
            get {
                if (jointFollower == null)
                {
                    jointFollower = GetComponent<JointFollower>();
                }
                handedness = jointFollower.handedness;
                return handedness;
            }
        }
    }
}
