using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core
{
    public class AddOffsetToLinkers : MonoBehaviour
    {
        public bool applyTransform = false;
        public Vector3 positionOffset = Vector3.zero;
        public Quaternion rotationOffset = Quaternion.identity;

        private List<TransformLinker> linkers = new List<TransformLinker>();

        /// <summary>
        /// Recursively collect all TransformLinker objects
        /// </summary>
        private void FindTransformsFormChildren(Transform iTransform)
        {
            int ChildCount = iTransform.childCount;
            for (int i = 0; i < ChildCount; ++i)
            {
                Transform Child = iTransform.GetChild(i);
                TransformLinker transformLinker = Child.GetComponent<TransformLinker>();
                if (transformLinker != null)
                {
                    linkers.Add(transformLinker);
                }
                FindTransformsFormChildren(Child);
            }
        }

        /// <summary>
        /// Apply the positionOffset and rotationOffset to the localPosition and localRotation of each
        /// child of each transformlinker in the linkers list.
        /// </summary>
        private void ApplyOffset()
        {
            if (applyTransform)
            {
                foreach (TransformLinker linker in linkers)
                {
                    Transform t = linker.transform;
                    int childCount = linker.transform.childCount;
                    for (int i = 0; i < childCount; ++i) {
                        t.GetChild(i).localPosition += positionOffset;
                        t.GetChild(i).localRotation *= rotationOffset;
                    }
                }
            }
        }

        // NOTE: this could be modified to be dynamic?
        void Start()
        {
            FindTransformsFormChildren(this.transform);
            ApplyOffset();
        }
    }
}
