using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine.XR.Hands;
using Unity.XR.CoreUtils;
using UnityEngine.Pool;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactor. Selects/hovers only the closest interactable for a given zOrder.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRHandTrackingEvents))]
    public class HPUIInteractor: XRBaseInteractor, IHPUIInteractor
    {
        /// <summary>
        /// When using ray cast based interactions the technique that can be used.
        /// </summary>
        public enum RayCastTechniqueEnum {
            /// <summary>
            /// Use the <see cref="coneRayAngles"/> to compute the rays.
            /// </summary>
            Cone,
            /// <summary>
            /// <see cref="GetAttachtransform">attach transform</see> up would be the center ray.
            /// </summary>
            FullRange
        }

        /// <inheritdoc />
        public new InteractorHandedness handedness
        {
            get => base.handedness;
            set
            {
                bool doReset = base.handedness != value;
                base.handedness = value;
                if (doReset)
                {
                    ResetAngleFunctions();
                }
            }
        }

        // TODO move these to an asset?
        [Tooltip("The time threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapTimeThreshold;
        /// <summary>
        /// The time threshold at which an interaction would be treated as a gesture.
        /// That is, if the interactor is in contact with an
        /// interactable for more than this threshold, it would be
        /// treated as a gesture.
        /// </summary>
        public float TapTimeThreshold
        {
            get => tapTimeThreshold;
            set
            {
                tapTimeThreshold = value;
                UpdateLogic();
            }
        }

        [Tooltip("The distance threshold at which an interaction would be treated as a gesture.")]
        [SerializeField]
        private float tapDistanceThreshold;
        /// <summary>
        /// The distance threshold at which an interaction would be treated as a gesture.
        /// That is, if the interactor has moved more than this value
        /// after coming into contact with an interactable, it would be
        /// treated as a gesture.
        /// </summary>
        public float TapDistanceThreshold
        {
            get => tapDistanceThreshold;
            set
            {
                tapDistanceThreshold = value;
                UpdateLogic();
            }
        }

        [SerializeField]
        [Tooltip("If true, will use ray casting, else will use sphere overlap for detecting interactions.")]
        private bool useRayCast = true;

        /// <summary>
        /// If true, will use ray casting, else will use sphere overlap for detecting interactions.
        /// </summary>
        public bool UseRayCast { get => useRayCast; set => useRayCast = value; }

        [SerializeField]
        [Tooltip("Ray cast technique to use.")]
        private RayCastTechniqueEnum rayCastTechnique = RayCastTechniqueEnum.FullRange;

        public RayCastTechniqueEnum RayCastTechnique {
            get => rayCastTechnique;
            set
            {
                rayCastTechnique = value;
                UpdateLogic();
            }
        }

        [SerializeField]
        [Tooltip("Event triggered on tap")]
        private HPUITapEvent tapEvent = new HPUITapEvent();

        /// <inheritdoc />
        public HPUITapEvent TapEvent { get => tapEvent; set => tapEvent = value; }

        [SerializeField]
        [Tooltip("Event triggered on gesture")]
        private HPUIGestureEvent gestureEvent = new HPUIGestureEvent();

        /// <inheritdoc />
        public HPUIGestureEvent GestureEvent { get => gestureEvent; set => gestureEvent = value; }

        [SerializeField]
        [Tooltip("Event triggered on hover update.")]
        private HPUIHoverUpdateEvent hoverUpdateEvent = new HPUIHoverUpdateEvent();

        /// <inheritdoc />
        public HPUIHoverUpdateEvent HoverUpdateEvent { get => hoverUpdateEvent; set => hoverUpdateEvent = value; }

        [SerializeField]
        [Tooltip("Interaction hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interaction hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        [SerializeField]
        [Tooltip("Interaction select radius.")]
        private float interactionSelectionRadius = 0.015f;

        /// <summary>
        /// Interaction selection radius.
        /// </summary>
        public float InteractionSelectionRadius
        {
            get => interactionSelectionRadius;
            set
            {
                interactionSelectionRadius = value;
                UpdateLogic();
            }
        }

        [SerializeField]
        [Tooltip("If true, select only happens for the target with highest priority.")]
        private bool selectOnlyPriorityTarget = true;

        /// <summary>
        /// If true, select only happens for the target with the highest priority.
        /// </summary>
        public bool SelectOnlyPriorityTarget { get => selectOnlyPriorityTarget; set => selectOnlyPriorityTarget = value; }

        // QueryTriggerInteraction.Ignore
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

        /// <summary>
        /// Determines whether triggers should be collided with.
        /// </summary>
        public QueryTriggerInteraction PhysicsTriggerInteraction { get => physicsTriggerInteraction; set => physicsTriggerInteraction = value; }

        [Space()]
        [SerializeField]
        [Tooltip("Show sphere visual.")]
        private bool showSphereVisual = true;

        /// <summary>
        /// Show sphere visual for selection. The radius of the sphere is equal to <see cref="InteractionSelectionRadius"/>.
        /// </summary>
        public bool ShowSphereVisual
        {
            get => showSphereVisual;
            set
            {
                showSphereVisual = value;
                UpdateVisuals();
            }
        }

        [SerializeField]
        [Tooltip("Show sphere rays used for interaction selections.")]
        private bool showDebugRayVisual = true;

        /// <summary>
        /// Show sphere rays used for interaction selections.
        /// </summary>
        public bool ShowDebugRayVisual { get => showDebugRayVisual; set => showDebugRayVisual = value; }

        [SerializeField]
        [Tooltip("The HPUIInteractorConeRayAngles asset to use when using cone")]
        private HPUIInteractorConeRayAngles coneRayAngles;

        /// <summary>
        /// The HPUIInteractorConeRayAngles asset to use when using cone
        /// </summary>
        public HPUIInteractorConeRayAngles ConeRayAngles { get => coneRayAngles; set => coneRayAngles = value; }

        [SerializeField]
        [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
        private Transform xrOriginTransform;

        /// <summary>
        /// XR Origin transform. If not set, will attempt to find XROrigin and use its transform.
        /// </summary>
        public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }

        [SerializeField]
        [Tooltip("The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique")]
        private HPUIInteractorFullRangeAngles fullRangeRayAngles;

        /// <summary>
        /// The HPUIInteractorFullRangeAngles asset to use for FullRange ray technique
        /// </summary>
        public HPUIInteractorFullRangeAngles FullRangeRayAngles { get => fullRangeRayAngles; set => fullRangeRayAngles = value; }

        /// <summary>
        /// The gesture logic used by the interactor
        /// </summary>
        public IHPUIGestureLogic GestureLogic { get; set; }

        private Dictionary<IHPUIInteractable, InteractionInfo> validTargets = new Dictionary<IHPUIInteractable, InteractionInfo>();
        private Vector3 lastInteractionPoint;
        private PhysicsScene physicsScene;
        private RaycastHit[] rayCastHits = new RaycastHit[200];
        private Collider[] overlapSphereHits = new Collider[200];
        private GameObject visualsObject;

        private List<HPUIInteractorRayAngle> activeFingerAngles;
        // Used when computing the centroid
        private Dictionary<IHPUIInteractable, List<InteractionInfo>> tempValidTargets = new();

        private XRHandTrackingEvents xrHandTrackingEvents;
        protected Dictionary<XRHandJointID, Vector3> jointLocations = new Dictionary<XRHandJointID, Vector3>();


#if UNITY_EDITOR
        private bool onValidateUpdate;
#endif

        // Used with the RayCastTechnique.SegmentVector, and RayCastTechnique.FullRange
        protected List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
            XRHandJointID.ThumbTip
        };

        private Dictionary<XRHandJointID, List<XRHandJointID>> trackedJointsToRelatedFingerJoints = new ()
        {
            {XRHandJointID.IndexProximal, new List<XRHandJointID>() {XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip}},
            {XRHandJointID.IndexIntermediate, new List<XRHandJointID>() {XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip}},
            {XRHandJointID.IndexDistal, new List<XRHandJointID>() {XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip}},
            {XRHandJointID.IndexTip, new List<XRHandJointID>() {XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip}},
            {XRHandJointID.MiddleProximal, new List<XRHandJointID>() {XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip}},
            {XRHandJointID.MiddleIntermediate, new List<XRHandJointID>() {XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip}},
            {XRHandJointID.MiddleDistal, new List<XRHandJointID>() {XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip}},
            {XRHandJointID.MiddleTip, new List<XRHandJointID>() {XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip}},
            {XRHandJointID.RingProximal, new List<XRHandJointID>() {XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip}},
            {XRHandJointID.RingIntermediate, new List<XRHandJointID>() {XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip}},
            {XRHandJointID.RingDistal, new List<XRHandJointID>() {XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip}},
            {XRHandJointID.RingTip, new List<XRHandJointID>() {XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip}},
            {XRHandJointID.LittleProximal, new List<XRHandJointID>() {XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip}},
            {XRHandJointID.LittleIntermediate, new List<XRHandJointID>() {XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip}},
            {XRHandJointID.LittleDistal, new List<XRHandJointID>() {XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip}},
            {XRHandJointID.LittleTip, new List<XRHandJointID>() {XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip}},
        };

        protected bool receivedNewJointData = false,
            flipZAngles = false;

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

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
            physicsScene = gameObject.scene.GetPhysicsScene();
            activeFingerAngles = FullRangeRayAngles.angles;
            UpdateLogic();

            foreach(XRHandJointID id in trackedJoints)
            {
                jointLocations.Add(id, Vector3.zero);
            }

            ResetAngleFunctions();
            if (XROriginTransform == null)
            {
                XROriginTransform = FindObjectOfType<XROrigin>()?.transform;
                if (XROriginTransform == null)
                {
                    Debug.LogError($"XR Origin not found! Manually set value for XROriginTransform");
                }
            }
            xrHandTrackingEvents.jointsUpdated.AddListener(UpdateJointsData);
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();
        }

        protected void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
                {
                    jointLocations[id] = xrOriginTransform.TransformPoint(pose.position);
                    receivedNewJointData = true;
                }
            }
        }

        protected void ResetAngleFunctions()
        {
            xrHandTrackingEvents = GetComponent<XRHandTrackingEvents>();
            xrHandTrackingEvents.handedness = handedness switch {
                InteractorHandedness.Right => Handedness.Right,
                InteractorHandedness.Left => Handedness.Left,
                _ => Handedness.Invalid
            };
            flipZAngles = handedness == InteractorHandedness.Left;
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        protected void OnValidate()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                // NOTE: some of the setup running in the respective methods are not compatible with
                // OnValidate as they trigger many SendMessage calls
                onValidateUpdate = true;
            }
        }
#endif

        /// <inheritdoc />
        protected void Update()
        {
#if UNITY_EDITOR
            if (onValidateUpdate)
            {
                UpdateLogic();
                UpdateVisuals();
                onValidateUpdate = false;
            }
#endif
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateVisuals();
        }

        //FIXME: debug code
        public Transform o1, o2, o3;

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            UnityEngine.Profiling.Profiler.BeginSample("HPUIInteractor.ProcessInteractor");
            // Following the logic in XRPokeInteractor
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                validTargets.Clear();

                Transform attachTransform = GetAttachTransform(null);
                Vector3 interactionPoint = attachTransform.position;
                Vector3 hoverEndPoint = attachTransform.position;

                if (UseRayCast)
                {
                    DataWriter = "//";

                    UnityEngine.Profiling.Profiler.BeginSample("rayDirections");
                    // TODO: Move this logic to its own component
                    switch(RayCastTechnique)
                    {
                        case RayCastTechniqueEnum.Cone:
                            if (receivedNewJointData)
                            {
                                receivedNewJointData = false;
                                Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip];
                                XRHandJointID activeFinger = (trackedJoints
                                         .Where(j => j != XRHandJointID.ThumbTip)
                                         .Select(j => new {item = j, pos = (jointLocations[j] - thumbTipPos).magnitude})
                                         .OrderBy(el => el.pos)
                                         .First()
                                         .item);

                                activeFingerAngles = activeFinger switch
                                {
                                    XRHandJointID.IndexProximal      => ConeRayAngles.IndexProximalAngles,
                                    XRHandJointID.IndexIntermediate  => ConeRayAngles.IndexIntermediateAngles,
                                    XRHandJointID.IndexDistal        => ConeRayAngles.IndexDistalAngles,
                                    XRHandJointID.IndexTip           => ConeRayAngles.IndexDistalAngles,
                                    XRHandJointID.MiddleProximal     => ConeRayAngles.MiddleProximalAngles,
                                    XRHandJointID.MiddleIntermediate => ConeRayAngles.MiddleIntermediateAngles,
                                    XRHandJointID.MiddleDistal       => ConeRayAngles.MiddleDistalAngles,
                                    XRHandJointID.MiddleTip          => ConeRayAngles.MiddleDistalAngles,
                                    XRHandJointID.RingProximal       => ConeRayAngles.RingProximalAngles,
                                    XRHandJointID.RingIntermediate   => ConeRayAngles.RingIntermediateAngles,
                                    XRHandJointID.RingDistal         => ConeRayAngles.RingDistalAngles,
                                    XRHandJointID.RingTip            => ConeRayAngles.RingDistalAngles,
                                    XRHandJointID.LittleProximal     => ConeRayAngles.LittleProximalAngles,
                                    XRHandJointID.LittleIntermediate => ConeRayAngles.LittleIntermediateAngles,
                                    XRHandJointID.LittleDistal       => ConeRayAngles.LittleDistalAngles,
                                    XRHandJointID.LittleTip          => ConeRayAngles.LittleDistalAngles,
                                    _ => throw new System.InvalidOperationException($"Unknown active finger seen. Got {activeFinger}")
                                };
                            }
                            break;
                        case RayCastTechniqueEnum.FullRange:
                            activeFingerAngles = FullRangeRayAngles.angles;
                            break;
                        default:
                            break;
                    }
                    UnityEngine.Profiling.Profiler.EndSample();

                    tempValidTargets.Clear();

                    UnityEngine.Profiling.Profiler.BeginSample("raycasts");
                    int idx = -1;
                    foreach(HPUIInteractorRayAngle angle in activeFingerAngles)
                    {
                        bool validInteractable = false;
                        idx++;
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
                                hpuiInteractable.IsHoverableBy(this))
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
                    UnityEngine.Profiling.Profiler.EndSample();
                }
                else
                {
                    int numberOfOverlaps = physicsScene.OverlapSphere(
                        interactionPoint,
                        InteractionHoverRadius,
                        overlapSphereHits,
                        physicsLayer,
                        physicsTriggerInteraction);

                    float shortestInteractableDist = float.MaxValue;

                    for (var i = 0; i < numberOfOverlaps; ++i)
                    {
                        Collider collider = overlapSphereHits[i];
                        if (interactionManager.TryGetInteractableForCollider(collider, out var interactable) &&
                            interactable is IHPUIInteractable hpuiInteractable &&
                            !validTargets.ContainsKey(hpuiInteractable) &&
                            hpuiInteractable.IsHoverableBy(this))
                        {
                            XRInteractableUtility.TryGetClosestPointOnCollider(interactable, interactionPoint, out DistanceInfo info);
                            float dist = Mathf.Sqrt(info.distanceSqr);
                            validTargets.Add(hpuiInteractable, new InteractionInfo(dist, info.point, info.collider, dist, selectionCheck: dist < InteractionSelectionRadius));
                            if (dist < shortestInteractableDist)
                            {
                                hoverEndPoint = info.point;
                            }
                        }
                    }
                }

                if (data != null)
                {
                    data.Invoke(DataWriter);
                }

                try
                {
                    if (validTargets.Count > 0)
                    {
                        HoverUpdateEvent?.Invoke(new HPUIHoverUpdateEventArgs(
                                                     this,
                                                     hoverEndPoint,
                                                     attachTransform.position));
                    }
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.BeginSample("gestureLogic");
                    GestureLogic.Update(validTargets.ToDictionary(kvp => kvp.Key, kvp => new HPUIInteractionData(kvp.Value.distance, kvp.Value.heuristic, kvp.Value.selectionCheck, kvp.Value.extra)));
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            base.GetValidTargets(targets);

            targets.Clear();
            IEnumerable<IHPUIInteractable> filteredValidTargets = validTargets
                .Where(kvp => (kvp.Key is IHPUIInteractable))
                .GroupBy(kvp => kvp.Key.zOrder)
                .Select(g => g
                        .OrderBy(kvp => kvp.Value.distance)
                        .First()
                        .Key)
                .OrderBy(ht => ht.zOrder);
            targets.AddRange(filteredValidTargets);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            bool canSelect = validTargets.TryGetValue(interactable as IHPUIInteractable, out InteractionInfo info) &&
                info.distance < interactionSelectionRadius &&
                ProcessSelectFilters(interactable);
            return canSelect && (!SelectOnlyPriorityTarget || GestureLogic.IsPriorityTarget(interactable as IHPUIInteractable));
        }

        /// <summary>
        /// Update the visuals being shown. Called when the <see cref="ShowSphereVisual"/> is updated or the component is updated in inspector.
        /// </summary>
        protected void UpdateVisuals()
        {
            if (!ShowSphereVisual)
            {
                if(visualsObject != null)
                {
                    Destroy(visualsObject);
                }
                return;
            }

            if (visualsObject == null)
            {
                visualsObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualsObject.transform.SetParent(GetAttachTransform(null), false);
                if (visualsObject.TryGetComponent<Collider>(out Collider debugCollider))
                {
                    Destroy(debugCollider);
                }
            }

            visualsObject.name = "[HPUI] Visual - Select: " + this;
            visualsObject.transform.localScale = Vector3.one * InteractionSelectionRadius;
        }

        private void UpdateLogic()
        {
            // When values are changed in inspector, update the values
            if (GestureLogic != null)
            {
                if (!(GestureLogic is HPUIGestureLogic))
                {
                    Debug.Log($"Non HPUIGestureLogic being used");
                    return;
                }
                GestureLogic.Dispose();
            }

            // If using raycast, use heuristic
            GestureLogic = new HPUIGestureLogic(this, TapTimeThreshold, TapDistanceThreshold, useHeuristic: useRayCast);
        }

        #region IHPUIInteractor interface
        /// <inheritdoc />
        public void OnTap(HPUITapEventArgs args)
        {
            tapEvent?.Invoke(args);
        }

        /// <inheritdoc />
        public void OnGesture(HPUIGestureEventArgs args)
        {
            gestureEvent?.Invoke(args);
        }

        /// <inheritdoc />
        public bool GetDistanceInfo(IHPUIInteractable interactable, out DistanceInfo distanceInfo)
        {
            if (validTargets.TryGetValue(interactable, out InteractionInfo info))
            {
                distanceInfo = new DistanceInfo
                {
                    point = info.point,
                    distanceSqr = (info.collider.transform.position - info.point).sqrMagnitude,
                    collider = info.collider
                };
                return true;
            }
            distanceInfo = new DistanceInfo();
            return false;
        }
        #endregion

        struct InteractionInfo
        {
            public float distance;
            public Vector3 point;
            public Collider collider;
            public float heuristic;
            public float extra;
            public bool selectionCheck;

            public InteractionInfo(float distance, Vector3 point, Collider collider, float heuristic=0, float extra=0, bool selectionCheck=false) : this()
            {
                this.distance = distance;
                this.point = point;
                this.collider = collider;
                this.heuristic = heuristic;
                // FIXME: This needs to change! Probably remove the spherecast based approach and completely use the angle/raycast
                this.extra = extra;
                this.selectionCheck = selectionCheck;
            }
        }
    }
}
 
