using UnityEngine;

namespace HPUI.Core
{
    [DefaultExecutionOrder(-130)]
    public class TransformLinker : MonoBehaviour
    {
	public Transform parent;
	public Transform secondParent;

        [Tooltip("When ParentName/SecondParentName is provided, use this to get the transform to parent." +
        "If not provided will use the HandIndex to get the HandCoordinateManager from the global HandsManager.")]
        public HandCoordinateManager handCoordinateManager;
        public int handIndex = 0;
        public string parentName;
        public string secondParentName;
        [Tooltip("If checked, the transform linker would set the localPosition and localRotation.")]
        public bool setLocalTransform = false;
        [Tooltip("If setLocalTransform and this is set, the linked transform will be relative to the object set to relativeParent.")]
        public Transform relativeParent;

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
                        parent = manager.GetLinkedSkeletonTransform(name);
                }
                else
                {
                    if (handCoordinateManager == null)
                    {
                        handCoordinateManager = HandsManager.instance.handCoordinateManagers[handIndex];
                    }
                    parent = handCoordinateManager.GetProxyTrasnform(parentName);
                    if (!string.IsNullOrEmpty(secondParentName))
                    {
                        secondParent = handCoordinateManager.GetProxyTrasnform(secondParentName);
                    }
                }
            }
        
	}

	// Update is called once per frame
	void Update()
	{
            Vector3 newPosition;
            Quaternion newRotation;
	    if (secondParent)
	    {
		Vector3 interDirection = secondParent.position - parent.position;
		if (interDirection != Vector3.zero)
		{
                    newPosition = parent.position + (interDirection) * 0.5f;
                    newRotation = Quaternion.LookRotation((secondParent.forward - parent.forward) / 2 + parent.forward, parent.up);
		}
                else
                {
                    newPosition = parent.position;
                    newRotation = parent.rotation;
                }
	    }
	    else
	    {
		newPosition = parent.position;
		newRotation = parent.rotation;
	    }

            if (setLocalTransform)
            {
                if (relativeParent == null)
                {
                    this.transform.localPosition = newPosition;
                    this.transform.localRotation = newRotation;
                }
                else
                {
                    if (transform.name == "R1D1_base")
                        Debug.Log((relativeParent.position + newPosition).ToString("F4"));
                    this.transform.position = relativeParent.position + newPosition;
                    this.transform.rotation = relativeParent.rotation * newRotation;
                }
            }
            else
            {
                this.transform.position = newPosition;
                // this.transform.rotation = Quaternion.Slerp(parent.rotation, secondParent.rotation, 0.5f);
                this.transform.rotation = newRotation;
            }
	}
    }
}
