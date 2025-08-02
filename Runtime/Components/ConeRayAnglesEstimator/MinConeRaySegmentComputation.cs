using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine.Assertions;
using static ubco.ovilab.HPUI.Interaction.HPUIRayCastDetectionBaseLogic;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Computes the cone rays from the frames that have the minimum ray distance in a given <see cref="ConeRayComputationDataRecord"/>.
    /// </summary>
    [Serializable]
    public class MinConeRaySegmentComputation : IConeRaySegmentComputation
    {
        List<HPUIInteractorRayAngle> IConeRaySegmentComputation.EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment, IEnumerable<ConeRayComputationDataRecord> interactionRecords)
        {
            List<HPUIRayCastDetectionBaseLogic.RaycastDataRecord> filteredInteractionRecords = new();

            // For each interaction, get the frame with the shortest distance
            foreach (ConeRayComputationDataRecord interactionRecord in interactionRecords)
            {
                if (interactionRecord.segment == segment)
                {
                    var minDistRaycastRecords = interactionRecord.records.AsParallel()
                        .Select(frameRaycastRecords =>
                        {
                            IEnumerable<RaycastDataRecord> filteredFrameRaycastRecords = frameRaycastRecords.Where(r => r.isSelection);
                            if (filteredFrameRaycastRecords.Count() == 0)
                            {
                                return null;
                            }
                            else
                            {
                                return new
                                {
                                    distance = filteredFrameRaycastRecords.Min(raycastRecord => raycastRecord.distance),
                                    raycastRecordsForFrame = filteredFrameRaycastRecords
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
            if (filteredInteractionRecords.Count() == 0)
            {
                return new List<HPUIInteractorRayAngle>();
            }

            // KLUDGE: Does AsParallel help?
            List<HPUIInteractorRayAngle> coneAnglesForSegment = filteredInteractionRecords.AsParallel()
                .Where(record => record.isSelection)
                .Select(record => new { angle = new HPUIInteractorRayAngle(record.angleX, record.angleZ, 0), distance = record.distance })
                // Since the same detection ray angle asset is used, we assume the x, z pairs are going to match.
                .GroupBy(record => record.angle, (angle, records) => new HPUIInteractorRayAngle(angle.X, angle.Z, records.Select(r => r.distance).Sum() / records.Count()))
                .ToList();

            return coneAnglesForSegment;
        }
    }
}
