using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
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
    public class ConeRayAnglesEstimator
    {
        private HPUIInteractor interactor;
        private List<ConeRayAnglesEstimationPair> interactableSegmentPairs;
        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        private List<InteractionDataRecord> interactionRecords = new();
        private List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();

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
        public ConeRayAnglesEstimator(HPUIInteractor interactor, List<ConeRayAnglesEstimationPair> interactableSegmentPairs, bool ignoreMissingSegments=false)
        {
            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
            }

            if (!ignoreMissingSegments && interactableSegmentPairs.Select(el => el.segment).Distinct().Count() != Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment)).Length)
            {
                throw new ArgumentException("Expecting all segments in interactableToSegmentMapping");
            }

            this.interactor = interactor;
            this.interactableSegmentPairs = interactableSegmentPairs;
            this.fullRayDetectionLogic = fullRayDetectionLogic;
            this.fullRangeAngles = fullRayDetectionLogic.FullRangeRayAngles;

            fullRayDetectionLogic.raycastData += RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableSegmentPairs.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.AddListener(OnGestureCallback);
            }
        }

        /// <summary>
        /// The callback used to get the data from the <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>.
        /// </summary>
        protected void RaycastDataCallback(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecords)
        {
            Assert.AreEqual(fullRangeAngles, ((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles);
            if (raycastDataRecords.Count > 0)
            {
                currentInteractionData.Add(raycastDataRecords);
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

        /// <summary>
        /// Stops the data collection and initates the estimation. Once done, callback will be invoked with the estimated asset.
        /// </summary>
        /// <remarks>
        /// This will unsubscribe to <see cref="HPUIInteractor.DetectionLogic"/>interactor.DetectionLogic</see>
        /// and the <see cref="IHPUIInteractable.GestureEvent">GestureEvent</see> of each
        /// interactable it is tracking.
        /// </remarks>
        public void EstimateConeRayAngles(Action<HPUIInteractorConeRayAngles> callback)
        {
            HPUIInteractorConeRayAngles estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableSegmentPairs.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.RemoveListener(OnGestureCallback);
            }

            interactor.StartCoroutine(EstimationCoroutine(callback, estimatedConeRayAngles));
        }

        /// <summary>
        /// Coroutine that does the actual work of <see cref="EstimateConeRayAngles"/>.
        /// </summary>
        protected IEnumerator EstimationCoroutine(Action<HPUIInteractorConeRayAngles> callback, HPUIInteractorConeRayAngles estimatedConeRayAngles)
        {
            yield return null;
            HPUIInteractorConeRayAngleSegment[] segments = (HPUIInteractorConeRayAngleSegment[])Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment));

            Task<List<HPUIInteractorRayAngle>>[] tasks = new Task<List<HPUIInteractorRayAngle>>[segments.Length];

            int i = 0;
            foreach (HPUIInteractorConeRayAngleSegment segment in segments)
            {
                tasks[i++] = Task.Run(() => EstimateConeAnglesForSegment(segment, interactionRecords, interactableSegmentPairs));
            }

            while (tasks.Any(t => !t.IsCompleted))
            {
                IEnumerable<Exception> exceptions = tasks.Select(t => t.Exception).Where(t => t != null);
                if(exceptions.Count() != 0)
                {
                    // Throwing the first thing that comes through.
                    // MAYBE: Should all of them be processed somehow?
                    throw exceptions.First();
                }
                yield return null;
            }

            for (i = 0; i < segments.Length; ++i)
            {
                HPUIInteractorConeRayAngleSegment segment = segments[i];
                List<HPUIInteractorRayAngle> coneAnglesForSegment = tasks[i].Result;
                switch (segment)
                {
                    case HPUIInteractorConeRayAngleSegment.IndexDistalSegment:
                        estimatedConeRayAngles.IndexDistalAngles = coneAnglesForSegment;
                        break;
                    case HPUIInteractorConeRayAngleSegment.IndexIntermediateSegment:
                        estimatedConeRayAngles.IndexIntermediateAngles = coneAnglesForSegment;
                        break;
                        case HPUIInteractorConeRayAngleSegment.IndexProximalSegment:
                            estimatedConeRayAngles.IndexProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegment.MiddleDistalSegment:
                            estimatedConeRayAngles.MiddleDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.MiddleIntermediateSegment:
                            estimatedConeRayAngles.MiddleIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.MiddleProximalSegment:
                            estimatedConeRayAngles.MiddleProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegment.RingDistalSegment:
                            estimatedConeRayAngles.RingDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.RingIntermediateSegment:
                            estimatedConeRayAngles.RingIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.RingProximalSegment:
                            estimatedConeRayAngles.RingProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegment.LittleDistalSegment:
                            estimatedConeRayAngles.LittleDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.LittleIntermediateSegment:
                            estimatedConeRayAngles.LittleIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegment.LittleProximalSegment:
                            estimatedConeRayAngles.LittleProximalAngles = coneAnglesForSegment;
                            break;
                }
            }

            try
            {
                callback.Invoke(estimatedConeRayAngles);
            }
            catch(Exception e)
            {
                Debug.LogError($"{e}");
            }
        }

        /// <summary>
        /// For a given segment, computes the List of <see cref="HPUIInteractorRayAngle">.
        /// </summary>
        /// <param name="segment">
        ///   The <see cref="HPUIInteractorConeRayAngleSegment"/> for which cone
        ///   angles are being computed.
        /// </param>
        /// <param name="interactionRecords">
        ///   Each <see cref="InteractionDataRecord"/> in the list is expected to be the data collected during a single gesture event.
        ///   The <see cref="InteractionDataRecord.segment">segment</see> of each record will be based on the interactableSegmentPairs
        ///   passed in the constructor.
        /// </param>
        /// <param name="interactableSegmentPairs">
        ///   The interactableSegmentPairs passed in the constructor.
        /// </param>
        protected virtual List<HPUIInteractorRayAngle> EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment,
                                                                                    List<InteractionDataRecord> interactionRecords,
                                                                                    List<ConeRayAnglesEstimationPair> interactableSegmentPairs)
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
                .Where(record => validInteractables.Contains(record.interactable) && record.isWithinThreshold)
                .Select(record => new { angle = new HPUIInteractorRayAngle(record.angleX, record.angleZ, 0), distance = record.distance })
                // Since the same detection ray angle asset is used, we assume the x, z pairs are going to match.
                .GroupBy(record => record.angle, (angle, records) => new HPUIInteractorRayAngle(angle.X, angle.Z, records.Select(r => r.distance).Sum() / records.Count()))
                .ToList();

            return coneAnglesForSegment;
        }

        /// <summary>
        /// Holds all the data collected for a single gesture event.
        /// </summary>
        protected struct InteractionDataRecord
        {
            public List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records;
            public HPUIInteractorConeRayAngleSegment segment;

            public InteractionDataRecord(List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records, HPUIInteractorConeRayAngleSegment segment) : this()
            {
                this.records = records;
                this.segment = segment;
            }
        }
    }
}
 
