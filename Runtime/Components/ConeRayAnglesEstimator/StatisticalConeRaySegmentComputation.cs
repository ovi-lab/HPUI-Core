using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.HPUI
{
    /// <summary>
    /// Computes cone ray angles for a given segment by aggregating distance samples gathered during interaction frames.
    /// For each unique ray direction (identified by its X and Z angles) the implementation collects all selection distances
    /// observed across the provided interaction records, filters rays that do not meet a minimum interaction frequency,
    /// and then produces a single representative distance per ray using the configured statistical technique.
    /// The resulting collection contains HPUIInteractorRayAngle entries (angleX, angleZ, distance) describing the cone
    /// for the segment.
    /// </summary>
    /// <remarks>
    /// Behavior summary:
    /// - Only rays marked as selection are considered.
    /// - A ray must appear in at least MinRayInteractionsThreshold fraction of total frames to be included.
    /// - When EstimateTechnique is Average the mean of the collected distances is used; when Percentile the configured
    ///   Percentile (0.0-1.0) is used (interpolated percentile).
    /// - The per-ray distance is multiplied by Multiplier before being stored in the output.
    /// - If CullRaysByDistanceToCentroid is true, rays are further culled by their angular proximity to the centroid
    ///   direction using CullingDistanceThresholdNormalized (1 = no culling, smaller values remove more rays).
    /// - If interactionRecords contains no matching segment frames or no rays pass the interaction threshold the
    ///   method returns an empty list (and emits a warning in the original implementation when appropriate).
    /// </remarks>
    /// <seealso cref="Estimate"/>
    /// <seealso cref="MinRayInteractionsThreshold"/>
    /// <seealso cref="Percentile"/>
    /// <seealso cref="Multiplier"/>
    /// <seealso cref="CullRaysByDistanceToCentroid"/>
    /// <seealso cref="CullingDistanceThresholdNormalized"/>
    [Serializable]
    public class StatisticalConeRaySegmentComputation : IConeRaySegmentComputation
    {
        public enum Estimate
        {
            /// <summary>
            /// For each ray seen during interactions, use the average
            /// interaction distance from all the data collected.
            /// </summary>
            Average,

            /// <summary>
            /// For each ray seen during interactions, get the nth
            /// percentile distance from all the data collected.
            /// </summary>
            Percentile
        }

        [SerializeField, Tooltip("The statistical estimate to use.")]
        private Estimate estimateTechnique;

        /// <summary>
        /// Gets or sets the statistical estimate technique used to aggregate ray interaction distances.
        /// Changing this value affects how the final cone distance is computed.
        /// </summary>
        /// <seealso cref="Estimate"/>
        public Estimate EstimateTechnique
        {
            get => estimateTechnique;
            set => estimateTechnique = value;
        }

        [SerializeField, Range(0.01f, 1f)]
        [Tooltip("The percentage of frames in the gesture that a ray should have been used to qualify for the final cone")]
        private float minRayInteractionsThreshold = 0.2f;

        /// <summary>
        /// Gets or sets the minimum fraction (0.01 - 1.0) of gesture frames in which a ray must have had interactions
        /// in order to be considered when computing the final cone.
        /// Values are clamped to the valid range [0.01, 1.0].
        /// </summary>
        public float MinRayInteractionsThreshold
        {
            get => minRayInteractionsThreshold;
            set => minRayInteractionsThreshold = Mathf.Clamp(value, 0.01f, 1f);
        }

        [SerializeField, Range(0f, 1f)]
        [Tooltip("When Percentile is used, get the nth percentile distance at which interactions occured for each ray")]
        private float percentile = 0.6f;

        /// <summary>
        /// Gets or sets the percentile (0.0 - 1.0) to use when <see cref="EstimateTechnique"/> is set to Percentile.
        /// For example, 0.5 corresponds to the 50th percentile (median). Values are clamped to [0.0, 1.0].
        /// </summary>
        public float Percentile
        {
            get => percentile;
            set => percentile = Mathf.Clamp(value, 0f, 1f);
        }

        [SerializeField, Range(1f, 2f)]
        [Tooltip("Multiply each ray by a fixed multiplier. Useful for when the rays produced are frequently losing contact during gestures.")]
        private float multiplier = 1f;

        /// <summary>
        /// Gets or sets the multiplier (1.0 - 2.0) applied to ray distances before aggregation.
        /// This can help compensate for rays that frequently lose contact during gestures.
        /// Values are clamped to the valid range [1.0, 2.0].
        /// </summary>
        public float Multiplier
        {
            get => multiplier;
            set => multiplier = Mathf.Clamp(value, 1f, 2f);
        }

        [SerializeField, Tooltip("When true, rays are culled based on their distance to the centroid; when false, all rays are considered.")]
        private bool cullRaysByDistanceToCentroid = false;

        /// <summary>
        /// Gets or sets a value indicating whether rays are culled by their distance to the centroid.
        /// True enables distance-based culling; false disables it and all rays are considered.
        /// </summary>
        public bool CullRaysByDistanceToCentroid { get => cullRaysByDistanceToCentroid; set => cullRaysByDistanceToCentroid = value; }

        [SerializeField, Range(0.01f, 1f)]
        [Tooltip("Normalized distance from the cluster centroid at which rays will be culled (1 = no culling, 0 = full culling).")]
        private float cullingDistanceThresholdNormalized = 1.0f;

        /// <summary>
        /// Normalized distance from the cluster centroid used to determine when rays should be culled.
        /// Value is in the range [0, 1]: 1 no rays are culled, 0 nearly all rays.
        /// </summary>
        public float CullingDistanceThresholdNormalized
        {
            get => cullingDistanceThresholdNormalized;
            set => cullingDistanceThresholdNormalized = Mathf.Clamp(value, 0.01f, 1f);
        }

        List<HPUIInteractorRayAngle> IConeRaySegmentComputation.EstimateConeAnglesForSegment(HPUIInteractorConeRayAngleSegment segment, IEnumerable<ConeRayComputationDataRecord> interactionRecords)
        {
            Dictionary<(float, float), float> averageRayDistance = new();
            // For each interaction, get the frame with the shortest distance
            bool atLeastOneRayAnalyzed = false;

            // Collect all the distances for a given ray, defined by the x and z angles
            Dictionary<(float, float), List<float>> rayDistances = new();

            int totalInteractionRecords = 0;

            foreach (ConeRayComputationDataRecord interactionRecord in interactionRecords)
            {
                if (interactionRecord.segment == segment)
                {
                    // for each frame in all the frames collected in a gesture
                    foreach (var frame in interactionRecord.records)
                    {
                        // for each ray in a given frame
                        foreach (var ray in frame.raycastDataRecordsList)
                        {
                            // Skipping rays that were not selection
                            if (!ray.isSelection)
                            {
                                continue;
                            }

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
                    totalInteractionRecords += interactionRecord.records.Count;
                }
            }

            int frameCountForMinRayInteractionsThreshold = (int)(minRayInteractionsThreshold * totalInteractionRecords);
            foreach (var ray in rayDistances)
            {
                atLeastOneRayAnalyzed = true;
                if (ray.Value.Count > frameCountForMinRayInteractionsThreshold)
                {
                    averageRayDistance[(ray.Key.Item1, ray.Key.Item2)] = EstimateTechnique switch
                        {
                            Estimate.Average => ray.Value.Average() * multiplier,
                            Estimate.Percentile => ray.Value.Percentile(percentile) * multiplier,
                            _ => throw new NotImplementedException("Unknown value")
                        };
                }
            }

            if (atLeastOneRayAnalyzed && averageRayDistance.Count() == 0)
            {
                Debug.LogWarning($"Data collection has gone wrong for Phalange {segment.ToString()}, no rays have been utilized enough for ray interaction threshold of {minRayInteractionsThreshold}");
            }

            List<HPUIInteractorRayAngle> coneAnglesForSegment = new();

            foreach (var ray in averageRayDistance)
            {
                coneAnglesForSegment.Add(new HPUIInteractorRayAngle(ray.Key.Item1, ray.Key.Item2, ray.Value));
            }

            if (CullRaysByDistanceToCentroid)
            {
                // KLUDGE: This is a simplistic centroid - is it worth having the Karcher (Riemannian) mean instead?
                Vector3 centroidRay = ((Vector3)coneAnglesForSegment.Select(angle => angle.GetDirection(false)).Aggregate((el1, el2) => el1 + el2) / coneAnglesForSegment.Count).normalized;

                // Projection lengths on the centroid. Since all vectors are normalized, no need to compute the projection itself
                Dictionary<HPUIInteractorRayAngle, float> distanceMapping = coneAnglesForSegment.ToDictionary(c => c, c => Vector3.Dot(((Vector3)c.GetDirection(false)).normalized, centroidRay));

                // Compute the threshold for filtering and filter
                float threshold = (distanceMapping.Values.Max() - distanceMapping.Values.Min()) * (1 - CullingDistanceThresholdNormalized) + distanceMapping.Values.Min();
                coneAnglesForSegment = distanceMapping.Where(kvp => kvp.Value >= threshold).Select(kvp => kvp.Key).ToList();
            }

            return coneAnglesForSegment;
        }
    }

    public static class ListExtension
    {
        public static float Percentile(this IEnumerable<float> source, float percentile)
        {
            if (percentile < 0 || percentile > 1)
                throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 1");

            var sorted = source.OrderBy(n => n).ToList();
            int count = sorted.Count;
            if (count == 0)
                throw new InvalidOperationException("Empty collection");

            float position = (percentile) * (count - 1);
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            float weight = position - lowerIndex;

            if (upperIndex >= count)
                return sorted[lowerIndex];

            return sorted[lowerIndex] * (1 - weight) + sorted[upperIndex] * weight;
        }
    }
}
