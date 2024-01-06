using System;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Core
{
    public enum HPUIGestureState
    {
        Tap, Swipe,
        Custom // TODO: Custom gestures?
    }

    #region events classes
    public class HPUIGestureEvent: UnityEvent<HPUIGestureEventArgs>
    {}

    /// <summary>
    /// Event data associated with an gesture interaction on HPUI
    /// </summary>
    public class HPUIGestureEventArgs: BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IXRSelectInteractor interactorObject
        {
            get => (IXRSelectInteractor)base.interactorObject;
            set => base.interactorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IHPUIInteractable interactableObject
        {
            get => (IHPUIInteractable)base.interactableObject;
            set => base.interactableObject = value;
        }
    }

    [Serializable]
    public class HPUITapEvent: UnityEvent<HPUITapEventArgs>
    {}

    /// <summary>
    /// Event data associated with an tap gesture interaction on HPUI
    /// </summary>
    public class HPUITapEventArgs: HPUIGestureEventArgs
    {}

    [Serializable]
    public class HPUISwipeEvent: UnityEvent<HPUISwipeEventArgs>
    {}

    /// <summary>
    /// Event data associated with an swipe gesture interaction on HPUI
    /// </summary>
    public class HPUISwipeEventArgs: HPUIGestureEventArgs
    {}
    #endregion
}
