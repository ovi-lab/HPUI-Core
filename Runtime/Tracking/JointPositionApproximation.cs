using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.UI;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// Approximates world-space joint poses for use by HPUIGeneratedContinuousInteractable by collecting
    /// recent hand joint samples and estimating bone lengths.
    /// </summary>
    /// <remarks>
    /// Gathers pose and length data until stable, computes keypoint poses, applies them to JointFollower
    /// components, and triggers automatic calibration. <see cref="TryComputePoseForKeyPoints"/> for mode
    /// details.
    /// </remarks>
    public class JointPositionApproximation : HandSubsystemSubscriber
    {
        [Tooltip("(Optional) Will be used to provide feedback during setup.")]
        [SerializeField] private HPUIGeneratedContinuousInteractableUI ui;

        private enum ApproximationComputeState { None, Starting, DataCollection, Computing, Finished }
        private Transform dummyXROriginTransform;

        /// <inheritdoc />
        public override Handedness Handedness
        {
            get => handedness;
            set => handedness = value;
        }

        private const int windowSize = 100;
        private const float maeThreshold = 0.003f; // 3mm
        private List<XRHandJointID> computeKeypointsJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.MiddleProximal, XRHandJointID.RingProximal, XRHandJointID.LittleProximal
        };

        private Handedness handedness;
        private Dictionary<XRHandJointID, (float mean, float mae, bool stable)> jointsLengthEstimation = new Dictionary<XRHandJointID, (float, float, bool)>();
        private Dictionary<XRHandJointID, Queue<float>> jointsLastLengths = new Dictionary<XRHandJointID, Queue<float>>();
        private Dictionary<XRHandJointID, (Queue<Vector3> positions, Pose pose, bool stable)> computeKeypointJointsData = new Dictionary<XRHandJointID, (Queue<Vector3> poses, Pose pose, bool stable)>();
        private Pose lastWristPose;
        private bool receivedLastWristPose = false;

        private ApproximationComputeState approximationComputeState = ApproximationComputeState.Starting;
        private JointFollower jointFollower;
        private HPUIGeneratedContinuousInteractable continuousInteractable;

        /// <inheritdoc />
        protected override void ProcessJointData(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags _)
        {
            if (jointFollower == null || !enabled || approximationComputeState != ApproximationComputeState.DataCollection)
            {
                return;
            }

            XRHand hand = handedness switch
            {
                Handedness.Left => subsystem.leftHand,
                Handedness.Right => subsystem.rightHand,
                _ => throw new InvalidOperationException($"Handedness value not valid (got {handedness})")
            };

            // Compute the distances of the joints
            foreach (XRHandFingerID fingerID in Enum.GetValues(typeof(XRHandFingerID)))
            {
                for(var i = fingerID.GetFrontJointID().ToIndex() + 2; // intermedial
                    i <= fingerID.GetBackJointID().ToIndex();  // to tip
                    i++)
                {
                    XRHandJointID jointID = XRHandJointIDUtility.FromIndex(i);

                    if (!jointsLengthEstimation.TryGetValue(jointID, out (float mean, float mae, bool stable) estimations))
                    {
                        estimations = (float.MaxValue, float.MaxValue, false);
                        jointsLengthEstimation.Add(jointID, estimations);
                    }

                    if (estimations.stable)
                    {
                        continue;
                    }

                    XRHandJointID parentJointID = XRHandJointIDUtility.FromIndex(i - 1);
                    XRHandJoint joint = hand.GetJoint(jointID);
                    XRHandJoint parentJoint = hand.GetJoint(parentJointID);

                    if (joint.TryGetPose(out Pose jointPose) && parentJoint.TryGetPose(out Pose parentJointPose))
                    {
                        if (!jointsLastLengths.TryGetValue(jointID, out Queue<float> jointLastLengths))
                        {
                            jointLastLengths = new Queue<float>();
                            jointsLastLengths.Add(jointID, jointLastLengths);
                        }
                        else
                        {
                            jointLastLengths = jointsLastLengths[jointID];
                        }

                        if (jointLastLengths.Count == windowSize)
                        {
                            jointLastLengths.Dequeue();
                        }

                        float distance = (parentJointPose.position - jointPose.position).magnitude;
                        jointLastLengths.Enqueue(distance);

                        float mean = jointLastLengths.Average();
                        float mae = jointLastLengths.Sum(v => Mathf.Abs(v - mean)) / jointLastLengths.Count;

                        jointsLengthEstimation[jointID] = (mean, mae, jointLastLengths.Count == windowSize ? mae < maeThreshold : false);
                    }
                }
            }

            // Compute the poses of the keypoints
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out lastWristPose))
            {
                receivedLastWristPose = true;

                foreach (XRHandJointID jointID in computeKeypointsJoints)
                {
                    XRHandJointID childJointID = XRHandJointIDUtility.FromIndex(jointID.ToIndex() + 1);
                    XRHandJoint joint = hand.GetJoint(jointID);
                    XRHandJoint childJoint = hand.GetJoint(childJointID);

                    if (joint.TryGetPose(out Pose jointPose) && childJoint.TryGetPose(out Pose childJointPose))
                    {
                        if (!computeKeypointJointsData.TryGetValue(jointID, out (Queue<Vector3> positions, Pose pose, bool stable) data))
                        {
                            data = (new Queue<Vector3>(), Pose.identity, false);
                            computeKeypointJointsData.Add(jointID, data);
                        }

                        if (data.stable)
                        {
                            continue;
                        }

                        if (data.positions.Count == windowSize)
                        {
                            data.positions.Dequeue();
                        }

                        data.positions.Enqueue(jointPose.GetTransformedBy(lastWristPose).position);
                        data.pose = new Pose(jointPose.position, Quaternion.LookRotation(childJointPose.position - jointPose.position, jointPose.up));
                        float mae = float.MaxValue;
                        if (data.positions.Count == windowSize)
                        {
                            mae = data.positions.Skip(1).Zip(data.positions.SkipLast(1), (p1, p2) => (p1 - p2).magnitude).Sum() / (data.positions.Count - 1);
                            if (mae < maeThreshold)
                            {
                                data.stable = true;
                            }
                        }

                        computeKeypointJointsData[jointID] = data;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to compute world-space poses for a set of hand joint keypoints using estimated joint lengths,
        /// previously computed keypoint joint poses, and the last known wrist pose.
        ///
        /// The method will only produce poses when the internal joint length estimations and the compute-keypoint
        /// joint data have reached a "stable" state for all required joints and a wrist pose has been received.
        /// While the method is gathering stability information it returns false and sets <paramref name="percentageDone"/>
        /// to indicate progress.
        /// </summary>
        /// <param name="keypoints">A list of XRHandJointID values for which poses are requested.</param>
        /// <param name="keypointPoses">
        /// Output dictionary that, on success, is populated with the computed Pose for each requested joint.
        /// Keys are the requested XRHandJointID values and values are world-space Poses (position and orientation).
        /// If the method returns false this will be set to null.
        /// </param>
        /// <param name="percentageDone">
        /// Output progress indicator in range [0,1] representing the average stability progress of:
        /// - the joint length estimations, and
        /// - the compute-keypoint joint data.
        /// This value is computed as the mean of the two stability ratios and is updated even when the method
        /// returns false while waiting for full stability or a wrist pose.
        /// </param>
        /// <returns>
        /// True if poses were successfully computed for the requested keypoints and written into
        /// <paramref name="keypointPoses"/>; otherwise false. The method returns false when internal data is
        /// uninitialized, not yet stable for all joints, or the last wrist pose has not been received.
        /// </returns>
        /// <remarks>
        /// Behavior and algorithm details:
        /// - The method requires that both the joint length estimations and the compute-keypoint joint data
        ///   are present and fully stable for all relevant joints before computing results.
        /// - Finger selection: the method determines which fingers are relevant to the requested keypoints.
        ///   If only a single finger is involved, that finger's proximal orientation is used as an anchor;
        ///   otherwise the middle finger's proximal orientation is used.
        /// - Orientation: an anchor pose's forward and up vectors are transformed into world space, the forward
        ///   vector is projected onto the plane orthogonal to the hand normal (derived from the wrist up), and
        ///   a look rotation is constructed from the projected forward and a computed up vector. This orientation
        ///   is applied to all computed keypoint poses.
        /// - Positioning: for each relevant finger (the thumb is intentionally skipped for this approximation),
        ///   positions are computed along the computed forward direction starting from the proximal joint position
        ///   (transformed into world space) and stepping by the estimated mean lengths for each subsequent joint.
        /// - The method uses a temporary origin transform (dummy origin) aligned to the XR origin transform to
        ///   convert stored local joint poses into world-space coordinates.
        /// - Only when the method returns true is <paramref name="keypointPoses"/> guaranteed to contain entries
        ///   for the requested joints; otherwise it will be null.
        /// </remarks>
        public bool TryComputePoseForKeyPoints(List<XRHandJointID> keypoints, out Dictionary<XRHandJointID, Pose> keypointPoses, out float percentageDone)
        {
            keypointPoses = null;
            percentageDone = 0;

            // Was just initiated
            if (jointsLengthEstimation.Count == 0 || computeKeypointJointsData.Count == 0)
            {
                return false;
            }

            // Checking of all joints
            // FIXME: Optimization - Avoid computing for all joints if not necessary
            float jointLengthsStableRatio = (float)jointsLengthEstimation.Where(kvp => kvp.Value.stable).Count() / (float)jointsLengthEstimation.Count;
            float computeKeypointsStableRatio = (float)computeKeypointJointsData.Where(kvp => kvp.Value.stable).Count() / (float)computeKeypointJointsData.Count;

            percentageDone = (jointLengthsStableRatio + computeKeypointsStableRatio) * 0.5f;

            if (jointLengthsStableRatio < 1 || computeKeypointsStableRatio < 1 || !receivedLastWristPose)
            {
                return false;
            }

            IEnumerable<int> keyPointsIndex = keypoints.Select(kp => XRHandJointIDUtility.ToIndex(kp));
            List<XRHandFingerID> usedFingers = ListPool<XRHandFingerID>.Get();

            keypointPoses = new Dictionary<XRHandJointID, Pose>();

            foreach (XRHandFingerID fingerID in Enum.GetValues(typeof(XRHandFingerID)))
            {
                if (keyPointsIndex
                    .Select(id => id > fingerID.GetFrontJointID().ToIndex() && id <= fingerID.GetBackJointID().ToIndex())
                    .Any())
                {
                    usedFingers.Add(fingerID);
                }
            }

            if (dummyXROriginTransform == null)
            {
                dummyXROriginTransform = xrOrigin.transform;
            }

            dummyXROriginTransform.position = xrOrigin.transform.position; // + new Vector3(0, xrOrigin.CameraYOffset, 0);
            dummyXROriginTransform.rotation = xrOrigin.transform.rotation;

            Pose xrOriginPose = new Pose(dummyXROriginTransform.position, dummyXROriginTransform.rotation);
            Vector3 handNormal = dummyXROriginTransform.TransformDirection(lastWristPose.up);
            Pose anchorPose;
            // If only one finger, the orientation can match the corresponding finger's proximal orientation.
            // If not use the  orientation of middle proximal.
            if (usedFingers.Count == 1)
            {
                anchorPose = computeKeypointJointsData[XRHandJointIDUtility.FromIndex(usedFingers[0].GetFrontJointID().ToIndex() + 1)].pose;
            }
            else
            {
                anchorPose = computeKeypointJointsData[XRHandJointID.MiddleProximal].pose;
            }

            Vector3 forward = Vector3.ProjectOnPlane(dummyXROriginTransform.TransformDirection(anchorPose.forward), handNormal);
            Vector3 up = Vector3.Cross(Vector3.Cross(forward, dummyXROriginTransform.TransformDirection(anchorPose.up)), forward);

            Quaternion rotation = Quaternion.LookRotation(forward, up);

            foreach (XRHandFingerID fingerID in usedFingers)
            {
                if (fingerID == XRHandFingerID.Thumb)
                {
                    continue;
                }

                Vector3? currentPos = null;
                for (var i = fingerID.GetFrontJointID().ToIndex() + 1; // proximal
                    i <= fingerID.GetBackJointID().ToIndex();  // until tip
                    i++)
                {
                    XRHandJointID jointID = XRHandJointIDUtility.FromIndex(i);
                    // If this is null, the jointID is that of the proximal
                    if (currentPos == null)
                    {
                        currentPos = dummyXROriginTransform.TransformPoint(computeKeypointJointsData[jointID].pose.position);
                    }
                    else
                    {
                        currentPos += forward.normalized * jointsLengthEstimation[jointID].mean;
                    }

                    if (keypoints.Contains(jointID))
                    {
                        keypointPoses.Add(jointID, new Pose((Vector3)currentPos, rotation));
                    }
                }
            }

            ListPool<XRHandFingerID>.Release(usedFingers);

            return true;
        }

        /// <summary>
        /// Restart the automated computation procedure.
        /// </summary>
        public void AutomatedRecompute()
        {
            approximationComputeState = ApproximationComputeState.Starting;
            jointsLengthEstimation.Clear();
            jointsLastLengths.Clear();
            computeKeypointJointsData.Clear();
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            jointFollower = GetComponent<JointFollower>();
            continuousInteractable = GetComponent<HPUIGeneratedContinuousInteractable>();

            // FIXME: figure out a better way for this sorcery!
            if (jointFollower != null && continuousInteractable != null)
            {
                handedness = jointFollower.Handedness;
                base.OnEnable();
            }
            else
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Computes joint pose approximations from the provided keypoint poses, applies them to the
        /// joint followers, and executes calibration on the associated continuous interactable.
        /// </summary>
        /// <param name="keypointPoses">Mapping from XRHandJointID to Pose containing the source poses
        /// for approximation. Must contain all joint IDs referenced by the joint follower datum and
        /// keypoint followers.</param>
        /// <remarks>
        /// Sets up keypoints, disables joint followers, assigns base and optional second joint poses to
        /// the main follower, applies poses to each keypoint follower, runs calibration, and updates the
        /// approximation state.
        /// </remarks>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown if a required joint
        /// ID is missing from <paramref name="keypointPoses"/>.</exception>
        protected virtual void ComputeApproximationAndExecuteCalibration(Dictionary<XRHandJointID, Pose> keypointPoses)
        {
            continuousInteractable.SetupKeypoints();

            foreach (Transform t in continuousInteractable.KeypointTransforms)
            {
                t.GetComponent<JointFollower>().enabled = false;
            }
            jointFollower.enabled = false;

            Pose newPose1, newPose2 = Pose.identity;
            XRHandJointID jointID;

            // Setting the base as the other keypoints can be childed to this
            jointID = jointFollower.JointFollowerDatumProperty.Value.jointID;
            newPose1 = keypointPoses[jointID];

            jointID = jointFollower.JointFollowerDatumProperty.Value.secondJointID;
            bool useSecondJointID = jointFollower.JointFollowerDatumProperty.Value.useSecondJointID;
            if (useSecondJointID)
            {
                newPose2 = keypointPoses[jointID];
            }
            jointFollower.InternalSetPose(newPose1, newPose2, useSecondJointID);

            foreach (Transform t in continuousInteractable.KeypointTransforms)
            {
                JointFollower kpJointFollower = t.GetComponent<JointFollower>();
                jointID = kpJointFollower.JointFollowerDatumProperty.Value.jointID;
                newPose1 = keypointPoses[jointID];
                kpJointFollower.InternalSetPose(newPose1, Pose.identity, false);
            }

            continuousInteractable.ExecuteCalibration();
            Debug.Log($"Finished generating");
            approximationComputeState = ApproximationComputeState.Finished;
        }

        /// <inheritdoc />
        protected override void Update()
        {
            base.Update();

            if (jointFollower == null)
            {
                Debug.LogWarning($"Not running Approximation routine as there is no JointFollower");
                return;
            }

            switch (approximationComputeState)
            {
                case ApproximationComputeState.Starting:
                    continuousInteractable.colliders.Clear();
                    continuousInteractable.ClearKeypointsCache();
                    ui?.Show();
                    approximationComputeState = ApproximationComputeState.DataCollection;
                    Debug.Log($"Started approximation");
                    break;
                case ApproximationComputeState.DataCollection:
                    IEnumerable<XRHandJointID> keypointsUsed = continuousInteractable.KeypointsData
                        .Where(kp => kp.keypointType != DeformableSurfaceKeypoint.KeypointsOptions.Transform)
                        .Select(kp => kp.keypointType switch
                        {
                            DeformableSurfaceKeypoint.KeypointsOptions.JointID => new List<XRHandJointID>() { kp.jointID },
                            DeformableSurfaceKeypoint.KeypointsOptions.JointFollowerData => kp.jointFollowerData.Value.JointsUsed(),
                            _ => throw new InvalidOperationException()
                        })
                        .SelectMany(kps => kps)
                        .Append(jointFollower.JointFollowerDatumProperty.Value.jointID);
                    if (jointFollower.JointFollowerDatumProperty.Value.useSecondJointID)
                    {
                        keypointsUsed = keypointsUsed.Append(jointFollower.JointFollowerDatumProperty.Value.secondJointID);
                    }

                    if (TryComputePoseForKeyPoints(keypointsUsed.ToList(),
                                                   out Dictionary<XRHandJointID, Pose> keypointPoses,
                                                   out float percentageDone))
                    {
                        ui?.Hide();
                        Debug.Log($"Finished collecting data for approximation");
                        approximationComputeState = ApproximationComputeState.Computing;
                        ComputeApproximationAndExecuteCalibration(keypointPoses);
                    }
                    else
                    {
                        if (ui != null)
                        {
                            ui.TextMessage = "Processing hand pose";
                            if (percentageDone >= 1)
                            {
                                ui.InProgress();
                            }
                            else
                            {
                                ui.SetProgress(percentageDone);
                            }
                        }
                    }
                    break;
                case ApproximationComputeState.Computing:
                    // Nothing to do here
                    break;
                case ApproximationComputeState.Finished:
                    foreach (Transform t in continuousInteractable.KeypointTransforms)
                    {
                        t.GetComponent<JointFollower>().enabled = true;
                    }
                    jointFollower.enabled = true;
                    approximationComputeState = ApproximationComputeState.None;
                    break;
                default:
                    break;
            }
        }
    }
}
