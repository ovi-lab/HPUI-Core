using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ubco.ovilab.HPUI.utils;

namespace ubco.ovilab.HPUI.Interaction
{
    /// <summary>
    /// Base HPUI interactor. Selects/hovers only the closest interactable for a given zOrder.
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
        [Tooltip("TODO")]
        [SerializeReference, SubclassSelector]
        private IHPUIDetectionLogic interactablesDetector = new HPUIFullRangeRayCastDetectionLogic();

        /// <summary>
        /// TODO
        /// </summary>
        public IHPUIDetectionLogic InteractablesDetector { get => interactablesDetector; set => value = interactablesDetector; }

        [Tooltip("The gesture logic used by the interactor")]
        [SerializeReference, SubclassSelector]
        private IHPUIGestureLogic gestureLogic = new HPUIGestureLogic(0.3f, 0.2f, 0.05f);

        /// <summary>
        /// The gesture logic used by the interactor
        /// </summary>
        public IHPUIGestureLogic GestureLogic { get => gestureLogic; set => value = gestureLogic; }

        private Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets = new();

// #if UNITY_EDITOR
//         private bool onValidateUpdate;
// #endif

        // protected bool receivedNewJointData = false,
        //     flipZAngles = false;

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

        // protected void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        // {
        //     foreach(XRHandJointID id in trackedJoints)
        //     {
        //         if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
        //         {
        //             jointLocations[id] = xrOriginTransform.TransformPoint(pose.position);
        //             receivedNewJointData = true;
        //         }
        //     }
        // }

        // protected void ResetAngleFunctions()
        // {
        //     xrHandTrackingEvents = GetComponent<XRHandTrackingEvents>();
        //     xrHandTrackingEvents.handedness = handedness switch {
        //         InteractorHandedness.Right => Handedness.Right,
        //         InteractorHandedness.Left => Handedness.Left,
        //         _ => Handedness.Invalid
        //     };
        //     flipZAngles = handedness == InteractorHandedness.Left;
        // }

// #if UNITY_EDITOR
//         /// <inheritdoc />
//         protected void OnValidate()
//         {
//             if (Application.isPlaying && gameObject.activeInHierarchy)
//             {
//                 // NOTE: some of the setup running in the respective methods are not compatible with
//                 // OnValidate as they trigger many SendMessage calls
//                 onValidateUpdate = true;
//             }
//         }
// #endif

//         /// <inheritdoc />
//         protected void Update()
//         {
// #if UNITY_EDITOR
//             if (onValidateUpdate)
//             {
//                 onValidateUpdate = false;
//             }
// #endif
//         }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
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

                UnityEngine.Profiling.Profiler.BeginSample("interactableDetection");
                try
                {
                    InteractablesDetector.DetectedInteractables(this, interactionManager, validTargets, out hoverEndPoint);
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
                    GestureLogic.Update(this, validTargets);
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
            IEnumerable<IHPUIInteractable> filteredValidTargets = validTargets.Select(el => el.Key);
            targets.AddRange(filteredValidTargets);
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            bool canSelect = ProcessSelectFilters(interactable);
            return canSelect && (!SelectOnlyPriorityTarget || GestureLogic.IsPriorityTarget(interactable as IHPUIInteractable));
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
 
