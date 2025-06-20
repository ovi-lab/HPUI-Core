using System.Collections.Generic;
using UnityEngine;

namespace BasicStats
{
    public static class Stats
    {
        public static void CalculateBestFitLine(List<Vector2> points, out float slope, out float yIntercept)
        {
            int n = points.Count;
            float sumX = 0f;
            float sumY = 0f;
            float sumXY = 0f;
            float sumX2 = 0f;

            // Calculate sums
            foreach (Vector2 point in points)
            {
                sumX += point.x;
                sumY += point.y;
                sumXY += point.x * point.y;
                sumX2 += point.x * point.x;
            }

            // Calculate slope (m)
            slope = ((n * sumXY) - (sumX * sumY)) / (n * sumX2 - sumX * sumX);

            // Calculate y-intercept (b)
            yIntercept = (sumY - slope * sumX) / n;
        }

        
        /// <summary>
        /// Removes outliers from a list of Vector2 points based on the angle from the origin (0, 0).
        /// </summary>
        /// <param name="points">The list of Vector2 points.</param>
        /// <returns>A new list of Vector2 points with outliers removed.</returns>
        public static List<Vector2> RemoveOutliers(List<Vector2> points)
        {
            if (points == null || points.Count < 4)
            {
                // Return the original list if there are too few points to calculate IQR.
                return points;
            }

            // Calculate angles of each point from the origin
            List<float> angles = new List<float>();
            foreach (Vector2 point in points)
            {
                float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                if (angle < 0)
                {
                    angle += 360; // Normalize to range [0, 360)
                }
                angles.Add(angle);
            }

            // Calculate IQR for angles
            float lowerBound, upperBound;
            GetIQRBounds(angles, out lowerBound, out upperBound);

            // Filter points within the IQR bounds of angles
            List<Vector2> filteredPoints = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
            {
                if (angles[i] >= lowerBound && angles[i] <= upperBound)
                {
                    filteredPoints.Add(points[i]);
                }
            }
            return filteredPoints;
        }

        /// <summary>
        /// Calculates the IQR bounds for a list of float values.
        /// </summary>
        /// <param name="values">The list of float values.</param>
        /// <param name="lowerBound">The calculated lower bound.</param>
        /// <param name="upperBound">The calculated upper bound.</param>
        private static void GetIQRBounds(List<float> values, out float lowerBound, out float upperBound)
        {
            values.Sort();

            // Calculate Q1 (25th percentile) and Q3 (75th percentile)
            float q1 = GetPercentile(values, 0.25f);
            float q3 = GetPercentile(values, 0.75f);

            // Calculate IQR and bounds
            float iqr = q3 - q1;
            lowerBound = q1 - iqr;
            upperBound = q3 + iqr;
        }

        /// <summary>
        /// Calculates the percentile value from a sorted list of floats.
        /// </summary>
        /// <param name="sortedValues">The sorted list of float values.</param>
        /// <param name="percentile">The percentile to calculate (e.g., 0.25 for 25th percentile).</param>
        /// <returns>The calculated percentile value.</returns>
        private static float GetPercentile(List<float> sortedValues, float percentile)
        {
            int count = sortedValues.Count;
            float index = (count - 1) * percentile;
            int lowerIndex = Mathf.FloorToInt(index);
            int upperIndex = Mathf.CeilToInt(index);

            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }

            float lowerValue = sortedValues[lowerIndex];
            float upperValue = sortedValues[upperIndex];
            return Mathf.Lerp(lowerValue, upperValue, index - lowerIndex);
        }
    }
}
