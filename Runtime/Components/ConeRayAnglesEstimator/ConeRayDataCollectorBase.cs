using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Base class for collecting data. Implementing classes are exepcted to appropriately
    /// populate <see cref="DataRecords"/>.
    /// </summary>
    /// <remarks>
    /// The interactor used is expected to be configured with a <see cref="HPUIFullRangeRayCastDetectionLogic"/>
    /// for <see cref="HPUIInteractor.DetectionLogic"/>. The data is collected by subscribing to
    /// <see cref="HPUIFullRangeRayCastDetectionLogic.raycastData"/>
    /// </remarks>
    public abstract class ConeRayDataCollectorBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The interactor used to collect RaycastDataRecord data.")]
        private HPUIInteractor interactor;

        /// <summary>
        /// The interactor used to collect <see cref="HPUIRayCastDetectionBaseLogic.RaycastDataRecord"/> data.
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

        public bool CollectingData { get; protected set; }

        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        protected List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();

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

            if (!(interactor.DetectionLogic is HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic))
            {
                throw new ArgumentException("Interactor is expected to have `HPUIFullRangeRayCastDetectionLogic` as the DetectionLogic.");
            }

            this.DataRecords = new List<ConeRayComputationDataRecord>();
            this.currentInteractionData = new List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>>();
            this.fullRayDetectionLogic = fullRayDetectionLogic;
            this.fullRangeAngles = fullRayDetectionLogic.FullRangeRayAngles;

            fullRayDetectionLogic.raycastData += RaycastDataCallback;
            CollectingData = true;
            return true;
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
