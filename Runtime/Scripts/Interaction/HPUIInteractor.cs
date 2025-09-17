using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ubco.ovilab.HPUI.Core.Utils;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ubco.ovilab.HPUI.Core.Interaction
{
    /// <summary>
    /// Base HPUI interactor.
    ///
    /// NOTES:
    /// - The target filter is exectued after <see cref="DetectionLogic"/> and before the <see cref="GestureLogic"/>.
    ///   It does not consider the order as this uses the heuristic reported by the <see cref="DetectionLogic"/>. 
    ///   The heuristic data can be accessed with <see cref="GetHPUIInteractionInfo"/>.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class HPUIInteractor: XRBaseInteractor, IHPUIInteractor
    {
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
        [Tooltip("If true, select only happens for the target with highest priority.")]
        private bool selectOnlyPriorityTarget = true;

        /// <summary>
        /// If true, select only happens for the target with the highest priority.
        /// </summary>
        public bool SelectOnlyPriorityTarget { get => selectOnlyPriorityTarget; set => selectOnlyPriorityTarget = value; }

        [Space()]
        [Tooltip("The detection logic used to detect interactables interacting in current frame and compute the heuristic.")]
        [SerializeReference, SubclassSelector]
        private IHPUIDetectionLogic detectionLogic = new HPUIFullRangeRayCastDetectionLogic();

        /// <summary>
        /// The detection logic used to detect interactables interacting in current frame and compute the heuristic.
        /// </summary>
        public IHPUIDetectionLogic DetectionLogic
        {
            get => detectionLogic;
            set
            {
                detectionLogic = value;
                detectionLogic?.Reset();
            }
        }

        [Tooltip("The gesture logic used by the interactor")]
        [SerializeReference, SubclassSelector]
        private IHPUIGestureLogic gestureLogic = new HPUIGestureLogic(0.3f, 0.2f, 0.05f);

        /// <summary>
        /// The gesture logic used by the interactor
        /// </summary>
        public IHPUIGestureLogic GestureLogic
        {
            get => gestureLogic;
            set
            {
                gestureLogic = value;
                gestureLogic?.Reset();
            }
        }

        private Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets = new();
        static readonly List<IXRInteractable> tempValidTargets = new(); // reusable list when processing valid targets
        static readonly List<IXRInteractable> tempResults = new(); // reusable list when processing valid targets
        private HPUIGesture gestureToTrigger = HPUIGesture.None;
        private HPUIInteractionEventArgs gestureEventArgs;
        private IHPUIInteractable priorityInteractable;
        private LinkedPool<HPUITapEventArgs> hpuiTapEventArgsPool = new LinkedPool<HPUITapEventArgs>(
            () => new HPUITapEventArgs(),
            actionOnRelease: (args) => args.SetParams(null, null, Vector2.zero),
            maxSize: 100
        );
        private LinkedPool<HPUIGestureEventArgs> hpuiGestureEventArgsPool = new LinkedPool<HPUIGestureEventArgs>(
            () => new HPUIGestureEventArgs(),
            actionOnRelease: (args) => args.SetParams(null, null, HPUIGestureState.Invalid, 0, 0, Vector2.zero, Vector2.zero, 0, Vector2.zero, null, Vector2.zero),
            maxSize: 100
        );
        private HPUITapEventArgs tapArgsToPopulate;
        private HPUIGestureEventArgs gestureArgsToPopulate;

#if UNITY_EDITOR
        private bool onValidateUpdate;
#endif

        #region Unity methods
        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            keepSelectedTargetValid = true;
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        protected virtual void OnValidate()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                // NOTE: some of the setup running in the respective methods are not compatible with
                // OnValidate as they can trigger many SendMessage calls
                onValidateUpdate = true;
            }
        }
#endif

        /// <inheritdoc />
        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (onValidateUpdate)
            {
                onValidateUpdate = false;
                DetectionLogic?.Reset();
                GestureLogic?.Reset();
            }
#endif
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            DetectionLogic?.Reset();
            GestureLogic?.Reset();
        }
        #endregion

        #region XRI methods
        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            UnityEngine.Profiling.Profiler.BeginSample("HPUIInteractor.ProcessInteractor");
            // Following the logic in XRPokeInteractor
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                validTargets.Clear();
                tempValidTargets.Clear();
                tempResults.Clear();

                Transform attachTransform = GetAttachTransform(null);
                Vector3 interactionPoint = attachTransform.position;
                Vector3 hoverEndPoint = attachTransform.position;

                UnityEngine.Profiling.Profiler.BeginSample("interactableDetection");
                try
                {
                    DetectionLogic.DetectedInteractables(this, interactionManager, validTargets, out hoverEndPoint);
                    tempResults.AddRange(validTargets.Keys);

                    // Remove interactables that will not get triggered because of the mismatch between the interactionLayers
                    for (int i = 0; i < tempResults.Count; ++i)
                    {
                        IXRInteractable interactable = tempResults[i];
                        if ((this.interactionLayers & interactable.interactionLayers) == 0)
                        {
                            validTargets.Remove(interactable as IHPUIInteractable);
                        }
                        else
                        {
                            tempValidTargets.Add(interactable);
                        }
                    }
                    tempResults.Clear();

                    IXRTargetFilter filter = targetFilter;
                    if (filter != null && filter.canProcess)
                    {
                        filter.Process(this, tempValidTargets, tempResults);

                        // Copy results elements to targets
                        tempValidTargets.Clear();
                        tempValidTargets.AddRange(validTargets.Keys);
                        foreach (IXRInteractable interactable in tempValidTargets)
                        {
                            if (!tempResults.Contains(interactable))
                            {
                                validTargets.Remove(interactable as IHPUIInteractable);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Something went wrong: {e}\n{e.StackTrace}");
                    validTargets.Clear();
                }
                UnityEngine.Profiling.Profiler.EndSample();

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
                    tapArgsToPopulate = hpuiTapEventArgsPool.Get();
                    gestureArgsToPopulate = hpuiGestureEventArgsPool.Get();
                    GestureLogic.ComputeInteraction(this, validTargets, out gestureToTrigger, out priorityInteractable, tapArgsToPopulate, gestureArgsToPopulate);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                switch(gestureToTrigger)
                {
                    case HPUIGesture.Tap:
                        try
                        {
                            if (priorityInteractable != null)
                            {
                                priorityInteractable.OnTap(tapArgsToPopulate);
                            }
                        }
                        finally
                        {
                            // NOTE: There can be interactables that don't take any events. Even
                            // when that happens, the interactor's events should get triggered.
                            tapEvent?.Invoke(tapArgsToPopulate);
                        }
                        break;
                    case HPUIGesture.Gesture:
                        try
                        {
                            if (priorityInteractable != null)
                            {
                                priorityInteractable.OnGesture(gestureArgsToPopulate);
                                if (gestureArgsToPopulate.State == HPUIGestureState.Canceled)
                                {
                                    // Since the ProcessInteractor is called much later in the update loop
                                    // this should not cause any issues. Instead of the manager firing the
                                    // SelectionExit event in the next cycle, we force it here.
                                    this.interactionManager.SelectExit(this, priorityInteractable);
                                    priorityInteractable = null;
                                }
                            }
                        }
                        finally
                        {
                            // NOTE: See note when tap gets triggered.
                            gestureEvent?.Invoke(gestureArgsToPopulate);
                        }
                        break;
                }

                hpuiTapEventArgsPool.Release(tapArgsToPopulate);
                hpuiGestureEventArgsPool.Release(gestureArgsToPopulate);
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            base.GetValidTargets(targets);

            targets.Clear();
            IEnumerable<IHPUIInteractable> filteredValidTargets = validTargets.Select(el => el.Key);
            targets.AddRange(filteredValidTargets);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            bool canSelect = ProcessSelectFilters(interactable);
            return canSelect && (!SelectOnlyPriorityTarget || priorityInteractable == interactable);
        }
        #endregion

        #region IHPUIInteractor interface
        /// <inheritdoc />
        /// <seealso cref="GetHPUIInteractionData"/>
        public bool GetDistanceInfo(IHPUIInteractable interactable, out DistanceInfo distanceInfo)
        {
            if (GetHPUIInteractionInfo(interactable, out HPUIInteractionInfo info))
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

        /// <summary>
        /// Returns the corresponding <see cref="HPUIInteractionInfo"/> for a given interactable in the current frame.
        /// If the interactable is not interacted with in the current frame, return false.
        /// </summary>
        /// <seealso cref="GetDistanceInfo"/>
        public bool GetHPUIInteractionInfo(IHPUIInteractable interactable, out HPUIInteractionInfo hpuiInteractionData)
        {
            return validTargets.TryGetValue(interactable, out hpuiInteractionData);
        }
    }
}
