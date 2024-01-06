using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core 
{
    public interface IHPUIInteractor: IXRInteractor, IXRSelectInteractor
    {
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
