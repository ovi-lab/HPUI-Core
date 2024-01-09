using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction 
{
    public interface IHPUIInteractable : IXRInteractable, IXRSelectInteractable
    {
        /// <summary>
        /// Lower z order will get higher priority.
        /// </summary>
        int zOrder { get; set; }

        /// <summary>
        /// Get the projection of the interactors position on the xz plane of this interactable, normalized.
        /// the returned Vector2 - (x, z) on the xz-plane.
        /// (0, 0) would be the bounds min on the surface & (1, 1) the bounds max on the surface.
        /// </summary>
        Vector2 ComputeInteractorPostion(IXRInteractor interactor);

        /// <summary>
        /// This is called when a tap event occurs on the interactable.
        /// </summary>
        void OnTap(HPUITapEventArgs args);

        /// <summary>
        /// Indicates if this handles gesture. If not, if given gesture 
        /// happens while this interactable is selected, it'll be passed to
        /// the next selected interactable in the priority list.
        /// </summary>
        bool HandlesGestureState(HPUIGestureState state);

        /// <summary>
        /// This is called when a swipe event occurs on the interactable.
        /// </summary>
        void OnSwipe(HPUISwipeEventArgs args);
    }

}
