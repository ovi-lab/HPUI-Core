using System.Collections;
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
        
        public Transform getLinkedSkeletonTransform(string proxyName)
        {
            var items = proxyToSeletonNameMapping.Where(x => x.ProxyName == proxyName).ToList();
            if (items.Count != 1)
                Debug.LogError("Error: number of proxynames for `" + proxyName + "` is : " + items.Count);
            return getSkeletonTransform(items[0].SkeletonName);
        }

        public Transform getSkeletonTransform(string descendantName)
        {
            return skeletonRoot.FindDescendentTransform(descendantName);
        }

        public Transform getProxyTrasnform(string descendentName)
        {
            return this.transform.FindDescendentTransform(descendentName);
        }

        public Transform getManagedCoord(string name)
        {
            if (managedCoordTransforms.ContainsKey(name))
            {
                return managedCoordTransforms[name];
            }
            return null;
        }

	public Vector3 CoordinatesInPalmReferenceFrame(Vector3 worldCoordinates)
	{
	    //worldCoordinates = HandCoordinateGetter.HandToWorldCoords(worldCoordinates);
	    worldCoordinates = palmBase.transform.InverseTransformPoint(worldCoordinates);
	    return worldCoordinates;
	}

	public Vector3 PalmToWorldCoords(Vector3 palmCoords)
	{
	    return palmBase.transform.TransformPoint(palmCoords);
	}

        void Start()
        {
            foreach (var name in managedCoordinates)
            {
                managedCoordTransforms[name] = getProxyTrasnform(name);
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
