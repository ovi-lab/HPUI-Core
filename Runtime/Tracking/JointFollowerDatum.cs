using System;
using UnityEngine;
using Unity.XR.CoreUtils.Datums;

namespace ubco.ovilab.HPUI.Tracking
{
    /// <summary>
    /// <see cref="ScriptableObject"/> container class that holds a <see cref="JointFollowerData"/> value.
    /// </summary>
    [CreateAssetMenu(fileName = "JointFollowerData", menuName = "HPUI/Joint Follower Data", order = 1)]
    public class JointFollowerDatum: Datum<JointFollowerData>
    {
    }

    /// <summary>
    /// Serializable container class that holds a JointFollower data value or container asset reference.
    /// </summary>
    /// <seealso cref="JointFollowerData"/>
    [Serializable]
    public class JointFollowerDatumProperty: DatumProperty<JointFollowerData, JointFollowerDatum>
    {
        /// <inheritdoc/>
        public JointFollowerDatumProperty(JointFollowerData value) : base(value)
        {
        }

        /// <inheritdoc/>
        public JointFollowerDatumProperty(JointFollowerDatum datum) : base(datum)
        {
        }
    }
}
