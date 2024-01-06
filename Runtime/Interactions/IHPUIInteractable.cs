using ubco.ovilab.HPUI.Core;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public interface IHPUIInteractable : IXRInteractable
    {
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
        /// This is called when a swipe event occurs on the interactable.
        /// </summary>
        void OnSwipe(HPUISwipeEventArgs args);
    }

}
