using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine.XR.Hands;
using Unity.XR.CoreUtils;
using UnityEngine.Pool;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Components
{
    public class HPUIInteractorLRVisual: MonoBehaviour
    {
        [SerializeField, Tooltip("The line renderer object to manage")]
        private LineRenderer lineRenderer;

        [SerializeField, Tooltip("The target interactor for event to subscribe to")]
        private HPUIInteractor hpuiInteractor;

        /// <inheritdoc />
        private void OnEnable()
        {
            if (hpuiInteractor == null)
            {
                hpuiInteractor = GetComponent<HPUIInteractor>();
            }

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            if (hpuiInteractor != null)
            {
                hpuiInteractor.HoverUpdateEvent.AddListener(OnHoverUpdate);
                hpuiInteractor.hoverEntered.AddListener(OnHoverEntered);
                hpuiInteractor.hoverExited.AddListener(OnHoverExited);
            }
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
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.HoverUpdateEvent"/>
        /// </summary>
        private void OnHoverUpdate(HPUIHoverUpdateEventArgs arg)
        {
            lineRenderer.SetPosition(0, arg.attachPoint);
            lineRenderer.SetPosition(1, arg.hoverPoint);
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.hoverEntered"/>
        /// </summary>
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Callback for <see cref="HPUIInteractor.hoverExited"/>
        /// </summary>
        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
