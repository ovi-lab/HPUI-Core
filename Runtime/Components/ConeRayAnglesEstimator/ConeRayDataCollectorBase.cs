using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace ubco.ovilab.HPUI
{
    public abstract class ConeRayDataCollectorBase : MonoBehaviour
    {
        [SerializeField, Tooltip("")]
        private HPUIInteractor interactor;

        /// <summary>
        /// TODO
        /// </summary>
        public HPUIInteractor Interactor { get => interactor; set => interactor = value; }

        public bool CollectingData { get; protected set; }

        private HPUIFullRangeRayCastDetectionLogic fullRayDetectionLogic;
        private HPUIInteractorFullRangeAngles fullRangeAngles;
        protected List<List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord>> currentInteractionData = new();

        public List<ConeRayComputationDataRecord> DataRecords { get; protected set; }

        /// <summary>
        /// TODO:
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
        /// TODO:
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
