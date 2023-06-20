using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core
{
    /// <summary>
    /// For all child TransformLinkeres under the object with this script,
    /// Make them relative to this object.
    /// </summary>
    public class TransoformLinkerRelativeModifier : MonoBehaviour
    {
        private List<TransformLinker> linkers;
        // Start is called before the first frame update
        void Start()
        {
            linkers = GetComponentsInChildren<TransformLinker>().ToList();
            foreach (TransformLinker linker in linkers)
            {
                linker.setLocalTransform = true;
                linker.relativeParent = this.transform;
            }
        }
    }
}
