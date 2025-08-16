using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Base class for collecting data. Implementing classes are expected to appropriately
    /// populate <see cref="DataRecords"/>.
    /// </summary>
    /// <remarks>
    /// The interactor used is expected to be configured with a <see cref="HPUIFullRangeRayCastDetectionLogic"/>
    /// for <see cref="HPUIInteractor.DetectionLogic"/>. The data is collected by subscribing to
    /// <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>
    /// </remarks>
    public abstract class RaycastDataCollectorBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The interactor used to collect RaycastDataRecord data.")]
        private HPUIInteractor interactor;

        /// <summary>
        /// The interactor used to collect <see cref="HPUIRayCastDetectionBaseLogic.RaycastDataRecord"/> data.
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

        [SerializeReference, Tooltip("The estimator to compute closest joint and side.")]
        private HPUIConeRayCastDetectionLogic.ClosestJointAndSideEstimator closestJointAndSideEstimator;

        /// <summary>
        /// The estimator to compute closest joint and side.
        /// </summary>
        public HPUIConeRayCastDetectionLogic.ClosestJointAndSideEstimator ClosestJointAndSideEstimator { get => closestJointAndSideEstimator; set => closestJointAndSideEstimator = value; }

        /// <summary>
        /// The flag indicating if data collection is active.
        /// </summary>
        public bool CollectingData { get; protected set; }

        /// <summary>
        /// The flag indicating if data collection is paused.
        /// This will inhibit <see cref="RaycastDataCallback"/>
        /// </summary>
        public bool PauseDataCollection { get; set; }

        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        protected List<RaycastDataRecordsContainer> currentInteractionData = new();

        public List<ConeRayComputationDataRecord> DataRecords { get; protected set; }

        /// <summary>
        /// This initiates the data collection process.
        /// This will subscribe to <see cref="HPUIInteractor.DetectionLogic"/>interactor.DetectionLogic</see>
        /// and the <see cref="IHPUIInteractable.GestureEvent">GestureEvent</see> of each
        /// interactable in interactableSegmentPairs.
        /// </summary>
        public virtual bool StartDataCollection()
        {
            Assert.IsTrue(Application.isPlaying, "This doesn't work in editor mode!");

            if (CollectingData)
            {
                Debug.LogWarning($"Haven't stopped collecting data.");
                return false;
            }

            if (ClosestJointAndSideEstimator.XRHandTrackingEvents == null)
            {
                Debug.LogError($"The `xrHandTrackingEvents` is not set!");
                return false;
            }

            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
            }

            this.DataRecords = new List<ConeRayComputationDataRecord>();
            this.currentInteractionData = new List<RaycastDataRecordsContainer>();
            this.fullRayDetectionLogic = fullRayDetectionLogic;
            this.fullRangeAngles = fullRayDetectionLogic.FullRangeRayAngles;

            Assert.IsNotNull(ClosestJointAndSideEstimator);
            ClosestJointAndSideEstimator.Reset();

            fullRayDetectionLogic.raycastData += RaycastDataCallback;
            CollectingData = true;
            return true;
        }

        /// <summary>
        /// The callback used to get the data from the <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>.
        /// </summary>
        protected void RaycastDataCallback(HPUIRayCastDetectionBaseLogic detectionLogic, List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> raycastDataRecords)
        {
            if (PauseDataCollection)
            {
                return;
            }

            Assert.AreEqual(fullRangeAngles,
                            ((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles,
                            $"Interactor {fullRangeAngles.name} is not the same as {((HPUIFullRangeRayCastDetectionLogic)interactor.DetectionLogic).FullRangeRayAngles.name}");

            ClosestJointAndSideEstimator.Estimate(out XRHandJointID closestJoint, out FingerSide closestSide);

            if (raycastDataRecords.Count > 0)
            {
                currentInteractionData.Add(new RaycastDataRecordsContainer(raycastDataRecords, closestSide, closestJoint));
            }
        }

        /// <summary>
        /// This terminates the data collection process and unsubscribe relevant callbacks.
        /// </summary>
        public virtual bool StopDataCollection()
        {
            Assert.IsTrue(Application.isPlaying, "This doesn't work in editor mode!");

            if (!CollectingData)
            {
                Debug.LogWarning($"Haven't started collecting data.");
                return false;
            }

            fullRayDetectionLogic.raycastData -= RaycastDataCallback;
            CollectingData = false;
            return true;
        }
    }
}
