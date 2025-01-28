using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    // TODO docuement all of this
    [Serializable, BurstCompile]
    public class HPUIInteractorRayAngle
    {
        [SerializeField] private float x;
        [SerializeField] private float z;
        [SerializeField] private float raySelectionThreshold;
        private Vector3 leftRayDirection;
        private Vector3 rightRayDirection;
        private bool isCached;
        public float X { get => x; }
        public float Z { get => z; }
        public float RaySelectionThreshold { get => raySelectionThreshold; set => raySelectionThreshold = value; }
        public HPUIInteractorRayAngle(float x, float z, float angleThreshold)
        {
            this.x = x;
            this.z = z;
            this.raySelectionThreshold = angleThreshold;
            isCached = false;
            GetDirection(x, z, false, out float3 _rightRay);
            GetDirection(x, z, true, out float3 _leftRay);
            this.rightRayDirection = _rightRay;
            this.leftRayDirection = _leftRay;
        }

        public HPUIInteractorRayAngle(float x, float z)
        {
            this.x = x;
            this.z = z;
            isCached = false;
            GetDirection(x, z, false, out float3 _rightRay);
            GetDirection(x, z, true, out float3 _leftRay);
            this.rightRayDirection = _rightRay;
            this.leftRayDirection = _leftRay;
        }

        public bool WithinThreshold(float dist)
        {
            return dist < raySelectionThreshold;
        }


        public float3 GetDirection(bool isLeftHand)
        {
            if (!isCached)
            {
                isCached = true;
                GetDirection(x, z, false, out float3 _rightRay);
                GetDirection(x, z, true, out float3 _leftRay);
                this.rightRayDirection = _rightRay;
                this.leftRayDirection = _leftRay;
            }

            return isLeftHand ? leftRayDirection : rightRayDirection;
        }

        /// <summary>
        /// The direction relative to the unity up, forward and right vectors.
        /// X is the angle from the up vector around the right vector.
        /// Z is the angle from the up vector around the forward vector.
        /// </summary>
        [BurstCompile]
        public static void GetDirection(float x, float z, bool flipZAngles, out float3 direction)
        {
            float x_ = x,
                z_ = flipZAngles ? -z : z;

            x_ = math.radians(x_);
            z_ = math.radians(z_);
            float tanx = math.tan(x_);
            float tanz = math.tan(z_);

            float yDist = math.sqrt(1 / (1 + math.pow(tanx, 2) + math.pow(tanz, 2)));
            if (math.abs(x) > 90 || math.abs(z) > 90)
            {
                yDist = -yDist;
            }

            float xDist = tanz * yDist;
            float zDist = tanx * yDist;
            direction = new float3(xDist, yDist, zDist);
        }

        #region Equality overrides
        public override bool Equals(object obj)
        {
            return (obj is HPUIInteractorRayAngle rayAngleObj) && rayAngleObj.X == this.x && rayAngleObj.z == this.z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ z.GetHashCode();
        }

        public static bool operator ==(HPUIInteractorRayAngle a, HPUIInteractorRayAngle b)
        {
            return Mathf.Approximately(a.X, b.X) && Mathf.Approximately(a.Z, b.Z);
        }

        public static bool operator !=(HPUIInteractorRayAngle a, HPUIInteractorRayAngle b)
        {
            return !Mathf.Approximately(a.X, b.X) || !Mathf.Approximately(a.Z, b.Z);
        }
        #endregion
    }
}