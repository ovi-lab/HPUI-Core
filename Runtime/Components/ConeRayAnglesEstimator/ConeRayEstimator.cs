using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ubco.ovilab.HPUI.Components;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace ubco.ovilab.HPUI
{
    public abstract class ConeRayEstimator
    {
        protected HPUIInteractorFullRangeAngles fullRangeAngles;
        protected HPUIInteractor interactor;
        protected HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        protected List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();
        protected List<InteractionDataRecord> interactionRecords = new();

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
        public ConeRayEstimator(HPUIInteractor interactor)
        {
            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
            }

            this.interactor = interactor;
            this.fullRayDetectionLogic = fullRayDetectionLogic;
            this.fullRangeAngles = fullRayDetectionLogic.FullRangeRayAngles;

            fullRayDetectionLogic.raycastData += RaycastDataCallback;
        }

        /// <summary>
        /// The callback used to get the data from the <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>.
        /// </summary>
        protected void RaycastDataCallback(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecords)
        {
            Assert.AreEqual(fullRangeAngles,
                            ((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles,
                            $"Interactor {fullRangeAngles.name} is not the same as {((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles.name}");

            if (raycastDataRecords.Count > 0)
            {
                currentInteractionData.Add(raycastDataRecords);
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
        public virtual void EstimateConeRayAngles(Action<HPUIInteractorConeRayAngles> callback)
        {
            HPUIInteractorConeRayAngles estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;

            interactor.StartCoroutine(EstimationCoroutine(callback, estimatedConeRayAngles));
        }

        /// <summary>
        /// Coroutine that does the actual work of <see cref="EstimateConeRayAngles"/>.
        /// </summary>
        protected virtual IEnumerator EstimationCoroutine(Action<HPUIInteractorConeRayAngles> callback, HPUIInteractorConeRayAngles estimatedConeRayAngles)
        {
            yield return null;
            HPUIInteractorConeRayAngleSegment[] segments = (HPUIInteractorConeRayAngleSegment[])Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment));

            Task<List<HPUIInteractorRayAngle>>[] tasks = new Task<List<HPUIInteractorRayAngle>>[segments.Length];

            int i = 0;
            foreach (HPUIInteractorConeRayAngleSegment segment in segments)
            {
                tasks[i++] = Task.Run(() => EstimateConeAnglesForSegment(segment));
            }

            while (tasks.Any(t => !t.IsCompleted))
            {
                IEnumerable<Exception> exceptions = tasks.Select(t => t.Exception).Where(t => t != null);
                if (exceptions.Count() != 0)
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
            catch (Exception e)
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
        protected abstract List<HPUIInteractorRayAngle> EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment);

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
