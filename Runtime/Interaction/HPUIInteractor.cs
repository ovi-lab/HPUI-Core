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
        public enum RayCastTechniqueEnum { cone, phalange, all }

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
        /// treated as as gesture.
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
        /// after coming into contact with a interactable, it would be
        /// treated as as gesture.
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
        private RayCastTechniqueEnum rayCastTechnique = RayCastTechniqueEnum.all;

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
        [Tooltip("Interation hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interation hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        [SerializeField]
        [Tooltip("Interation select radius.")]
        private float interactionSelectionRadius = 0.015f;

        /// <summary>
        /// Interation selection radius.
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
        /// If true, select only happens for the target with highest priority.
        /// </summary>
        public bool SelectOnlyPriorityTarget { get => selectOnlyPriorityTarget; set => selectOnlyPriorityTarget = value; }

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

        protected IHPUIGestureLogic gestureLogic;
        private Dictionary<IHPUIInteractable, InteractionInfo> validTargets = new Dictionary<IHPUIInteractable, InteractionInfo>();
        private Vector3 lastInteractionPoint;
        private PhysicsScene physicsScene;
        private RaycastHit[] sphereCastHits = new RaycastHit[200];
        private Collider[] overlapSphereHits = new Collider[200];
        private GameObject visualsObject;

        private List<HPUIInteractorRayAngle> allAngles,
            activeFingerAngles;
        private IEnumerable<Vector3> cachedDirections = new List<Vector3>();
        // Used when computing the centroid
        private Dictionary<IHPUIInteractable, List<InteractionInfo>> tempValidTargets = new();

        private XRHandTrackingEvents xrHandTrackingEvents;
        private Dictionary<XRHandJointID, Vector3> jointLocations = new Dictionary<XRHandJointID, Vector3>();
        private List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
            XRHandJointID.ThumbTip
        };

        private Dictionary<XRHandJointID, List<XRHandJointID>> tarckedJointsToRelatedFingerJoints = new ()
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

        private bool recievedNewJointData = false,
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

            // FIXME: put these in a better place?
            int maxAngle = 130,
                minAngle = -130,
                angleStep = 5;

            allAngles = new List<HPUIInteractorRayAngle>();
            for (int x = minAngle; x <= maxAngle; x = x + angleStep)
            {
                for (int z = minAngle; z <= maxAngle; z = z + angleStep)
                {
                    allAngles.Add(new HPUIInteractorRayAngle(x, z));
                }
            }

            // Avoid null ref exception before hand tracking gets going
            activeFingerAngles = allAngles;

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

        protected void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
                {
                    jointLocations[id] = xrOriginTransform.TransformPoint(pose.position);
                    recievedNewJointData = true;
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
            if (Application.isPlaying)
            {
                UpdateLogic();
                UpdateVisuals();
            }
        }
#endif

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateVisuals();
        }

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            // Following the logic in XRPokeInteractor
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                validTargets.Clear();

                Transform attachTransform = GetAttachTransform(null);
                Vector3 interactionPoint = attachTransform.position;

                if (UseRayCast)
                {
                    IEnumerable<Vector3> directions;
                    DataWriter = "//";

                    // TODO: Move this logic to its own component
                    switch(RayCastTechnique)
                    {
                        case RayCastTechniqueEnum.cone:
                            if (recievedNewJointData)
                            {
                                recievedNewJointData = false;
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
                        case RayCastTechniqueEnum.all:
                            directions = allAngles.Select(a => a.GetDirection(attachTransform, flipZAngles));
                            cachedDirections = directions;
                            break;
                        case RayCastTechniqueEnum.phalange:
                            if (recievedNewJointData)
                            {
                                recievedNewJointData = false;
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
                                List<XRHandJointID> relatedFignerJoints = tarckedJointsToRelatedFingerJoints[firstItem.item];
                                var secondItem = activePhalanges.Where(el => el.item != firstItem.item && relatedFignerJoints.Contains(el.item)).First();
                                Vector3 segmentVectorNormalized = (firstItem.pos - secondItem.pos).normalized;
                                Vector3 point = Vector3.Dot((thumbTipPos - secondItem.pos), segmentVectorNormalized) * segmentVectorNormalized + secondItem.pos;

                                Vector3 up = point - thumbTipPos;
                                Vector3 right = Vector3.Cross(up, attachTransform.forward);
                                Vector3 forward = Vector3.Cross(up, right);

                                // FIXME: put these in a better place?
                                int maxAngle = 20,
                                minAngle = -20,
                                angleStep = 5;
                                List<Vector3> tempDirections = new List<Vector3>();

                                for (int x = minAngle; x <= maxAngle; x = x + angleStep)
                                {
                                    for (int z = minAngle; z <= maxAngle; z = z + angleStep)
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
                    // float x_ = (float)angles.Select(a => a.x).Average();
                    // float z_ = (float)angles.Select(a => a.z).Average();

                    // var direction_ = Quaternion.AngleAxis(x_, attachTransform.right) * Quaternion.AngleAxis(z_, attachTransform.forward) * attachTransform.up;

                    // Debug.DrawLine(interactionPoint, interactionPoint + direction_.normalized * InteractionHoverRadius * 2, Color.blue);

                    foreach(Vector3 direction in directions)
                    {
                        bool validInteractable = false;
                        if (Physics.Raycast(interactionPoint,
                                            direction,
                                            out RaycastHit hitInfo,
                                            InteractionHoverRadius,
                                            // FIXME: physics layers should be allowed to be set in inpsector
                                            Physics.AllLayers,
                                            // FIXME: QueryTriggerInteraction should be allowed to be set in inpsector
                                            QueryTriggerInteraction.Ignore))
                        {
                            if (interactionManager.TryGetInteractableForCollider(hitInfo.collider, out var interactable) &&
                                interactable is IHPUIInteractable hpuiInteractable &&
                                hpuiInteractable.IsHoverableBy(this))
                            {
                                validInteractable = true;
                                if (RayCastTechnique == RayCastTechniqueEnum.cone || RayCastTechnique == RayCastTechniqueEnum.all)
                                {
                                    HPUIInteractorRayAngle angle = activeFingerAngles[directions.TakeWhile(el => el == direction).Count()];
                                    DataWriter = $"{interactable.transform.name},{angle.x},{angle.z},{hitInfo.distance}";
                                }
                                List<InteractionInfo> infoList;
                                if (!tempValidTargets.TryGetValue(hpuiInteractable, out infoList))
                                {
                                    infoList = ListPool<InteractionInfo>.Get();
                                    tempValidTargets.Add(hpuiInteractable, infoList);
                                }

                                infoList.Add(new InteractionInfo(hitInfo.distance, hitInfo.point));
                            }
                        }

                        if (ShowDebugRayVisual)
                        {
                            Color rayColor = validInteractable ? Color.green : Color.red;
                            Debug.DrawLine(interactionPoint, interactionPoint + direction.normalized * InteractionHoverRadius, rayColor);
                        }
                    }

                    foreach (KeyValuePair<IHPUIInteractable, List<InteractionInfo>> kvp in tempValidTargets)
                    {
                        InteractionInfo smallest = kvp.Value.OrderBy(el => el.distance).First();
                        smallest.huristic = 1 / kvp.Value.Count;
                        validTargets.Add(kvp.Key, smallest);
                        ListPool<InteractionInfo>.Release(kvp.Value);
                    }

                    tempValidTargets.Clear();
                }
                else
                {
                    int numberOfOverlaps = physicsScene.OverlapSphere(
                        interactionPoint,
                        InteractionHoverRadius,
                        overlapSphereHits,
                        // FIXME: physics layers should be allowed to be set in inpsector
                        Physics.AllLayers,
                        // FIXME: QueryTriggerInteraction should be allowed to be set in inpsector
                        QueryTriggerInteraction.Ignore);

                    for (var i = 0; i < numberOfOverlaps; ++i)
                    {
                        Collider collider = overlapSphereHits[i];
                        if (interactionManager.TryGetInteractableForCollider(collider, out var interactable) &&
                            interactable is IHPUIInteractable hpuiInteractable &&
                            !validTargets.ContainsKey(hpuiInteractable) &&
                            hpuiInteractable.IsHoverableBy(this))
                        {
                            XRInteractableUtility.TryGetClosestPointOnCollider(interactable, interactionPoint, out DistanceInfo info);
                            validTargets.Add(hpuiInteractable, new InteractionInfo(Mathf.Sqrt(info.distanceSqr), info.point));
                        }
                    }
                }
            }
            gestureLogic.Update(validTargets.ToDictionary(kvp => kvp.Key, kvp => new HPUIInteractionData(kvp.Value.distance, kvp.Value.huristic)));

            if (data != null)
            {
                data.Invoke(DataWriter);
            }
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
            return canSelect && (!SelectOnlyPriorityTarget || gestureLogic.IsPriorityTarget(interactable as IHPUIInteractable));
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
            if (gestureLogic != null)
            {
                gestureLogic.Dispose();
            }

            // If using raycast, use huristic
            gestureLogic = new HPUIGestureLogic(this, TapTimeThreshold, TapDistanceThreshold, InteractionSelectionRadius, useHuristic: useRayCast);
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
        public Vector3 GetCollisionPoint(IHPUIInteractable interactable)
        {
            if (validTargets.TryGetValue(interactable, out InteractionInfo info))
            {
                return info.point;
            }
            return GetAttachTransform(interactable).position;
        }
        #endregion

        struct InteractionInfo
        {
            public float distance;
            public Vector3 point;
            public float huristic;

            public InteractionInfo(float distance, Vector3 point, float huristic=0) : this()
            {
                this.distance = distance;
                this.point = point;
                this.huristic = huristic;
            }
        }
    }
}
 
