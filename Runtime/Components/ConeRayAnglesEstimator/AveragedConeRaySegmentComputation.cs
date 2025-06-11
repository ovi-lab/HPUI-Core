using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// TODO: docs
    /// </summary>
    [Serializable]
    public class AveragedConeRaySegmentComputation : IConeRaySegmentComputation
    {
        List<HPUIInteractorRayAngle> IConeRaySegmentComputation.EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment, IEnumerable<ConeRayComputationDataRecord> interactionRecords)
        {
            Dictionary<(float, float), float> averageRayDistance = new();
            // For each interaction, get the frame with the shortest distance
            foreach (ConeRayComputationDataRecord interactionRecord in interactionRecords)
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
