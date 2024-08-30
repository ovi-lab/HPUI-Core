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
using System;

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
            /// <see cref="GetAttachtransform">attach transform</see> to projection of thumb tip on the closest segment would be the center ray.
            /// </summary>
            SegmentVector,
            /// <summary>
            /// <see cref="GetAttachtransform">attach transform</see> up would be the center ray.
            /// </summary>
            FullRange
        }

        /// <summary>
        /// The parameters used to compute the ray angles when <see cref="RayCastTechnique.SegmentVector"/>
        /// or <see cref="RayCastTechnique.FullRange"/> are used.
        /// </summary>
        [System.Serializable]
        public struct RayAngleParams
        {
            public int maxAngle;
            public int minAngle;
            public int angleStep;

            public RayAngleParams(int maxAngle, int minAngle, int angleStep) : this()
            {
                this.maxAngle = maxAngle;
                this.minAngle = minAngle;
                this.angleStep = angleStep;
            }
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
                UpdateRayCastTechnique();
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
        [Tooltip("The HPUIInteractorRayAngles asset to use when using cone")]
        private HPUIInteractorRayAngles coneRayAngles;

        /// <summary>
        /// The HPUIInteractorRayAngles asset to use when using cone
        /// </summary>
        public HPUIInteractorRayAngles ConeRayAngles { get => coneRayAngles; set => coneRayAngles = value; }

        [SerializeField]
        [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
        private Transform xrOriginTransform;

        /// <summary>
        /// XR Origin transform. If not set, will attempt to find XROrigin and use its transform.
        /// </summary>
        public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }

        [SerializeField]
        [Tooltip("Ray configuration for FullRange ray technique")]
        private RayAngleParams fullRangeRayParameters;

        /// <summary>
        /// Ray configuration for FullRange ray technique
        /// </summary>
        public RayAngleParams FullRangeRayParameters
        {
            get => fullRangeRayParameters;
            set
            {
                fullRangeRayParameters = value;
                UpdateRayCastTechnique();
            }
        }

        [SerializeField]
        [Tooltip("Ray configuration for Segment Vector ray technique")]
        private RayAngleParams segmentVectorRayParameters;

        /// <summary>
        /// Ray configuration for Segment Vector ray technique.
        /// </summary>
        public RayAngleParams SegmentVectorRayParameters
        {
            get => segmentVectorRayParameters;
            set
            {
                segmentVectorRayParameters = value;
                UpdateRayCastTechnique();
            }
        }

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

        private List<HPUIInteractorRayAngle> allAngles,
            activeFingerAngles;
        private IEnumerable<Vector3> cachedDirections = new List<Vector3>();
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
            UpdateRayCastTechnique();
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
                UpdateRayCastTechnique();
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
                    IEnumerable<Vector3> directions;
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
                                    XRHandJointID.IndexProximal or XRHandJointID.IndexIntermediate or XRHandJointID.IndexDistal or XRHandJointID.IndexTip => ConeRayAngles.IndexAngles,
                                    XRHandJointID.MiddleProximal or XRHandJointID.MiddleIntermediate or XRHandJointID.MiddleDistal or XRHandJointID.MiddleTip => ConeRayAngles.MiddleAngles,
                                    XRHandJointID.RingProximal or XRHandJointID.RingIntermediate or XRHandJointID.RingDistal or XRHandJointID.RingTip => ConeRayAngles.RingAngles,
                                    XRHandJointID.LittleProximal or XRHandJointID.LittleIntermediate or XRHandJointID.LittleDistal or XRHandJointID.LittleTip => ConeRayAngles.LittleAngles,
                                    _ => throw new System.InvalidOperationException($"Unknown active finger seen. Got {activeFinger}")
                                };
                            }
                            directions = activeFingerAngles.Select(a => a.GetDirection(attachTransform, flipZAngles));
                            cachedDirections = directions;
                            break;
                        case RayCastTechniqueEnum.FullRange:
                            activeFingerAngles = allAngles;
                            directions = allAngles.Select(a => a.GetDirection(attachTransform, flipZAngles));
                            cachedDirections = directions;
                            break;
                        case RayCastTechniqueEnum.SegmentVector:
                            if (receivedNewJointData)
                            {
                                receivedNewJointData = false;
                                Vector3 thumbTipPos = jointLocations[XRHandJointID.ThumbTip];
                                var activePhalanges = (trackedJoints
                                                       .Where(j => j != XRHandJointID.ThumbTip)
                                                       .Select(j => new {
                                                               item = j,
                                                               pos = jointLocations[j],
                                                               dist = (jointLocations[j] - thumbTipPos).magnitude
                                                           })
                                                       .OrderBy(el => el.dist));
                                var firstItem = activePhalanges.First();
                                List<XRHandJointID> relatedFingerJoints = trackedJointsToRelatedFingerJoints[firstItem.item];
                                var secondItem = activePhalanges.Where(el => el.item != firstItem.item && relatedFingerJoints.Contains(el.item)).First();
                                Vector3 segmentVectorNormalized = (firstItem.pos - secondItem.pos).normalized;
                                Vector3 point = Vector3.Dot((thumbTipPos - secondItem.pos), segmentVectorNormalized) * segmentVectorNormalized + secondItem.pos;

                                Vector3 up = point - thumbTipPos;
                                Vector3 right = Vector3.Cross(up, attachTransform.forward);
                                Vector3 forward = Vector3.Cross(up, right);

                                List<Vector3> tempDirections = new List<Vector3>();

                                for (int x = SegmentVectorRayParameters.minAngle; x <= SegmentVectorRayParameters.maxAngle; x = x + SegmentVectorRayParameters.angleStep)
                                {
                                    for (int z = SegmentVectorRayParameters.minAngle; z <= SegmentVectorRayParameters.maxAngle; z = z + SegmentVectorRayParameters.angleStep)
                                    {
                                        tempDirections.Add(HPUIInteractorRayAngle.GetDirection(x, z, right, forward, up, flipZAngles));
                                    }
                                }

                                directions = tempDirections;

                                cachedDirections = directions;
                            }
                            else
                            {
                                directions = cachedDirections;
                            }
                            break;
                        default:
                            directions = cachedDirections;
                            break;
                    }
                    UnityEngine.Profiling.Profiler.EndSample();
                    // float x_ = (float)angles.Select(a => a.x).Average();
                    // float z_ = (float)angles.Select(a => a.z).Average();

                    // var direction_ = Quaternion.AngleAxis(x_, attachTransform.right) * Quaternion.AngleAxis(z_, attachTransform.forward) * attachTransform.up;

                    // Debug.DrawLine(interactionPoint, interactionPoint + direction_.normalized * InteractionHoverRadius * 2, Color.blue);

                    tempValidTargets.Clear();

                    UnityEngine.Profiling.Profiler.BeginSample("raycasts");
                    int idx = -1;
                    foreach(Vector3 direction in directions)
                    {
                        bool validInteractable = false;
                        idx++;
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
                                if (data != null && (RayCastTechnique == RayCastTechniqueEnum.Cone || RayCastTechnique == RayCastTechniqueEnum.FullRange))
                                {
                                    HPUIInteractorRayAngle angle = activeFingerAngles[idx];
                                    DataWriter = $"{interactable.transform.name},{angle.x},{angle.z},{hitInfo.distance}";
                                }
                                List<InteractionInfo> infoList;
                                if (!tempValidTargets.TryGetValue(hpuiInteractable, out infoList))
                                {
                                    infoList = ListPool<InteractionInfo>.Get();
                                    tempValidTargets.Add(hpuiInteractable, infoList);
                                }

                                infoList.Add(new InteractionInfo(hitInfo.distance, hitInfo.point, hitInfo.collider));
                            }
                        }

                        if (ShowDebugRayVisual)
                        {
                            Color rayColor = validInteractable ? Color.green : Color.red;
                            Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * InteractionSelectionRadius, rayColor);
                        }
                    }
                    UnityEngine.Profiling.Profiler.EndSample();

                    Vector3 centroid;
                    float xEndPoint = 0, yEndPoint = 0, zEndPoint = 0;
                    float count = tempValidTargets.Sum(kvp => kvp.Value.Count);

                    UnityEngine.Profiling.Profiler.BeginSample("raycast centroid");
                    foreach (KeyValuePair<IHPUIInteractable, List<InteractionInfo>> kvp in tempValidTargets)
                    {
                        int localCount = kvp.Value.Count;
                        float localXEndPoint = 0, localYEndPoint = 0, localZEndPoint = 0;

                        foreach(InteractionInfo i in kvp.Value)
                        {
                            xEndPoint += i.point.x;
                            yEndPoint += i.point.y;
                            zEndPoint += i.point.z;
                            localXEndPoint += i.point.x;
                            localYEndPoint += i.point.y;
                            localZEndPoint += i.point.z;
                        }

                        centroid = new Vector3(localXEndPoint, localYEndPoint, localZEndPoint) / count;

                        InteractionInfo closestToCentroid = kvp.Value.OrderBy(el => (el.point - centroid).magnitude).First();
                        // This distance is needed to compute the selection
                        float shortestDistance = kvp.Value.Min(el => el.distance);
                        closestToCentroid.heuristic = (((float)count / (float)localCount) + 1) * shortestDistance;
                        closestToCentroid.distance = shortestDistance;
                        closestToCentroid.extra = (float)localCount;

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
                            validTargets.Add(hpuiInteractable, new InteractionInfo(dist, info.point, info.collider));
                            if (dist < shortestInteractableDist)
                            {
                                hoverEndPoint = info.point;
                            }
                        }
                    }
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
                    GestureLogic.Update(validTargets.ToDictionary(kvp => kvp.Key, kvp => new HPUIInteractionData(kvp.Value.distance, kvp.Value.heuristic, kvp.Value.extra)));
                    UnityEngine.Profiling.Profiler.EndSample();

                    if (data != null)
                    {
                        data.Invoke(DataWriter);
                    }
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
            GestureLogic = new HPUIGestureLogic(this, TapTimeThreshold, TapDistanceThreshold, InteractionSelectionRadius, useHeuristic: useRayCast);
        }

        /// <summary>
        /// Method called for setup related to ray cast technique
        /// </summary>
        protected void UpdateRayCastTechnique()
        {
            allAngles = new List<HPUIInteractorRayAngle>();

            switch (rayCastTechnique)
            {
                case RayCastTechniqueEnum.FullRange:
                    if (FullRangeRayParameters.minAngle == 0 && FullRangeRayParameters.maxAngle == 0)
                    {
                        throw new InvalidOperationException("Full Range Vector Ray Parameters not configured!");
                    }

                    if (FullRangeRayParameters.angleStep == 0)
                    {
                        throw new InvalidOperationException("Full Range Vector Ray Parameters angle step is 0!");
                    }

                    ComputeAllAngles();
                    break;
                case RayCastTechniqueEnum.SegmentVector:
                    if (SegmentVectorRayParameters.minAngle == 0 && SegmentVectorRayParameters.maxAngle == 0)
                    {
                        throw new InvalidOperationException("Segment Vector Range Vector Ray Parameters not configured!");
                    }

                    if (SegmentVectorRayParameters.angleStep == 0)
                    {
                        throw new InvalidOperationException("Segment Vector Vector Ray Parameters angle step is 0!");
                    }
                    break;
                case RayCastTechniqueEnum.Cone:
                    if (ConeRayAngles == null)
                    {
                        throw new InvalidOperationException("Cone Ray Angles cannot be empty when using Cone");
                    }
                    break;
            }

            // Avoid null ref exception before the hand tracking module gets going
            activeFingerAngles = allAngles;
        }

        private void ComputeAllAngles()
        {
            float numberOfSamples = Mathf.Pow(360 / FullRangeRayParameters.angleStep, 2);
            List<Vector3> spericalPoints = new();
            float phi = Mathf.PI * (Mathf.Sqrt(5) - 1);

            float yMin = Mathf.Cos(Mathf.Min(Mathf.Abs(FullRangeRayParameters.minAngle * Mathf.Deg2Rad), Mathf.Min(FullRangeRayParameters.maxAngle * Mathf.Deg2Rad)));

            for(int i=0; i < numberOfSamples ; i++)
            {
                float y = 1 - (i / (numberOfSamples - 1)) * 2;
                if (y < yMin)
                {
                    break;
                }

                float radius = Mathf.Sqrt(1 - y * y);

                float theta = phi * i;

                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;

                Vector3 point = new Vector3(x, y, z);

                float zAngle = Vector3.Angle(Vector3.up, new Vector3(0, y, z)) * (z < 0 ? -1: 1);
                float xAngle = Vector3.Angle(Vector3.up, new Vector3(x, y, 0)) * (x < 0 ? -1: 1);

                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle));
            }
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

            public InteractionInfo(float distance, Vector3 point, Collider collider, float heuristic=0, float extra=0) : this()
            {
                this.distance = distance;
                this.point = point;
                this.collider = collider;
                this.heuristic = heuristic;
                this.extra = extra;
            }
        }
    }
}
 
