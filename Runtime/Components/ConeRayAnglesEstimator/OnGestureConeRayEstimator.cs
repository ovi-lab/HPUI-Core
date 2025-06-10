using System.Collections.Generic;
using System;
using UnityEngine.Assertions;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Components
{
    [Serializable]
    /// <summary>
    /// Container that associates an <see cref="HPUIBaseInteractable"/> with a <see cref="HPUIInteractorConeRayAngleSegment"/>.
    /// </summary>
    public struct ConeRayAnglesEstimationPair
    {
        public HPUIBaseInteractable interactable;
        public HPUIInteractorConeRayAngleSegment segment;
    }

    /// <summary>
    /// Segments of the cone estimation. Corresponds to the fields of <see cref="HPUIInteractorConeRayAngles"/>
    /// </summary>
    public enum HPUIInteractorConeRayAngleSegment
    {
        IndexDistalSegment,
        IndexIntermediateSegment,
        IndexProximalSegment,
        MiddleDistalSegment,
        MiddleIntermediateSegment,
        MiddleProximalSegment,
        RingDistalSegment,
        RingIntermediateSegment,
        RingProximalSegment,
        LittleDistalSegment,
        LittleIntermediateSegment,
        LittleProximalSegment
    }

    /// <summary>
    /// Estimates a new set of cone ray angles to be used for <see cref="HPUIInteractor.DetectionLogic"/>.
    /// </summary>
    /// <example>
    /// The following example does an estimation and save the asset once it is done.
    /// <code>
    /// ConeRayAnglesEstimator estimator = new ConeRayAnglesEstimator(interactor, interactableSegmentPairs);
    /// estimator.EstimateConeRayAngles((asset) => {
    ///     AssetDatabase.CreateAsset(asset, "Assets/NewConeAngles.asset");
    ///     AssetDatabase.SaveAssets();
    /// })
    /// </code>
    /// </example>
    /// <remarks>
    /// This uses <see cref="IHPUIInteractable.GestureEvent"/> to collect the data. Also, the interactor
    /// used is expected to be configured with a <see cref="HPUIFullRangeRayCastDetectionLogic"/>
    /// for <see cref="HPUIInteractor.DetectionLogic"/>. The data is colected by subscribing to
    /// <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>
    /// </remarks>
    /// <seealso cref="EstimateConeRayAngles"/>
    public class OnGestureConeRayEstimator: ConeRayEstimator 
    {
        private List<ConeRayAnglesEstimationPair> interactableSegmentPairs;

        /// <summary>
        /// Instantiates the estimator. This will subscribe to <see cref="HPUIInteractor.DetectionLogic"/>interactor.DetectionLogic</see>
        /// and the <see cref="IHPUIInteractable.GestureEvent">GestureEvent</see> of each
        /// interactable in interactableSegmentPairs.
        /// </summary>
        /// <param name="interactor">
        ///   <see cref="HPUIFullRangeRayCastDetectionLogic"/> for <see cref="HPUIInteractor.DetectionLogic"/>.
        ///   If not configured as such, a ArgumentException will be thrown.
        /// </param>
        /// <param name="interactableSegmentPairs">
        ///   List of <see cref="ConeRayAnglesEstimationPair"/>.
        /// </param>
        /// <param name="ignoreMissingSegments">
        ///   If not true, interactableSegmentPairs should have atleast one entry for each
        ///   <see cref="HPUIInteractorConeRayAngleSegment"/>.
        /// </param>
        public OnGestureConeRayEstimator(HPUIInteractor interactor, List<ConeRayAnglesEstimationPair> interactableSegmentPairs, bool ignoreMissingSegments=false): base(interactor)
        {
            if (!ignoreMissingSegments && interactableSegmentPairs.Select(el => el.segment).Distinct().Count() != Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).Length)
            {
                throw new ArgumentException("Expecting all segments in interactableToSegmentMapping");
            }

            this.interactableSegmentPairs = interactableSegmentPairs;

            foreach (IHPUIInteractable interactable in interactableSegmentPairs.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.AddListener(OnGestureCallback);
            }
        }

        /// <summary>
        /// The callback used with the interactable gesture event to track the events.
        /// </summary>
        protected void OnGestureCallback(HPUIGestureEventArgs args)
        {
            if (args.State == HPUIGestureState.Stopped)
            {
                foreach(ConeRayAnglesEstimationPair pair in interactableSegmentPairs)
                {
                    if ((IHPUIInteractable)pair.interactable == args.interactableObject)
                    {
                        interactionRecords.Add(new InteractionDataRecord(currentInteractionData, pair.segment));
                    }
                }
                currentInteractionData = new();
            }
        }

        /// <inheritdoc />
        public override void EstimateConeRayAngles(Action<HPUIInteractorConeRayAngles> callback)
        {
            foreach (IHPUIInteractable interactable in interactableSegmentPairs.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.RemoveListener(OnGestureCallback);
            }

            base.EstimateConeRayAngles(callback);
        }

        /// <inheritdoc />
        protected override List<HPUIInteractorRayAngle> EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> filteredInteractionRecords = new();

            List<IHPUIInteractable> validInteractables = interactableSegmentPairs.Where(pair => pair.segment == segment).Select(pair => pair.interactable as IHPUIInteractable).ToList();

            // For each interaction, get the frame with the shortest distance
            foreach (InteractionDataRecord interactionRecord in interactionRecords)
            {
                if (interactionRecord.segment == segment)
                {
                    var minDistRaycastRecords = interactionRecord.records.AsParallel()
                        .Select(frameRaycastRecords =>
                        {
                            var filteredRecords = frameRaycastRecords.Where(raycastRecord => validInteractables.Contains(raycastRecord.interactable));
                            if (filteredRecords.Count() == 0)
                            {
                                return null;
                            }
                            else
                            {
                                return new
                                {
                                    distance = filteredRecords.Min(raycastRecord => raycastRecord.distance),
                                    raycastRecordsForFrame = frameRaycastRecords
                                };
                            }
                        })
                        .Where(frameRaycastRecords => frameRaycastRecords != null);

                    Assert.IsTrue(minDistRaycastRecords.Count() > 0, "There's an interaction where no rays had interacted with any of the expected targets. Something has gone wrong!");

                    filteredInteractionRecords.AddRange(minDistRaycastRecords
                                                        .OrderBy(frameRaycastRecords => frameRaycastRecords.distance)
                                                        .First()
                                                        .raycastRecordsForFrame);
                }
            }
            if (filteredInteractionRecords.Count() == 0)
            {
                return new List<HPUIInteractorRayAngle>();
            }

            // KLUDGE: Does AsParallel help?
            List<HPUIInteractorRayAngle> coneAnglesForSegment = filteredInteractionRecords.AsParallel()
                .Where(record => validInteractables.Contains(record.interactable) && record.isSelection)
                .Select(record => new { angle = new HPUIInteractorRayAngle(record.angleX, record.angleZ, 0), distance = record.distance })
                // Since the same detection ray angle asset is used, we assume the x, z pairs are going to match.
                .GroupBy(record => record.angle, (angle, records) => new HPUIInteractorRayAngle(angle.X, angle.Z, records.Select(r => r.distance).Sum() / records.Count()))
                .ToList();

            return coneAnglesForSegment;
        }
    }
}
 
