using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core
{
    /// <summary>
    /// The current objects transform is set to match the
    /// `relativeObject`s trasform relative to the `targetSource`s trasform.
    /// Similar to make `targetSource` the parent of this object, and
    /// `source` the parent of `relativeObject`, then change the local
    /// transform of this to match that of `relativeObject` and
    /// unparent.
    /// </summary>
    public class ReparentFixedTransform : MonoBehaviour
    {
        public Transform source;
        public Transform relativeObject;
        public Transform targetSource;

        // Update is called once per frame
        public static void Apply(Transform source,
                                 Transform relativeObject,
                                 Transform targetSource,
                                 Transform targetRelativeObject)
        {
            targetRelativeObject.transform.position = targetSource.TransformPoint(source.InverseTransformPoint(relativeObject.position));
            targetRelativeObject.transform.rotation = relativeObject.rotation * (targetSource.rotation * Quaternion.Inverse(source.rotation));
        }

        void Update()
        {
            ReparentFixedTransform.Apply(source, relativeObject, targetSource, this.transform);
        }
    }
}
