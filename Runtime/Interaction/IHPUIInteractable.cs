using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Interaction 
{
    public interface IHPUIInteractable : IXRSelectInteractable, IXRHoverInteractable
    {
        /// <summary>
        /// Event triggered on tap
        /// </summary>
        public HPUITapEvent TapEvent { get; }

        /// <summary>
        /// Event triggered on gesture
        /// </summary>
        public HPUIGestureEvent GestureEvent { get; }

        /// <summary>
        /// Lower z order will get higher priority.
        /// </summary>
        int zOrder { get; set; }

        /// <summary>
        /// The max point on the surface of the interactable, relative to the center of the object.
        /// Center is the position of transform returned by the <see cref="GetAttachTransform"/>.
        /// </summary>
        Vector2 boundsMax { get; }

        /// <summary>
        /// The min point on the surface of the interactable, relative to the center of the object.
        /// Center is the position of transform returned by the <see cref="GetAttachTransform"/>.
        /// </summary>
        Vector2 boundsMin { get; }

        /// <summary>
        /// Get the projection of the interactor position on the xz plane of this interactable.
        /// the returned Vector2 - (x, z) on the xz-plane, relative to the center of the
        /// interactable in Unity units.
        /// </summary>
        Vector2 ComputeInteractorPosition(IHPUIInteractor interactor);

        /// <summary>
        /// This is called when a tap event occurs on the interactable.
        /// </summary>
        void OnTap(HPUITapEventArgs args);

        /// <summary>
        /// Indicates if this handles gesture. If not, if given gesture 
        /// happens while this interactable is selected, it'll be passed to
        /// the next selected interactable in the priority list.
        /// </summary>
        bool HandlesGesture(HPUIGesture gesture);

        /// <summary>
        /// This is called when a gesture event occurs on the interactable.
        /// </summary>
        void OnGesture(HPUIGestureEventArgs args);
    }

}
