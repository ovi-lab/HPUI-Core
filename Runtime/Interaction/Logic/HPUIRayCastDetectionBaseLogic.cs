using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
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
        [Tooltip("Physics layer mask used for limiting poke sphere overlap.")]
        private LayerMask physicsLayer = Physics.AllLayers;

        /// <summary>
        /// Physics layer mask used for limiting poke sphere overlap.
        /// </summary>
        public LayerMask PhysicsLayer { get => physicsLayer; set => physicsLayer = value; }

        [SerializeField]
        [Tooltip("Determines whether triggers should be collided with.")]
        private QueryTriggerInteraction physicsTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        [Tooltip("Show sphere rays used for interaction selections.")]
        private bool showDebugRayVisual = true;

        /// <summary>
        /// Show sphere rays used for interaction selections.
        /// </summary>
        public bool ShowDebugRayVisual { get => showDebugRayVisual; set => showDebugRayVisual = value; }

        // FIXME: debug code
        StringBuilder dataWriter = new StringBuilder(65000);
        public string DataWriter {
            get
            {
                string toReturn = dataWriter.ToString();
                dataWriter.Clear();
                return toReturn;
            }
            set
            {
                dataWriter.AppendFormat("::{0}", value);
            }
        }

        public event System.Action<string> data;


        protected IHPUIInteractor interactor;
        protected Dictionary<IHPUIInteractable, InteractionInfo> validTargets = new();
        // Used when computing the centroid
        protected Dictionary<IHPUIInteractable, List<InteractionInfo>> tempValidTargets = new();

        private RaycastHit[] rayCastHits = new RaycastHit[200];

        public void SetInteractor(IHPUIInteractor interactor)
        {
            this.interactor = interactor;
        }

        /// <inheritdoc />
        public abstract void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, InteractionInfo> validTargets, out Vector3 hoverEndPoint);

        /// <inheritdoc />
        public void Dispose()
        {}

        protected void Process(IHPUIInteractor interactor, XRInteractionManager interactionManager, List<HPUIInteractorRayAngle> activeFingerAngles, Dictionary<IHPUIInteractable, InteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            DataWriter = "//";
            validTargets.Clear();

            Transform attachTransform = interactor.GetAttachTransform(null);
            Vector3 interactionPoint = attachTransform.position;
            hoverEndPoint = interactionPoint;
            bool flipZAngles = interactor.handedness == InteractorHandedness.Left;

            foreach(HPUIInteractorRayAngle angle in activeFingerAngles)
            {
                bool validInteractable = false;
                Vector3 direction = angle.GetDirection(attachTransform, flipZAngles);
                int hits = Physics.RaycastNonAlloc(interactionPoint,
                                                   direction,
                                                   rayCastHits,
                                                   InteractionHoverRadius,
                                                   physicsLayer,
                                                   physicsTriggerInteraction);

                for (int hitI = 0; hitI < hits; hitI++)
                {
                    RaycastHit hitInfo = rayCastHits[hitI];
                    if (interactionManager.TryGetInteractableForCollider(hitInfo.collider, out var interactable) &&
                        interactable is IHPUIInteractable hpuiInteractable &&
                        hpuiInteractable.IsHoverableBy(interactor))
                    {
                        validInteractable = true;
                        // Opposite directions mean the interactor is above the interactable.
                        // negaative distance indicates the interactor ie under the interactable.
                        float sign = Vector3.Dot(hitInfo.collider.transform.up, direction) < 0 ? 1 : -1;
                        float distance = hitInfo.distance * sign;

                        if (data != null)
                        {
                            DataWriter = $"{interactable.transform.name},{angle.X},{angle.Z},{distance}";
                        }

                        List<InteractionInfo> infoList;
                        if (!tempValidTargets.TryGetValue(hpuiInteractable, out infoList))
                        {
                            infoList = ListPool<InteractionInfo>.Get();
                            tempValidTargets.Add(hpuiInteractable, infoList);
                        }

                        infoList.Add(new InteractionInfo(distance, hitInfo.point, hitInfo.collider, selectionCheck:angle.WithinThreshold(distance)));
                    }
                }

                if (ShowDebugRayVisual)
                {
                    Color rayColor = validInteractable ? Color.green : Color.red;
                    Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * angle.RaySelectionThreshold, rayColor);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            Vector3 centroid;
            float xEndPoint = 0, yEndPoint = 0, zEndPoint = 0;
            float count = tempValidTargets.Sum(kvp => kvp.Value.Count);

            UnityEngine.Profiling.Profiler.BeginSample("raycast centroid");
            foreach (KeyValuePair<IHPUIInteractable, List<InteractionInfo>> kvp in tempValidTargets)
            {
                float localXEndPoint = 0, localYEndPoint = 0, localZEndPoint = 0;
                float localOverThresholdCount = 0;

                foreach(InteractionInfo i in kvp.Value)
                {
                    xEndPoint += i.point.x;
                    yEndPoint += i.point.y;
                    zEndPoint += i.point.z;
                    localXEndPoint += i.point.x;
                    localYEndPoint += i.point.y;
                    localZEndPoint += i.point.z;
                    if (i.selectionCheck)
                    {
                        localOverThresholdCount++;
                    }
                }

                centroid = new Vector3(localXEndPoint, localYEndPoint, localZEndPoint) / count;

                InteractionInfo closestToCentroid = kvp.Value.OrderBy(el => (el.point - centroid).magnitude).First();
                // This distance is needed to compute the selection
                float shortestDistance = kvp.Value.Min(el => el.distance);
                closestToCentroid.heuristic = (((float)count / (float)localOverThresholdCount)) * (shortestDistance + 1);
                closestToCentroid.distance = shortestDistance;
                closestToCentroid.extra = (float)localOverThresholdCount;
                closestToCentroid.selectionCheck = localOverThresholdCount > 0;

                validTargets.Add(kvp.Key, closestToCentroid);
                ListPool<InteractionInfo>.Release(kvp.Value);
            }

            if (count > 0)
            {
                hoverEndPoint = new Vector3(xEndPoint, yEndPoint, zEndPoint) / count;;
            }

            tempValidTargets.Clear();

            if (data != null)
            {
                data.Invoke(DataWriter);
            }
        }

    }
}
