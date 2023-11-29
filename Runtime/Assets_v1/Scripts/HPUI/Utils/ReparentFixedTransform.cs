using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ubc.ok.ovilab.HPUI.utils.Extensions;

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
        public Transform targetRelativeObject;

        public bool matchChildren = false;

        private Dictionary<Transform, Transform> matchingList;
        // Update is called once per frame
        public static void Apply(Transform source,
                                 Transform relativeObject,
                                 Transform targetSource,
                                 Transform targetRelativeObject)
        {
            // TODO: Consider using the mulitple functions instead of single operations?
            targetRelativeObject.position = targetSource.TransformPoint(source.InverseTransformPoint(relativeObject.position));
            Vector3 up = targetSource.TransformDirection(source.InverseTransformDirection(relativeObject.up));
            Vector3 forward = targetSource.TransformDirection(source.InverseTransformDirection(relativeObject.forward));
            targetRelativeObject.transform.rotation = Quaternion.LookRotation(forward, up);
        }


        void Start()
        {
            matchingList = new Dictionary<Transform, Transform>();
            if (matchChildren)
            {
                Transform[] targetTransforms = targetRelativeObject.GetComponentsInChildren<Transform>();
                foreach(Transform t in targetTransforms)
                {
                    Transform otherT = relativeObject.FindDescendentTransform(t.name);
                    if (otherT != null)
                    {
                        matchingList.Add(t, otherT);
                    }
                }
            }
        }

        void Update()
        {
            ReparentFixedTransform.Apply(source, relativeObject, targetSource, targetRelativeObject);

            if (matchChildren)
            {
                foreach(KeyValuePair<Transform, Transform> kvp in matchingList)
                {
                    ReparentFixedTransform.Apply(source, kvp.Value, targetSource, kvp.Key);
                }
            }
        }
    }
}
