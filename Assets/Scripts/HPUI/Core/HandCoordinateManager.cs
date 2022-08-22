using System.Collections.Generic;
using UnityEngine;
using static HPUI.utils.Extensions;
using System.Linq;
using System;

namespace HPUI.Core
{
    [DefaultExecutionOrder(-140)]
    public class HandCoordinateManager : MonoBehaviour
    {
        public Transform skeletonRoot;
        public List<ProxyMapping> proxyToSeletonNameMapping = new List<ProxyMapping>();
	public GameObject palmBase;

        public List<string> managedCoordinates = new List<string>();
        private Dictionary<string, Transform> managedCoordTransforms = new Dictionary<string, Transform>();

        /// <summary>
        /// Provided the proxy name, return the transform of the skeleton the proxy follows.
        /// </summary>
        public Transform GetLinkedSkeletonTransform(string proxyName)
        {
            var items = proxyToSeletonNameMapping.Where(x => x.ProxyName == proxyName).ToList();
            if (items.Count != 1)
                Debug.LogError("Error: number of proxynames for `" + proxyName + "` is : " + items.Count);
            return GetSkeletonTransform(items[0].SkeletonName);
        }

        /// <summary>
        /// Given a name, return the corresponding transform with the name from the skeleton being proxied.
        /// </summary>
        public Transform GetSkeletonTransform(string descendantName)
        {
            return skeletonRoot.FindDescendentTransform(descendantName);
        }

        /// <summary>
        /// Given a name, return the corresponding transform of the proxy with the name.
        /// </summary>
        public Transform GetProxyTrasnform(string descendentName)
        {
            return this.transform.FindDescendentTransform(descendentName);
        }

        /// <summary>
        /// Provided a proxy name, return the transform of that proxy.
        /// </summary>
        public Transform GetManagedCoord(string name)
        {
            if (managedCoordTransforms.ContainsKey(name))
            {
                return managedCoordTransforms[name];
            }
            return null;
        }

        /// <summary>
        /// Returns the position in the palms frame of reference from a given position in the world frame of reference.
        /// </summary>
	public Vector3 CoordinatesInPalmReferenceFrame(Vector3 worldCoordinates)
	{
	    //worldCoordinates = HandCoordinateGetter.HandToWorldCoords(worldCoordinates);
	    worldCoordinates = palmBase.transform.InverseTransformPoint(worldCoordinates);
	    return worldCoordinates;
	}

        /// <summary>
        /// Returns the position in the world frame of reference from a given position in the palm frame of reference.
        /// </summary>
	public Vector3 PalmToWorldCoords(Vector3 palmCoords)
	{
	    return palmBase.transform.TransformPoint(palmCoords);
	}

        void Start()
        {
            foreach (var name in managedCoordinates)
            {
                managedCoordTransforms[name] = GetProxyTrasnform(name);
            }
            // Debug.Log(string.Join(",", managedCoordTransforms.Select(kvp => kvp.Key + ": " + kvp.Value)));
        }
    }

    [Serializable]
    public class ProxyMapping
    {
        public string ProxyName, SkeletonName;
    }
}
