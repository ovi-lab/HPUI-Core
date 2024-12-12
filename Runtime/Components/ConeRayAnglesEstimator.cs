using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public struct ConeRayAnglesEstimationPair
    {
        public HPUIBaseInteractable interactable;
        public HPUIInteractorConeRayAngleSegments segment;
    }

    public enum HPUIInteractorConeRayAngleSegments
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

    public class ConeRayAnglesEstimator
    {
        private HPUIInteractor interactor;
        private List<ConeRayAnglesEstimationPair> interactableSegmentPairs;
        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        private List<InteractionDataRecord> interactionRecords = new();
        private List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();

        public ConeRayAnglesEstimator(HPUIInteractor interactor, List<ConeRayAnglesEstimationPair> interactableSegmentPairs)
        {
            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
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

            Assert.AreEqual(interactableSegmentPairs.Select(el => el.segment).Distinct().Count(),
                            Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegments)).Length,
                            "Expecting all segments in interactableToSegmentMapping");
        }

        private void RaycastDataCallback(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecords)
        {
            Assert.AreEqual(fullRangeAngles, ((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles);
            if (raycastDataRecords.Count > 0)
            {
                currentInteractionData.Add(raycastDataRecords);
            }
        }

        protected virtual void OnGestureCallback(HPUIGestureEventArgs args)
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

        public virtual void EstimateConeRayAngles(Action<HPUIInteractorConeRayAngles> callback)
        {
            HPUIInteractorConeRayAngles estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableSegmentPairs.Select(el => el.interactable).Distinct())
            {
                interactable.GestureEvent.RemoveListener(OnGestureCallback);
            }

            interactor.StartCoroutine(EstimationCoroutine(callback, estimatedConeRayAngles));
        }

        protected virtual IEnumerator EstimationCoroutine(Action<HPUIInteractorConeRayAngles> callback, HPUIInteractorConeRayAngles estimatedConeRayAngles)
        {
            yield return null;
            HPUIInteractorConeRayAngleSegments[] segments = (HPUIInteractorConeRayAngleSegments[])Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegments));

            Task<List<HPUIInteractorRayAngle>>[] tasks = new Task<List<HPUIInteractorRayAngle>>[segments.Length];

            int i = 0;
            foreach (HPUIInteractorConeRayAngleSegments segment in segments)
            {
                tasks[i++] = Task.Run(() =>
                {
                    List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> filteredInteractionRecords = new();

                    List<IHPUIInteractable> validInteractables = interactableSegmentPairs.Where(pair => pair.segment == segment).Select(pair => pair.interactable as IHPUIInteractable).ToList();

                    // For each interaction, get the frame with the shortest distance
                    foreach (InteractionDataRecord interactionRecord in this.interactionRecords)
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

                    List<HPUIInteractorRayAngle> coneAnglesForSegment = filteredInteractionRecords.AsParallel()
                        .Where(record => validInteractables.Contains(record.interactable))
                        .Select(record => new {angle = new HPUIInteractorRayAngle(record.angleX, record.angleZ, 0), distance = record.distance})
                        // Since the same detection ray angle asset is used, we assume the x, z pairs are going to match.
                        .GroupBy(record => record.angle, (angle, records) => new HPUIInteractorRayAngle(angle.X, angle.Z, records.Select(r => r.distance).Sum() / records.Count()))
                        .ToList();

                    return coneAnglesForSegment;
                });

            }

            while (tasks.Any(t => !t.IsCompleted))
            {
                yield return null;
            }

            for (i = 0; i < segments.Length; ++i)
            {
                HPUIInteractorConeRayAngleSegments segment = segments[i];
                List<HPUIInteractorRayAngle> coneAnglesForSegment = tasks[i].Result;
                switch (segment)
                {
                    case HPUIInteractorConeRayAngleSegments.IndexDistalSegment:
                        estimatedConeRayAngles.IndexDistalAngles = coneAnglesForSegment;
                        break;
                    case HPUIInteractorConeRayAngleSegments.IndexIntermediateSegment:
                        estimatedConeRayAngles.IndexIntermediateAngles = coneAnglesForSegment;
                        break;
                        case HPUIInteractorConeRayAngleSegments.IndexProximalSegment:
                            estimatedConeRayAngles.IndexProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegments.MiddleDistalSegment:
                            estimatedConeRayAngles.MiddleDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.MiddleIntermediateSegment:
                            estimatedConeRayAngles.MiddleIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.MiddleProximalSegment:
                            estimatedConeRayAngles.MiddleProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegments.RingDistalSegment:
                            estimatedConeRayAngles.RingDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.RingIntermediateSegment:
                            estimatedConeRayAngles.RingIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.RingProximalSegment:
                            estimatedConeRayAngles.RingProximalAngles = coneAnglesForSegment;
                            break;

                        case HPUIInteractorConeRayAngleSegments.LittleDistalSegment:
                            estimatedConeRayAngles.LittleDistalAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.LittleIntermediateSegment:
                            estimatedConeRayAngles.LittleIntermediateAngles = coneAnglesForSegment;
                            break;
                        case HPUIInteractorConeRayAngleSegments.LittleProximalSegment:
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

        protected struct InteractionDataRecord
        {
            public List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records;
            public HPUIInteractorConeRayAngleSegments segment;

            public InteractionDataRecord(List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> records, HPUIInteractorConeRayAngleSegments segment) : this()
            {
                this.records = records;
                this.segment = segment;
            }
        }
    }
}
 
