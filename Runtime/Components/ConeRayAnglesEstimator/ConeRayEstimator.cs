using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI
{
    public class ConeRayEstimator : MonoBehaviour
    {
        /// <summary>
        /// Represents the different states of the cone processing operation.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// The system is ready to start processing.
            /// </summary>
            Ready,

            /// <summary>
            /// The system is currently collecting input data.
            /// </summary>
            CollectingData,

            /// <summary>
            /// The system is currently estimating rays relative to the cone.
            /// </summary>
            EstimatingConeRays
        }

        [SerializeField, Tooltip("The data collector responsible for gathering cone ray data.")]
        private ConeRayDataCollectorBase dataCollector;

        /// <summary>
        /// The data collector responsible for gathering cone ray data.
        /// </summary>
        public ConeRayDataCollectorBase DataCollector { get => dataCollector; set => dataCollector = value; }

        [SerializeReference, SubclassSelector]
        [Tooltip("Handles the computation logic for cone ray segments.")]
        private IConeRaySegmentComputation coneRaySegmentComputation;

        /// <summary>
        /// Handles the computation logic for cone ray segments.
        /// </summary>
        public IConeRaySegmentComputation ConeRaySegmentComputation { get => coneRaySegmentComputation; set => coneRaySegmentComputation = value; }

        [SerializeField, Tooltip("Asset containing the configuration for cone ray angles generated from estimation.")]
        private HPUIInteractorConeRayAngles generatedAsset;

        /// <summary>
        /// Asset containing the configuration for cone ray angles generated from estimation.
        /// </summary>
        public HPUIInteractorConeRayAngles GeneratedAsset { get => generatedAsset; protected set => generatedAsset = value; }

        private HPUIFullRangeRayCastDetectionLogic fullrangeRaycastDetectionLogicReference;

        [Space()]
        [SerializeField, Tooltip("If true, will set the detection logic of interactor to HPUIConeRayCastDetectionLogic with generated asset.")]
        private bool setDetectionLogicOnEstimation = false;

        /// <summary>
        /// If true, sets the detection logic of the interactor to HPUIConeRayCastDetectionLogic using the generated asset after estimation.
        /// </summary>
        public bool SetDetectionLogicOnEstimation { get => setDetectionLogicOnEstimation; set => setDetectionLogicOnEstimation = value; }

        [SerializeField]
        [Tooltip("(optional) The hand tracking event to use with HPUIConeRayCastDetectionLogic if SetDetectionLogicOnEstimation is true." +
                 "If this is not set, then will look for XRHandTrackingEvents in the Interactor.")]
        private XRHandTrackingEvents xrHandTrackingEventsForConeDetection;

        /// <summary>
        /// (Optional) The hand tracking event to use with HPUIConeRayCastDetectionLogic if SetDetectionLogicOnEstimation is true.
        /// If not set, the system will attempt to find XRHandTrackingEvents on the Interactor.
        /// </summary>
        public XRHandTrackingEvents XRHandTrackingEventsForConeDetection { get => xrHandTrackingEventsForConeDetection; set => xrHandTrackingEventsForConeDetection = value; }

        /// <summary>
        /// Represents the current state of the cone ray estimation process.
        /// </summary>
        public State CurrentState { get; private set; }

        /// <summary>
        /// This event gets called after the asset has been generated and assigned to <see cref="GeneratedAsset"/>
        /// </summary>
        public UnityEvent OnConeAssetGenerated;

        /// <summary>
        /// Initiate data collection. If this component was used to generate an asset, and
        /// the detection logic is not a <see cref="HPUIFullRangeRayCastDetectionLogic"/>, the
        /// HPUIInteractorFullRangeAngles before the asset was generated will be set as the
        /// detection logic of the interactor.
        /// </summary>
        public void StartDataCollection()
        {
            Assert.IsTrue(Application.isPlaying, "This doesn't work in editor mode!");

            if (CurrentState != State.Ready)
            {
                Debug.LogWarning($"Current state of estimator is {CurrentState}, Cannot start new procedure.");
                return;
            }

            if (SetDetectionLogicOnEstimation)
            {
                Assert.IsTrue(XRHandTrackingEventsForConeDetection != null || dataCollector.Interactor.GetComponent<XRHandTrackingEvents>() != null,
                              "XRHandTrackingEventsForConeDetection is null and Interactor doesn't have an XRHandTrackingEvents component.");
            }

            if (dataCollector == null)
            {
                throw new ArgumentException("DataCollector not configured.");
            }

            if (coneRaySegmentComputation == null)
            {
                throw new ArgumentException("ConeRaySegmentComputation not configured.");
            }

            if (!(dataCollector.Interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic))
            {
                if (fullrangeRaycastDetectionLogicReference == null)
                {
                    throw new ArgumentException("Expected interactor to be configured with HPUIInteractorFullRangeAngles.");
                }

                dataCollector.Interactor.DetectionLogic = fullrangeRaycastDetectionLogicReference as IHPUIDetectionLogic;
            }
            else
            {
                fullrangeRaycastDetectionLogicReference = dataCollector.Interactor.DetectionLogic as HPUIFullRangeRayCastDetectionLogic;
            }

            if (dataCollector.StartDataCollection())
            {
                CurrentState = State.CollectingData;
            }
            else
            {
                throw new InvalidOperationException("DataCollector failed to start collecting data");
            }
        }

        /// <summary>
        /// Stops the data collection and initiates the estimation. Once done, callback will be invoked with the estimated asset.
        /// </summary>
        /// <remarks>
        /// This will unsubscribe to <see cref="HPUIInteractor.DetectionLogic"/>interactor.DetectionLogic</see>
        /// and the <see cref="IHPUIInteractable.GestureEvent">GestureEvent</see> of each
        /// interactable it is tracking.
        /// </remarks>
        public virtual void EndAndEstimate()
        {
            Assert.IsTrue(Application.isPlaying, "This doesn't work in editor mode!");

            if (CurrentState != State.CollectingData)
            {
                Debug.LogWarning($"Current state of estimator is {CurrentState}, was expecting `CollectingData`. Cannot end and estimate.");
                return;
            }

            if (!dataCollector.StopDataCollection())
            {
                throw new InvalidOperationException("DataCollector failed to stop collecting data");
            }
            IEnumerable<ConeRayComputationDataRecord> dataRecords = dataCollector.DataRecords;
            HPUIInteractorConeRayAngles estimatedConeRayAngles = ScriptableObject.CreateInstance<HPUIInteractorConeRayAngles>();

            StartCoroutine(EstimationCoroutine(estimatedConeRayAngles, dataRecords));
            CurrentState = State.EstimatingConeRays;
        }

        /// <summary>
        /// Coroutine that does the actual work of <see cref="EstimateConeRayAngles"/>.
        /// </summary>
        protected virtual IEnumerator EstimationCoroutine(HPUIInteractorConeRayAngles estimatedConeRayAngles, IEnumerable<ConeRayComputationDataRecord> dataRecords)
        {
            yield return null;
            HPUIInteractorConeRayAngleSegment[] segments = (HPUIInteractorConeRayAngleSegment[])Enum.GetValues(typeof(HPUIInteractorConeRayAngleSegment));

            Task<List<HPUIInteractorRayAngle>>[] tasks = new Task<List<HPUIInteractorRayAngle>>[segments.Length];

            int i = 0;
            foreach (HPUIInteractorConeRayAngleSegment segment in segments)
            {
                IEnumerable<ConeRayComputationDataRecord> filteredDataRecords = dataRecords.Where(r => r.segment == segment);
                tasks[i++] = Task.Run(() => coneRaySegmentComputation.EstimateConeAnglesForSegment(segment, filteredDataRecords));
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

            GeneratedAsset = estimatedConeRayAngles;

            if (SetDetectionLogicOnEstimation)
            {
                dataCollector.Interactor.DetectionLogic = new HPUIConeRayCastDetectionLogic(
                    dataCollector.Interactor.DetectionLogic.InteractionHoverRadius,
                    estimatedConeRayAngles,
                    XRHandTrackingEventsForConeDetection != null ? XRHandTrackingEventsForConeDetection : dataCollector.Interactor.GetComponent<XRHandTrackingEvents>());
            }

            CurrentState = State.Ready;
            OnConeAssetGenerated?.Invoke();
        }
    }
}
