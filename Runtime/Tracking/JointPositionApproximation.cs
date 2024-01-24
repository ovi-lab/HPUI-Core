using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    public class JointPositionApproximation: HandSubsystemSubscriber
    {
        private static JointPositionApproximation leftJointPositionApproximation, rightJointPositionApproximation;
        public static JointPositionApproximation LeftJointPositionApproximation {
            get
            {
                if (leftJointPositionApproximation == null)
                {
                    leftJointPositionApproximation = InstantiateObj(Handedness.Left);
                }
                return leftJointPositionApproximation;
            }
        }

        public static JointPositionApproximation RightJointPositionApproximation {
            get
            {
                if (rightJointPositionApproximation == null)
                {
                    rightJointPositionApproximation = InstantiateObj(Handedness.Right);
                }
                return rightJointPositionApproximation;
            }
        }

        private const int windowSize = 100;
        private const float maeThreshold = 0.003f; // 3mm
        private List<XRHandJointID> computeKeypointsJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.MiddleProximal, XRHandJointID.RingProximal, XRHandJointID.LittleProximal
        };
        private Handedness handedness; // TODO make this work for both hands
        private Dictionary<XRHandJointID, (float mean, float mae, bool stable)> jointsLengthEsitmation = new Dictionary<XRHandJointID, (float, float, bool)>();
        private Dictionary<XRHandJointID, Queue<float>> jointsLastLengths = new Dictionary<XRHandJointID, Queue<float>>();
        private Dictionary<XRHandJointID, (Queue<Vector3> positions, Pose pose, bool stable)> computeKeypointJointsData = new Dictionary<XRHandJointID, (Queue<Vector3> poses, Pose pose, bool stable)>();
        private Pose lastWristPose;
        private bool recievedLastWristPose = false;

        public Handedness Handedness { get => handedness; set => handedness = value; }

        /// <inheritdoc />
        protected override void ProcessJointData(XRHandSubsystem subsystem)
        {
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

                    if (!jointsLengthEsitmation.TryGetValue(jointID, out (float mean, float mae, bool stable) estimations))
                    {
                        estimations = (float.MaxValue, float.MaxValue, false);
                        jointsLengthEsitmation.Add(jointID, estimations);
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

                        jointsLengthEsitmation[jointID] = (mean, mae, jointLastLengths.Count == windowSize ? mae < maeThreshold : false);
                    }
                }
            }

            // Compute the poses of the keypoints
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out lastWristPose))
            {
                recievedLastWristPose = true;

                foreach (XRHandJointID jointID in computeKeypointsJoints)
                {
                    if (hand.GetJoint(jointID).TryGetPose(out Pose jointPose))
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
                        data.pose = jointPose;
                        if (data.positions.Count == windowSize)
                        {
                            float mae = data.positions.Skip(1).Zip(data.positions.SkipLast(1), (p1, p2) => (p1 - p2).magnitude).Sum() / data.positions.Count;
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

        public bool TryComputePoseForKeyPoints(List<XRHandJointID> keypoints, out Dictionary<XRHandJointID, Pose> keypointPoses, out float percentageDone)
        {
            keypointPoses = null;
            percentageDone = 0;

            // Was just initiated
            if (jointsLengthEsitmation.Count == 0)
            {
                return false;
            }

            // Checking of all joints
            // FIXME: Optimization - Avoid computing for all joints if not necessary
            float jointLengthsStableRatio = (float)jointsLengthEsitmation.Where(kvp => kvp.Value.stable).Count() / (float)windowSize;
            float computeKeypointsStableRatio = (float)computeKeypointJointsData.Where(kvp => kvp.Value.stable).Count() / (float)windowSize;

            percentageDone = (jointLengthsStableRatio + computeKeypointsStableRatio) * 0.5f;
            if (jointLengthsStableRatio == 1 || computeKeypointsStableRatio == 1 || !recievedLastWristPose)
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

            Pose xrOriginPose = new Pose(xrOriginTransform.position, xrOriginTransform.rotation);
            Pose anchorPose;
            // If only one finger, the orientation can match the corresponding finger's proximal orientation.
            // If not use the middle proximal's orientation.
            if (usedFingers.Count == 1)
            {
                anchorPose = computeKeypointJointsData[XRHandJointIDUtility.FromIndex(usedFingers[0].GetFrontJointID().ToIndex() + 1)].pose;
            }
            else
            {
                anchorPose = computeKeypointJointsData[XRHandJointID.MiddleProximal].pose;
            }

            Vector3 forward = xrOriginTransform.TransformDirection(anchorPose.forward);
            Vector3 up = xrOriginTransform.TransformDirection(anchorPose.up);

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
                        currentPos = xrOriginTransform.TransformPoint(computeKeypointJointsData[jointID].pose.position);
                    }
                    else
                    {
                        currentPos += forward.normalized * jointsLengthEsitmation[jointID].mean;
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

        public void Reset()
        {
            computeKeypointJointsData.Clear();
            jointsLengthEsitmation.Clear();
            jointsLastLengths.Clear();
            computeKeypointJointsData.Clear();
        }

        private static JointPositionApproximation InstantiateObj(Handedness handedness)
        {
            GameObject obj = new GameObject($"JointPositionApproximation_{handedness}");
            JointPositionApproximation jointPositionApproximation = obj.AddComponent<JointPositionApproximation>();
            jointPositionApproximation.handedness = handedness;
            return jointPositionApproximation;
        }
    }
}
