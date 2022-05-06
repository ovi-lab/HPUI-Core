using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core
{
    [DefaultExecutionOrder(-130)]
    public class TransformLinker : MonoBehaviour
    {
	public Transform parent;
	public Transform secondParent;

        public int handIndex = 0;
        public string parentName;
        
	// Start is called before the first frame update
	void Start()
	{
            if (!parent)
            {
                if (parentName == null || parentName == "")
                {
                    var name = transform.name;
                    HandCoordinateManager manager = GetComponentInParent(typeof(HandCoordinateManager)) as HandCoordinateManager;
                    if (!manager)
                        Debug.LogError("Transform linker without `parentName` or `parent` defined alllowed only in decendednts of `HandCoordinateManager`.");
                    else
                        parent = manager.getLinkedSkeletonTransform(name);
                }
                else
                {
                    parent = HandsManager.instance.handCoordinateManagers[handIndex].getProxyTrasnform(parentName);
                }
            }
        
	}

	// Update is called once per frame
	void Update()
	{
	    if (secondParent)
	    {
		Vector3 interDirection = secondParent.position - parent.position;
		if (interDirection != Vector3.zero)
		{
		    this.transform.position = parent.position + (interDirection) * 0.5f;
		    // this.transform.rotation = Quaternion.Slerp(parent.rotation, secondParent.rotation, 0.5f);
		    this.transform.rotation = Quaternion.LookRotation((secondParent.forward - parent.forward)/2 + parent.forward, parent.up);
		}
	    }
	    else
	    {
		this.transform.position = parent.position;
		this.transform.rotation = parent.rotation;
	    }
	}
    }
}
