using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    public interface IHPUIInteractor : IXRSelectInteractor, IXRHoverInteractor
    {
        /// <summary>
        /// Event triggered during gesture.
        /// Avoid holding the reference to the corresponding <see cref="HPUIInteractorGestureEventArgs"/>,
        /// it may get disposed ouside of this event call.
        /// </summary>
        /// <seealso cref="HPUIInteractorGestureEventArgs"/>
        /// <seealso cref="HPUIInteractorGestureEvent"/>
        public HPUIInteractorGestureEvent GestureEvent { get; }

        /// <summary>
        /// Event that triggers with hover strength data.
        /// </summary>
        /// <seealso cref="HPUIHoverUpdateEventArgs"/>
        public HPUIHoverUpdateEvent HoverUpdateEvent { get; }

        /// <summary>
        /// Get the <see cref="DistanceInfo"/> for a given interactable.
        /// </summary>
        bool GetDistanceInfo(IHPUIInteractable interactable, out DistanceInfo distanceInfo);
    }
}
