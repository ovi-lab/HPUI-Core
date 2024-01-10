using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Interaction 
{
    public interface IHPUIInteractor: IXRInteractor, IXRSelectInteractor
    {
        /// <summary>
        /// This is called when a tap event occurs on the interactable.
        /// </summary>
        void OnTap(HPUITapEventArgs args);

        /// <summary>
        /// This is called when a gesture event occurs on the interactable.
        /// </summary>
        void OnGesture(HPUIGestureEventArgs args);
    }

}
