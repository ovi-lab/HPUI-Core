using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Assertions;
using System.Linq;
using UnityEngine.XR.Hands;

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

        public HPUIInteractorConeRayAngles FinishEstimation()
        {
            estimator.EstimateConeRayAngles(out HPUIInteractorConeRayAngles angles);
            interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(interactor.DetectionLogic.InteractionHoverRadius, angles, interactor.GetComponent<XRHandTrackingEvents>());
            return angles;
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
                interactable.GestureEvent.AddListener(OnTapCallback);
            }

            Assert.AreEqual(interactableToSegmentMapping.Keys.Distinct().Count(), Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegments)).Length, "Expecting all segments in interactableToSegmentMapping");
        }

        private void RaycastDataCallback(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecords)
        {
            Assert.AreEqual(fullRangeAngles, ((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles);
            currentInteractionData.Add(raycastDataRecords);
        }

        protected virtual void OnTapCallback(HPUIGestureEventArgs args)
        {
            if (args.State == HPUIGestureState.Stopped)
            {
                interactionRecords.Add(new InteractionDataRecord(currentInteractionData, interactableToSegmentMapping[args.interactableObject as IHPUIInteractable]));
                currentInteractionData = new();
                Debug.Log($"------- {args.interactableObject.transform.name}");
            }
        }

        public virtual bool EstimateConeRayAngles(out HPUIInteractorConeRayAngles estimatedConeRayAngles)
        {
            estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            foreach(HPUIInteractorConeRayAngleSegments segment in (HPUIInteractorConeRayAngleSegments[])Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegments)))
            {
                Debug.Log($"------ {segment}");
                List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> filteredInteractionRecords = new();

                List<IHPUIInteractable> validInteractables = interactableToSegmentMapping.Where(kvp => kvp.Value == segment).Select(kvp => kvp.Key).ToList();

                // For each interaction, get the frame with the shortest distance
                foreach(InteractionDataRecord interactionRecord in this.interactionRecords)
                {
                    if (interactionRecord.segment == segment)
                    {
                        float minDistance = float.MaxValue;
                        List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> minDistRaycastRecords = null;

                        foreach(List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastRecords in interactionRecord.records)
                        {
                            foreach(HPUIRayCastDetectionBaseLogic.RaycastDataRecord raycastRecord in raycastRecords)
                            {
                                if (validInteractables.Contains(raycastRecord.interactable) && raycastRecord.distance < minDistance)
                                {
                                    minDistRaycastRecords = raycastRecords;
                                    minDistance = raycastRecord.distance;
                                }
                            }
                        }

                        Assert.IsNotNull(minDistRaycastRecords);
                        filteredInteractionRecords.AddRange(minDistRaycastRecords);
                    }
                }

                Debug.Log($"1---- got {filteredInteractionRecords.Count}");

                // Since the same detection ray angle asset is used, we assume the x, z pairs are going to match.
                Dictionary<HPUIInteractorRayAngle, (float sum, int count)> distances = new();

                foreach(HPUIRayCastDetectionBaseLogic.RaycastDataRecord raycastRecord in filteredInteractionRecords)
                {
                    if (!validInteractables.Contains(raycastRecord.interactable))
                    {
                        continue;
                    }

                    HPUIInteractorRayAngle angle = new HPUIInteractorRayAngle(raycastRecord.angleX, raycastRecord.angleZ, 0);
                    (float sum, int count) data;
                    if (!distances.TryGetValue(angle, out data))
                    {
                        data = (0, 0);
                        distances.Add(angle, data);
                    }

                    data.sum += raycastRecord.distance;
                    data.count++;

                    distances[angle] = data;
                }
                
                Debug.Log($"2---- got {distances.Count}");

                List<HPUIInteractorRayAngle> coneAnglesForSegment = new();

                foreach(KeyValuePair<HPUIInteractorRayAngle, (float sum, int count)> kvp in distances)
                {
                    HPUIInteractorRayAngle angle = kvp.Key;
                    angle.RaySelectionThreshold = kvp.Value.sum / kvp.Value.count;
                    coneAnglesForSegment.Add(angle);
                    Debug.Log($"3--- {angle.X} {angle.Z}  {angle.RaySelectionThreshold}");
                }

                switch(segment)
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

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;

            foreach (IHPUIInteractable interactable in interactableToSegmentMapping.Keys)
            {
                interactable.GestureEvent.RemoveListener(OnTapCallback);
            }
            return false;
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
 
