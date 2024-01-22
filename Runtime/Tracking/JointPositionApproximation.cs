using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Tracking
{
    public class JointPositionApproximation: HandSubsystemSubscriber
    {
        private const int windowSize = 10;
        private const float maeThreshold = 0.003f; // 3mm
        private List<XRHandJointID> computeKeypointsJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.MiddleProximal, XRHandJointID.RingProximal, XRHandJointID.LittleProximal
        };
        private Handedness handedness; // TODO make this work for both hands
        private Dictionary<XRHandJointID, (float mean, float mae, bool stable)> jointsLengthEsitmation = new Dictionary<XRHandJointID, (float, float, bool)>();
        private Dictionary<XRHandJointID, Queue<float>> jointsLastLengths = new Dictionary<XRHandJointID, Queue<float>>();
        private Dictionary<XRHandJointID, (Queue<Vector3> positions, bool stable)> computeKeypointJointsData = new Dictionary<XRHandJointID, (Queue<Vector3> poses, bool stable)>();
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
                for(var i = fingerID.GetFrontJointID().ToIndex(); // intermedial
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

                    XRHandJointID parentJointID = XRHandJointIDUtility.FromIndex(i + 1);
                    XRHandJoint joint = hand.GetJoint(jointID);
                    XRHandJoint parentJoint = hand.GetJoint(parentJointID);

                    if (joint.TryGetPose(out Pose jointPose) && joint.TryGetPose(out Pose parentJointPose))
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

                        if (!jointsLengthEsitmation.ContainsKey(jointID))
                        {
                            jointsLengthEsitmation.Add(jointID, (mean, mae, jointLastLengths.Count == windowSize ? mae < maeThreshold : false));
                        }
                    }
                }
            }

            // Compute the poses of the keypoints
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out lastWristPose))
            {
                recievedLastWristPose = true;
                Matrix4x4 wristWorldToLocalMatrix = Matrix4x4.TRS(lastWristPose.position, lastWristPose.rotation, Vector3.one).inverse;

                foreach (XRHandJointID jointID in computeKeypointsJoints)
                {
                    if (hand.GetJoint(jointID).TryGetPose(out Pose jointPose))
                    {
                        if (!computeKeypointJointsData.TryGetValue(jointID, out (Queue<Vector3> positions, bool stable) data))
                        {
                            data = (new Queue<Vector3>(), false);
                            computeKeypointJointsData.Add(jointID, data);
                        }

                        if (data.stable)
                        {
                            continue;
                        }

                        Vector3 localPosition = wristWorldToLocalMatrix.MultiplyPoint3x4(jointPose.position);

                        if (data.positions.Count == windowSize)
                        {
                            data.positions.Dequeue();
                        }

                        data.positions.Enqueue(localPosition);
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

        public bool TryComputePoseForKeyPoints(List<XRHandJointID> keypoints, out Dictionary<XRHandJointID, Pose> keypointPoses, Handedness handedness)
        {
            if (handedness != this.handedness)
            {
                throw new InvalidOperationException("`handedness` is not the one expected.");
            }
            // Checking of all joints
            // FIXME: Optimization - Avoid computing for all joints if not necessary
            bool jointLengthsStable = jointsLengthEsitmation.All(kvp => kvp.Value.stable);
            bool computeKeypointsStable = computeKeypointJointsData.All(kvp => kvp.Value.stable);
            keypointPoses = new Dictionary<XRHandJointID, Pose>();

            if (!jointLengthsStable || !computeKeypointsStable || !recievedLastWristPose)
            {
                return false;
            }

            Vector3 forward = computeKeypointJointsData[XRHandJointID.MiddleProximal].positions.Last();

            // The assumption here is the plane formed by the ring & middle proximal with the wrist would the plane of the hand when held out flat.
            // This is used to compute the up vector to get the rotation.
            (XRHandJointID pivot, XRHandJointID other) pointsForUp = handedness switch
            {
                Handedness.Left => (XRHandJointID.MiddleProximal, XRHandJointID.RingProximal),
                Handedness.Right => (XRHandJointID.RingProximal, XRHandJointID.MiddleProximal),
                _ => throw new InvalidOperationException("`handedness` is invalid.")
            };

            Vector3 pivot = computeKeypointJointsData[pointsForUp.pivot].positions.Last();
            Vector3 up = Vector3.Cross(computeKeypointJointsData[pointsForUp.other].positions.Last() - pivot, - pivot);

            // up & forward in wrist local space
            Matrix4x4 wristLocalToWorldMatrix = Matrix4x4.TRS(lastWristPose.position, lastWristPose.rotation, Vector3.one);

            forward = wristLocalToWorldMatrix.MultiplyVector(forward);
            up = wristLocalToWorldMatrix.MultiplyVector(up);
            Quaternion rotation = Quaternion.LookRotation(forward, up);

            foreach (XRHandFingerID fingerID in Enum.GetValues(typeof(XRHandFingerID)))
            {
                Vector3? currentPos = null;
                for (var i = fingerID.GetFrontJointID().ToIndex() + 1; // proximal
                    i <= fingerID.GetBackJointID().ToIndex();  // until tip
                    i++)
                {
                    XRHandJointID jointID = XRHandJointIDUtility.FromIndex(i);
                    // If this is null, the jointID is that of the proximal
                    if (currentPos == null)
                    {
                        currentPos = wristLocalToWorldMatrix.MultiplyPoint3x4(computeKeypointJointsData[jointID].positions.Last());
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
    }
}
