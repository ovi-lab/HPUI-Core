using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base class for deteting interactions using ray casts.  The cone of rays is based on the finger
    /// segment that is closest to the thumb tip.  The heuristic assigned to the interactable is based
    /// on the number of rays that makes contact with the interactable and the distances to it.
    /// </summary>
    public abstract class HPUIRayCastDetectionBaseLogic : IHPUIDetectionLogic
    {
        [SerializeField]
        [Tooltip("Interaction hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interaction hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        [SerializeField]
        [Tooltip("Physics layer mask used for limiting raycast collisions.")]
        private LayerMask physicsLayer = Physics.AllLayers;

        /// <summary>
        /// Physics layer mask used for limiting raycast collisions.
        /// This is different from <see cref="XRBaseInteractor.interactionLayers"/>.
        /// <see cref="XRBaseInteractor.interactionLayers"/> is
        /// related to filtering interactions in XRI.  This is related
        /// filtering the physics interactions.
        /// </summary>
        /// <seealso cref="Physics.RaycastNonAlloc"/>
        public LayerMask PhysicsLayer { get => physicsLayer; set => physicsLayer = value; }

        [SerializeField]
        [Tooltip("Determines whether triggers should be collided with.")]
        private QueryTriggerInteraction physicsTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        [Tooltip("Show rays used for interaction selections. None hides all rays, All shows active rays in green and inactive rays in red, OnlyActive shows only active rays in green")]
        private DebugRayVisual debugRayVisual = DebugRayVisual.All;

        /// <summary>
        /// If subscribed to, provides the data of the raycasts during each frame.
        /// </summary>
        public event System.Action<List<RaycastDataRecord>> raycastData;

        protected IHPUIInteractor interactor;
        protected Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets = new();
        // Used when computing the centroid
        protected Dictionary<IHPUIInteractable, List<RaycastInteractionInfo>> tempValidTargets = new();

        private RaycastHit[] rayCastHits = new RaycastHit[200];
        private List<RaycastDataRecord> raycastDataRecords = new();

        public void SetInteractor(IHPUIInteractor interactor)
        {
            this.interactor = interactor;
        }

        /// <inheritdoc />
        public abstract void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint);

        /// <inheritdoc />
        public virtual void Dispose()
        { }

        /// <inheritdoc />
        public virtual void Reset()
        { }

        /// <summary>
        /// Given the list of <see cref="HPUIInteractorRayAngle"/>, detect the interactables and populate the <see cref="validTargets"/> dictionary.
        /// </summary>
        protected void Process(IHPUIInteractor interactor, XRInteractionManager interactionManager, IReadOnlyList<HPUIInteractorRayAngle> activeFingerAngles, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            validTargets.Clear();

            Transform attachTransform = interactor.GetAttachTransform(null);
            Vector3 interactionPoint = attachTransform.position;
            hoverEndPoint = interactionPoint;
            UnityEngine.Profiling.Profiler.BeginSample("Process angles");
            bool isLeftHand = interactor.handedness == InteractorHandedness.Left;
            foreach (HPUIInteractorRayAngle angle in activeFingerAngles)
            {
                bool validInteractable = false,
                    isSelection = false;
                UnityEngine.Profiling.Profiler.BeginSample("Compute direction");
                // TODO: Batch compute this.
                Vector3 direction = attachTransform.TransformDirection(angle.GetDirection(isLeftHand));
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("raycast");
                // TODO: Use RaycastCommand
                int hits = Physics.RaycastNonAlloc(interactionPoint,
                                                   direction,
                                                   rayCastHits,
                                                   InteractionHoverRadius,
                                                   physicsLayer,
                                                   physicsTriggerInteraction);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Compute RaycastInteractionInfo");
                for (int hitI = 0; hitI < hits; hitI++)
                {
                    RaycastHit hitInfo = rayCastHits[hitI];
                    if (interactionManager.TryGetInteractableForCollider(hitInfo.collider, out var interactable) &&
                        interactable is IHPUIInteractable hpuiInteractable &&
                        hpuiInteractable.IsHoverableBy(interactor))
                    {
                        validInteractable = true;
                        // Opposite directions mean the interactor is above the interactable.
                        // negative distance indicates the interactor is under the interactable.
                        float dotProduct = Vector3.Dot(hitInfo.collider.transform.up.normalized, direction.normalized);
                        float sign = dotProduct < 0 ? 1 : -1;
                        float distance = hitInfo.distance * sign;
                        // When the ray origin is behind the interactable (dotProduct > 0) we ignore the rays
                        // that make angle between 45-90 between the collider.up and direction. The assumption
                        // here is, rays in that window are not hitting from the back, but from and angle.
                        // e.g., when seperate surfaces are used for volar and radial targets.
                        if (dotProduct > 0 && dotProduct < 0.707) // cos(45deg)
                        {
                            isSelection = false;
                        }
                        else
                        {
                            // FIXME: with the above condition, is this necessary?
                            // But we use the absolute distance to make sure rays way outside
                            // the threshold is not selected. i.e. avoid situations like -1 < 0.01
                            isSelection = angle.WithinThreshold(hitInfo.distance);
                        }

                        if (raycastData != null)
                        {
                            raycastDataRecords.Add(new RaycastDataRecord(hpuiInteractable, angle.X, angle.Z, hitInfo.distance, isSelection));
                        }

                        List<RaycastInteractionInfo> infoList;
                        if (!tempValidTargets.TryGetValue(hpuiInteractable, out infoList))
                        {
                            infoList = ListPool<RaycastInteractionInfo>.Get();
                            tempValidTargets.Add(hpuiInteractable, infoList);
                        }

                        // Using distance as the temp/default value for heuristic
                        infoList.Add(new RaycastInteractionInfo(distance, isSelection, hitInfo.point, hitInfo.collider));
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();

                if (debugRayVisual == DebugRayVisual.All)
                {
                    Color rayColor = validInteractable && isSelection ? Color.green : Color.red;
                    Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * Mathf.Min(angle.RaySelectionThreshold, InteractionHoverRadius), rayColor);
                }
                if (debugRayVisual == DebugRayVisual.OnlyActive)
                {
                    if (validInteractable && isSelection)
                    {
                        Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * angle.RaySelectionThreshold, Color.green);
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (ComputeHPUIInteractionInfo(tempValidTargets, validTargets, out Vector3 newHoverEndPoint))
            {
                hoverEndPoint = newHoverEndPoint;
            }

            foreach (List<RaycastInteractionInfo> info in tempValidTargets.Values)
            {
                ListPool<RaycastInteractionInfo>.Release(info);
            }

            tempValidTargets.Clear();

            if (raycastData != null)
            {
                raycastData.Invoke(raycastDataRecords);
                raycastDataRecords = new();
            }
        }

        /// <summary>
        /// Computes the final list of <see cref="HPUIInteractionInfo"/> for each interactable.
        /// In <see cref="validRayCastTargets"/> for each interactable, the list of information of each ray interactions is porvided.
        /// That is, in <see cref="validRayCastTargets"/> each the <see cref="RaycastInteractionInfo"/> contains the interaction information
        /// of a single raycast.  The method also processes the hoverEndPoint based on all the ray information provided.
        /// This method will upadte the <see cref="validTargets"/> dictionary.
        /// </summary>
        protected virtual bool ComputeHPUIInteractionInfo(Dictionary<IHPUIInteractable, List<RaycastInteractionInfo>> validRayCastTargets,
                                                          Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets,
                                                          out Vector3 hoverEndPoint)
        {
            Vector3 centroid;
            float xEndPoint = 0, yEndPoint = 0, zEndPoint = 0;
            float count = validRayCastTargets.Sum(kvp => kvp.Value.Count);

            foreach (KeyValuePair<IHPUIInteractable, List<RaycastInteractionInfo>> kvp in validRayCastTargets)
            {
                float localXEndPoint = 0, localYEndPoint = 0, localZEndPoint = 0;
                float localOverThresholdCount = 0,
                    localCount = 0;

                foreach (RaycastInteractionInfo i in kvp.Value)
                {
                    xEndPoint += i.point.x;
                    yEndPoint += i.point.y;
                    zEndPoint += i.point.z;
                    localXEndPoint += i.point.x;
                    localYEndPoint += i.point.y;
                    localZEndPoint += i.point.z;

                    localCount++;
                    if (i.isSelection)
                    {
                        localOverThresholdCount++;
                    }
                }

                centroid = new Vector3(localXEndPoint, localYEndPoint, localZEndPoint) / localCount;

                RaycastInteractionInfo closestToCentroid = kvp.Value.OrderBy(el => (el.point - centroid).magnitude).First();
                // This distance is needed to compute the selection
                float shortestDistance = kvp.Value.Min(el => el.distanceValue);
                float heuristic = (((float)count / (float)localOverThresholdCount)) * (shortestDistance + 1);
                float distanceValue = shortestDistance;
                bool isSelection = localOverThresholdCount > 0;

                HPUIInteractionInfo hpuiInteractionInfo = new HPUIInteractionInfo(heuristic, isSelection, closestToCentroid.point, closestToCentroid.collider, shortestDistance, null);

                validTargets.Add(kvp.Key, hpuiInteractionInfo);
            }

            hoverEndPoint = new Vector3(xEndPoint, yEndPoint, zEndPoint) / count;

            return count > 0;
        }

        /// <summary>
        /// The raycast interactoin information used with the <see cref="HPUIRayCastDetectionBaseLogic.Process"/>
        /// </summary>
        protected struct RaycastInteractionInfo
        {
            public bool isSelection;
            public Vector3 point;
            public Collider collider;
            public float distanceValue;

            public RaycastInteractionInfo(float distanceValue, bool isSelection, Vector3 point, Collider collider) : this()
            {
                this.isSelection = isSelection;
                this.point = point;
                this.collider = collider;
                this.distanceValue = distanceValue;
            }
        }

        /// <summary>
        /// Record of a single raycast
        /// </summary>
        public struct RaycastDataRecord
        {
            /// <summary>
            /// The interactable which the ray interacted with.
            /// </summary>
            public IHPUIInteractable interactable;

            /// <summary>
            /// The angle around the X axis of the interactor
            /// <seealso cref="HPUIInteractorRayAngle"/>
            /// </summary>
            public float angleX;

            /// <summary>
            /// The angle around the Z axis of the interactor
            /// <seealso cref="HPUIInteractorRayAngle"/>
            /// </summary>
            public float angleZ;

            /// <summary>
            /// The distance from the ray's origin (interactor) to the interactable.
            /// </summary>
            public float distance;

            /// <summary>
            /// If the ray interaction is a valid selection.
            /// </summary>
            public bool isSelection;

            public RaycastDataRecord(IHPUIInteractable interactable, float x, float z, float distance, bool isSelection) : this()
            {
                this.interactable = interactable;
                this.angleX = x;
                this.angleZ = z;
                this.distance = distance;
                this.isSelection = isSelection;
            }
        }

        [Serializable]
        public enum DebugRayVisual
        {
            None = 0,
            All = 1,
            OnlyActive = 2
        }
    }
}
