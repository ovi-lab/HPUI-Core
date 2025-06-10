using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Components
{
    /// <summary>
    /// Calibrates a new set of cone ray cone ray angles
    /// to be used for <see cref="HPUIInteractor.DetectionLogic"/>
    /// Works similar to <see cref="OnGestureConeRayEstimator"/> except it
    /// uses multiple frames to estimate an average length
    /// of interaction per ray. Makes use of <see cref="HPUIInteractorConeRayAngleSegment"/>
    /// from <see cref="OnGestureConeRayEstimator"/> for the list of phalanges.
    /// </summary>
    public class GuidedConeRayEstimator: ConeRayEstimator
    {
        private bool isCalibrationActive = false;

        public bool IsCalibrationActive { get => isCalibrationActive; set => isCalibrationActive = value; }

        public GuidedConeRayEstimator(HPUIInteractor interactor) : base(interactor)
        { }

        /// <summary>
        /// The callback used with the interactable gesture event to track the events.
        /// </summary>
        public void EndCalibrationForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            interactionRecords.Add(new InteractionDataRecord(currentInteractionData, segment));

            currentInteractionData = new();
        }

        /// <inheritdoc />
        protected override List<HPUIInteractorRayAngle> EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment)
        {
            Dictionary<(float, float), float> averageRayDistance = new();
            // For each interaction, get the frame with the shortest distance
            foreach (InteractionDataRecord interactionRecord in interactionRecords)
            {
                if (interactionRecord.segment == segment)
                {
                    // Collect all the distances for a given ray
                    Dictionary<(float, float), List<float>> rayDistances = new();
                    // for each frame in all the frames collected in a gesture
                    foreach (var frame in interactionRecord.records)
                    {
                        // for each ray in a given frame
                        foreach (var ray in frame)
                        {
                            // if a list hasn't been created for a ray
                            // i.e. this is the first time the ray is interacting
                            // in this gesture
                            if (!rayDistances.ContainsKey((ray.angleX, ray.angleZ)))
                            {
                                rayDistances[(ray.angleX, ray.angleZ)] = new List<float>();
                            }
                            // add the ray to the list
                            rayDistances[(ray.angleX, ray.angleZ)].Add(ray.distance);
                        }
                    }

                    foreach (var ray in rayDistances)
                    {
                        if (ray.Value.Count > 10) //TODO: Remove magic number!
                        {
                            averageRayDistance[(ray.Key.Item1, ray.Key.Item2)] = ray.Value.Average();
                        }
                    }
                }
            }

            if (averageRayDistance.Count() == 0)
            {
                return new List<HPUIInteractorRayAngle>();
            }
            List<HPUIInteractorRayAngle> coneAnglesForSegment = new();

            foreach (var ray in averageRayDistance)
            {
                coneAnglesForSegment.Add(new HPUIInteractorRayAngle(ray.Key.Item1, ray.Key.Item2, ray.Value));
            }

            return coneAnglesForSegment;
        }
    }
}
