using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Collects raycast-related data based on hand joint locations and finger sides for a set of configured interactables.
    ///
    /// This implementation of <see cref="RaycastDataCollectorBase"/> subscribes to gesture events from the configured
    /// interactables and aggregates the current interaction data into one or more <see cref="ConeRayComputationDataRecord"/>
    /// entries whenever an interactable signals that a gesture has stopped. Each aggregated record is associated with a
    /// specific hand joint and finger side, and mapped to the corresponding <see cref="HPUIInteractorConeRayAngleSegment"/>.
    ///
    /// Typical usage:
    /// - Configure the list of <see cref="HPUIBaseInteractable"/> targets (via the <see cref="Interactables"/> property).
    /// - Call <see cref="StartDataCollection"/> to begin listening to gesture events and collecting data.
    /// - When a gesture stops, collected samples in <c>currentInteractionData</c> are grouped by joint and side,
    ///   converted into <see cref="ConeRayComputationDataRecord"/> instances, and added to <see cref="DataRecords"/>.
    /// - Call <see cref="StopDataCollection"/> to unsubscribe from gesture events and stop collection.
    ///
    /// Notes:
    /// - The actual mapping from (joint, side) to <see cref="HPUIInteractorConeRayAngleSegment"/> is implemented in <see cref="OnGestureCallback"/>.
    /// </summary>
    public class LocationBasedRayDataCollector : RaycastDataCollectorBase
    {
        [SerializeField, Tooltip("The list of interactables whose gesture events this collector listens to.")]
        private List<HPUIBaseInteractable> interactables = new();

        /// <summary>
        /// The list of interactables whose gesture events this collector listens to.
        /// </summary>
        public List<HPUIBaseInteractable> Interactables { get => interactables; set => interactables = value; }

        private XRHandJointID closestJoint;
        private FingerSide closestSide;

        /// <summary>
        /// Starts data collection by subscribing to gesture events on all configured interactables.
        /// </summary>
        /// <returns>
        /// The return value of the base <see cref="RaycastDataCollectorBase.StartDataCollection"/> call. A caller should
        /// check this value to verify that collection was successfully started.
        /// </returns>
        public override bool StartDataCollection()
        {
            bool retval = base.StartDataCollection();

            foreach (IHPUIInteractable interactable in Interactables)
            {
                interactable.GestureEvent.AddListener(OnGestureCallback);
            }
            return retval;
        }

        /// <summary>
        /// Stops data collection by unsubscribing from gesture events on all configured interactables.
        /// </summary>
        /// <returns>
        /// The return value of the base <see cref="RaycastDataCollectorBase.StopDataCollection"/> call.
        /// </returns>
        public override bool StopDataCollection()
        {
            foreach (IHPUIInteractable interactable in Interactables)
            {
                interactable.GestureEvent.RemoveListener(OnGestureCallback);
            }

            return base.StopDataCollection();
        }

        /// <summary>
        /// Callback invoked when an interactable fires a gesture event.
        ///
        /// When a gesture transitions to the <see cref="HPUIGestureState.Stopped"/> state, the method groups the
        /// samples currently saved in <c>currentInteractionData</c> by (hand joint, finger side), maps each group to the
        /// appropriate <see cref="HPUIInteractorConeRayAngleSegment"/>, and appends a new
        /// <see cref="ConeRayComputationDataRecord"/> for each group into <see cref="DataRecords"/>.
        /// After aggregation the local buffer <c>currentInteractionData</c> is cleared.
        /// </summary>
        /// <param name="args">Gesture event arguments providing gesture state information.</param>
        /// <exception cref="ArgumentException">Thrown if a grouped (joint, side) value does not map to a known segment.</exception>
        protected void OnGestureCallback(HPUIGestureEventArgs args)
        {
            if (args.State == HPUIGestureState.Stopped)
            {
                foreach(IGrouping<(XRHandJointID, FingerSide), RaycastDataRecordsContainer> records in currentInteractionData.GroupBy(data => (data.handJointID, data.fingerSide)))
                {
                    HPUIInteractorConeRayAngleSegment segment = HPUIInteractorConeRayAngleSegmentConversion.ToConeRayAngleSegment(records.Key.Item1, records.Key.Item2);

                    DataRecords.Add(new ConeRayComputationDataRecord(records.ToList(), segment));
                }
                currentInteractionData = new();
            }
        }
    }
}
