using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;
using System.Linq;
using UnityEngine.XR.Hands;
using System.Threading.Tasks;
using System.Collections;

namespace ubco.ovilab.HPUI.Interaction
{
    public class EstimateConeRayAngles: MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Corresponding Interactor")]
        private HPUIInteractor interactor;

        [SerializeField]
        [Tooltip("Interactable segment pair")]
        private List<ConeRayAnglesEstimatorPair> interactableToSegmentMapping;

        private ConeRayAnglesEstimator estimator;

        public void StartEstimation()
        {
            estimator = new ConeRayAnglesEstimator(interactor, interactableToSegmentMapping.ToDictionary(el => (IHPUIInteractable)el.interactable, el => el.segment));
        }

        public void FinishEstimation(Action<HPUIInteractorConeRayAngles> callback)
        {
            estimator.EstimateConeRayAngles((angles) =>
            {
                interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(interactor.DetectionLogic.InteractionHoverRadius, angles, interactor.GetComponent<XRHandTrackingEvents>());
                callback.Invoke(angles);
            });
        }

        [Serializable]
        public struct ConeRayAnglesEstimatorPair
        {
            public HPUIBaseInteractable interactable;
            public HPUIInteractorConeRayAngleSegments segment;
        }
    }

    public class ConeRayAnglesEstimator
    {
        private HPUIInteractor interactor;
        private Dictionary<IHPUIInteractable, HPUIInteractorConeRayAngleSegments> interactableToSegmentMapping;
        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        private List<InteractionDataRecord> interactionRecords = new();
        private List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();

        public ConeRayAnglesEstimator(HPUIInteractor interactor, Dictionary<IHPUIInteractable, HPUIInteractorConeRayAngleSegments> interactableToSegmentMapping)
        {
            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
            }
            this.interactor = interactor;
            this.interactableToSegmentMapping = interactableToSegmentMapping;
            this.fullRayDetectionLogic = fullRayDetectionLogic;
            this.fullRangeAngles = fullRayDetectionLogic.FullRangeRayAngles;

            fullRayDetectionLogic.raycastData += RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableToSegmentMapping.Keys)
            {
                interactable.GestureEvent.AddListener(OnGestureCallback);
            }

            Assert.AreEqual(interactableToSegmentMapping.Keys.Distinct().Count(), Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegments)).Length, "Expecting all segments in interactableToSegmentMapping");
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
                interactionRecords.Add(new InteractionDataRecord(currentInteractionData, interactableToSegmentMapping[args.interactableObject as IHPUIInteractable]));
                currentInteractionData = new();
            }
        }

        public virtual void EstimateConeRayAngles(Action<HPUIInteractorConeRayAngles> callback)
        {
            HPUIInteractorConeRayAngles estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableToSegmentMapping.Keys)
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

                    List<IHPUIInteractable> validInteractables = interactableToSegmentMapping.Where(kvp => kvp.Value == segment).Select(kvp => kvp.Key).ToList();

                    // For each interaction, get the frame with the shortest distance
                    foreach (InteractionDataRecord interactionRecord in this.interactionRecords)
                    {
                        if (interactionRecord.segment == segment)
                        {
                            List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> minDistRaycastRecords = interactionRecord.records.AsParallel()
                                .Select(records => new {
                                        distance = records.Min(iRecords => iRecords.distance),
                                        frameRecords = records
                                    })
                                .OrderBy(records => records.distance)
                                .First().frameRecords;

                            filteredInteractionRecords.AddRange(minDistRaycastRecords);
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
}
 
