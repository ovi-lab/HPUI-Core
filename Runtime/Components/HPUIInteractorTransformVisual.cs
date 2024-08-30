using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Components
{
    public class HPUIInteractorTransformVisual: MonoBehaviour
    {
        [SerializeField, Tooltip("The line renderer object to manage")]
        private Transform visualTransform;

        [SerializeField, Tooltip("The target interactor for event to subscribe to")]
        private HPUIInteractor hpuiInteractor;

        /// <inheritdoc />
        private void OnEnable()
        {
            if (hpuiInteractor == null)
            {
                hpuiInteractor = GetComponent<HPUIInteractor>();
            }

            if (hpuiInteractor != null)
            {
                hpuiInteractor.HoverUpdateEvent.AddListener(OnHoverUpdate);
                hpuiInteractor.hoverEntered.AddListener(OnHoverEntered);
                hpuiInteractor.hoverExited.AddListener(OnHoverExited);
            }
            visualTransform?.gameObject.SetActive(true);
        }

        /// <inheritdoc />
        private void OnDisable()
        {
            if (hpuiInteractor != null)
            {
                hpuiInteractor.HoverUpdateEvent.RemoveListener(OnHoverUpdate);
                hpuiInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                hpuiInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
            visualTransform?.gameObject.SetActive(false);
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.HoverUpdateEvent"/>
        /// </summary>
        private void OnHoverUpdate(HPUIHoverUpdateEventArgs arg)
        {
            visualTransform.position = arg.hoverPoint;
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.hoverEntered"/>
        /// </summary>
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (visualTransform != null)
            {
                visualTransform.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.hoverExited"/>
        /// </summary>
        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (visualTransform != null)
            {
                visualTransform.gameObject.SetActive(false);
            }
        }
    }
}
