using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Collect data for each gesture.  Determins the segment for a given interaction based on the
    /// mapping provided in <see cref="InteractableToSegmentMapping"/>.
    /// </summary>
    /// <seealso cref="IHPUIInteractable.GestureEvent"/>
    public class OnGestureDataCollector : ConeRayDataCollectorBase
    {
        [Serializable]
        /// <summary>
        /// Container that associates an <see cref="HPUIBaseInteractable"/> with a
        /// <see cref="HPUIInteractorConeRayAngleSegment"/>.
        /// </summary>
        public struct ConeRayAnglesEstimationPair
        {
            public HPUIBaseInteractable interactable;
            public HPUIInteractorConeRayAngleSegment segment;
        }

        [SerializeField, Tooltip("Interactable segment pairs.")]
        private List<ConeRayAnglesEstimationPair> interactableToSegmentMapping = new();

        /// <summary>
        /// List of interactable to segment mappaing pairs. The list is expected to have all segments.
        /// </summary>
        public List<ConeRayAnglesEstimationPair> InteractableToSegmentMapping { get => interactableToSegmentMapping; set => interactableToSegmentMapping = value; }

        [SerializeField, Tooltip("If not true, interactableSegmentPairs should have atleast one entry for each")]
        private bool ignoreMissingSegments = false;

        /// <summary>
        /// If not true, interactableSegmentPairs should have atleast one entry for each
        /// </summary>
        public bool IgnoreMissingSegments { get => ignoreMissingSegments; set => ignoreMissingSegments = value; }

        /// <inheritdoc />
        public override bool StartDataCollection()
        {
            bool retval = base.StartDataCollection();
            if (!ignoreMissingSegments && InteractableToSegmentMapping.Select(el => el.segment).Distinct().Count() != Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).Length)
            {
                throw new ArgumentException("Expecting all segments in interactableToSegmentMapping");
            }

            foreach (IHPUIInteractable interactable in InteractableToSegmentMapping.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.AddListener(OnGestureCallback);
            }
            return retval;
        }

        /// <inheritdoc />
        public override bool StopDataCollection()
        {
            foreach (IHPUIInteractable interactable in InteractableToSegmentMapping.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.RemoveListener(OnGestureCallback);
            }

            return base.StopDataCollection();
        }

        /// <summary>
        /// The callback used with the interactable gesture event to track the events.
        /// </summary>
        protected void OnGestureCallback(HPUIGestureEventArgs args)
        {
            if (args.State == HPUIGestureState.Stopped)
            {
                foreach(ConeRayAnglesEstimationPair pair in InteractableToSegmentMapping)
                {
                    if ((IHPUIInteractable)pair.interactable == args.interactableObject)
                    {
                        DataRecords.Add(new ConeRayComputationDataRecord(currentInteractionData, pair.segment));
                    }
                }
                currentInteractionData = new();
            }
        }
    }
}
