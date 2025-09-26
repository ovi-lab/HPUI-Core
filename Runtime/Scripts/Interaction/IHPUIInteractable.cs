using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    public interface IHPUIInteractable : IXRSelectInteractable, IXRHoverInteractable
    {
        /// <summary>
        /// Event triggered on gesture
        /// </summary>
        public HPUIGestureEvent GestureEvent { get; }

        /// <summary>
        /// Auxiliary gesture realted event triggered on gesture
        /// </summary>
        public HPUIInteractableStateEvent AuxGestureEvent { get; }

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
        /// If the interactor is not hovering, will return false.
        /// the returned Vector2 - (x, z) on the xz-plane, relative to the center of the
        /// interactable in Unity units.
        /// </summary>
        bool ComputeInteractorPosition(IHPUIInteractor interactor, out Vector2 position);

        /// <summary>
        /// Indicates if this handles gesture. If not, if given gesture 
        /// happens while this interactable is selected, it'll be passed to
        /// the next selected interactable in the priority list.
        /// </summary>
        bool HandlesGesture();

        /// <summary>
        /// This is called when a gesture event occurs on the interactable.
        /// </summary>
        void OnGesture(HPUIGestureEventArgs args);

        /// <summary>
        /// This is called when an auxiliary gesture event occurs on the interactable.
        /// </summary>
        void OnInteractableStateEvent(HPUIInteractableStateEventArgs args);
    }
}
