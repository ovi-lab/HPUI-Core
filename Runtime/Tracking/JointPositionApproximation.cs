using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        private const int windowSize = 10;
        private const float maeThreshold = 0.003f; // 3mm
        private List<XRHandJointID> computeKeypointsJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.MiddleProximal, XRHandJointID.RingProximal, XRHandJointID.LittleProximal
        };
        private Handedness handedness; // TODO make this work for both hands
        private Dictionary<XRHandJointID, (float mean, float mae, bool stable)> jointsLengthEsitmation = new Dictionary<XRHandJointID, (float, float, bool)>();
        private Dictionary<XRHandJointID, Queue<float>> jointsLastLengths = new Dictionary<XRHandJointID, Queue<float>>();
        private Dictionary<XRHandJointID, (Queue<Vector3> positions, Vector3 position, bool stable)> computeKeypointJointsData = new Dictionary<XRHandJointID, (Queue<Vector3> poses, Vector3 position, bool stable)>();
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
                        if (!computeKeypointJointsData.TryGetValue(jointID, out (Queue<Vector3> positions, Vector3 position, bool stable) data))
                        {
                            data = (new Queue<Vector3>(), Vector3.zero, false);
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
                        data.position = jointPose.position;
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

            keypointPoses = new Dictionary<XRHandJointID, Pose>();

            Pose xrOriginPose = new Pose(xrOriginTransform.position, xrOriginTransform.rotation);
            lastWristPose = lastWristPose.GetTransformedBy(xrOriginPose);

            Vector3 forward = xrOriginTransform.TransformPoint(computeKeypointJointsData[XRHandJointID.MiddleProximal].position) - lastWristPose.position;

            // The assumption here is the plane formed by the ring & middle proximal with the wrist would the plane of the hand when held out flat.
            // This is used to compute the up vector to get the rotation.
            (XRHandJointID pivot, XRHandJointID other) pointsForUp = handedness switch
            {
                Handedness.Right => (XRHandJointID.MiddleProximal, XRHandJointID.RingProximal),
                Handedness.Left => (XRHandJointID.RingProximal, XRHandJointID.MiddleProximal),
                _ => throw new InvalidOperationException("`handedness` is invalid.")
            };

            Vector3 pivot = xrOriginTransform.TransformPoint(computeKeypointJointsData[pointsForUp.pivot].position);
            Vector3 up = Vector3.Cross(xrOriginTransform.TransformPoint(computeKeypointJointsData[pointsForUp.other].position) - pivot, lastWristPose.position - pivot);

            Quaternion rotation = Quaternion.LookRotation(forward, up);

            foreach (XRHandFingerID fingerID in Enum.GetValues(typeof(XRHandFingerID)))
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
                        currentPos = xrOriginTransform.TransformPoint(computeKeypointJointsData[jointID].position);
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

            return true;
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
