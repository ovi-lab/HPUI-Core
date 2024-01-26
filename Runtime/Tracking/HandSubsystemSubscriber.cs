using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// Abstract class which subscribes to the <see cref="XRHandSubsystem"/> to get hand pose data.
    /// <seealso cref="XRHandTrackingEvents"/>
    /// </summary>
    public abstract class HandSubsystemSubscriber: MonoBehaviour
    {
        private XRHandSubsystem handSubsystem;
        protected Transform xrOriginTransform;

        /// <inheritdoc />
        protected virtual void Update()
        {
            if (handSubsystem != null && handSubsystem.running)
            {
                return;
            }

            List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);
            bool foundRunningHandSubsystem = false;
            for (var i = 0; i < handSubsystems.Count; ++i)
            {
                XRHandSubsystem handSubsystem = handSubsystems[i];
                if (handSubsystem.running)
                {
                    UnsubscribeHandSubsystem();
                    this.handSubsystem = handSubsystem;
                    foundRunningHandSubsystem = true;
                    break;
                }
            }

            if (!foundRunningHandSubsystem)
            {
                return;
            }

            SubscribeHandSubsystem();
        }

        /// <inheritdoc />
        protected virtual void OnEnable()
        {
            if (xrOriginTransform == null)
            {
                xrOriginTransform = FindObjectOfType<XROrigin>().transform;
            }

            SubscribeHandSubsystem();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnsubscribeHandSubsystem();
            if (handSubsystem != null)
            {
                handSubsystem = null;
            }
        }

        /// <summary>
        /// Subscribe to events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        protected virtual void SubscribeHandSubsystem()
        {
            if (handSubsystem == null)
                return;

            handSubsystem.updatedHands += OnUpdatedHands;
        }

        /// <summary>
        /// Unsubscribe from events on the <see cref="XRHandSubsystem"/>
        /// </summary>
        protected virtual void UnsubscribeHandSubsystem()
        {
            if (handSubsystem == null)
                return;

            handSubsystem.updatedHands -= OnUpdatedHands;
        }

        private void OnUpdatedHands(XRHandSubsystem subsystem,
                                    XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
                                    XRHandSubsystem.UpdateType updateType)
        {
            switch (updateType)
            {
                case XRHandSubsystem.UpdateType.Dynamic:
                    // Update game logic that uses hand data
                    break;
                case XRHandSubsystem.UpdateType.BeforeRender:
                    ProcessJointData(subsystem);
                    break;
            }
        }

        /// <summary>
        /// Apply data recieved to the transform.
        /// </summary>
        protected abstract void ProcessJointData(XRHandSubsystem subsystem);
    }
}
