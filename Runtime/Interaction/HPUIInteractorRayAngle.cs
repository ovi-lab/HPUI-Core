using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using System;

namespace ubco.ovilab.HPUI.Interaction
{
    // TODO docuement all of this
    [Serializable, BurstCompile]
    public struct HPUIInteractorRayAngle
    {
        [SerializeField] private float x;
        [SerializeField] private float z;
        [SerializeField] private float raySelectionThreshold;

        public float X { get => x; }
        public float Z { get => z; }
        public float RaySelectionThreshold { get => raySelectionThreshold; set => raySelectionThreshold = value; }

        public HPUIInteractorRayAngle(float x, float z, float angleThreshold)
        {
            this.x = x;
            this.z = z;
            this.raySelectionThreshold = angleThreshold;
        }

        public HPUIInteractorRayAngle(float x, float z)
        {
            this.x = x;
            this.z = z;
            this.raySelectionThreshold = 1f;
        }

        public bool WithinThreshold(float dist)
        {
            return dist < raySelectionThreshold;
        }

        // /// <summary>
        // /// Get a quaternion that would rotate x angles around its right axis and z angles around its forward axis.
        // /// </summary>
        // private static Quaternion GetRotation(float x, float z)
        // {
        //     float yDist = Mathf.Sqrt(1 / (1 + Mathf.Pow(Mathf.Tan(x * Mathf.Deg2Rad), 2) + Mathf.Pow(Mathf.Tan(z * Mathf.Deg2Rad), 2)));
        //     if (Mathf.Abs(x) > 90 || Mathf.Abs(z) > 90)
        //     {
        //         yDist = -yDist;
        //     }

        //     float xDist = Mathf.Tan(x * Mathf.Deg2Rad) * yDist;
        //     float zDist = Mathf.Tan(z * Mathf.Deg2Rad) * yDist;

        //     Vector3 newUp = Vector3.up * yDist + Vector3.right * xDist + Vector3.forward * zDist;

        //     // Don't care about the forward axis, as long the the up vector gets to the right postion
        //     Vector3 newforward = Vector3.Cross(newUp, Quaternion.Euler(0, 0, 90) * newUp);
        //     Vector3 newRight = Vector3.Cross(newforward, newUp);
        //     Debug.Log($"{Vector3.Dot(newUp, Vector3.Cross(newRight, newforward))}");

        //     return Quaternion.LookRotation(newUp, newforward);
        //     // return Quaternion.LookRotation(Vector3.up, newUp);
        // }
        /// <summary>
        /// The direction relatetive to the unity up, forward and right vectors.
        /// X is the angle from the up vector around the right vector.
        /// Z is the angle from the up vector around the forward vector.
        /// </summary>
        [BurstCompile]
        public static void GetDirection(float x, float z, in float3 right, in float3 forward, in float3 up, bool flipZAngles, out float3 direction)
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

            direction = yDist * up + zDist * forward + xDist * right;
        }

        public Vector3 GetDirection(Transform attachTransform, bool flipZAngles)
        {
            GetDirection(x, z, attachTransform.right, attachTransform.forward, attachTransform.up, flipZAngles, out float3 direction);
            return direction;
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
 
