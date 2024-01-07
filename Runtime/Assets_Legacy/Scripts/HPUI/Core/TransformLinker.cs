using UnityEngine;

namespace ubco.ovilab.HPUI.Legacy
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

        private Vector3 GetCombiendDirection(Vector3 p1, Vector3 p2, Vector3 direction1, Vector3 direction2, Vector3 direction3)
        {
            // p1 + t1*d1 + t3*d3 = t2*p2 + p2
            // t1 * d1 - t2 * d2 + t3 * d3 = p2 - p1

            Vector3 b = p2 - p1;
            double[,] A = new double[3, 3] {
                { direction1.x, -direction2.x, direction3.x },
                { direction1.y, -direction2.y, direction3.y },
                { direction1.z, -direction2.z, direction3.z }
            };
            double[] B = new double[3] { b.x, b.y, b.z };
            double[] result;
            alglib.densesolverreport report;
            alglib.rmatrixsolve(A, 3, B, out result, out report);

            if (report.terminationtype <= 0)
            {
                return direction1;
            }
            float t1 = (float)result[0];
            float t3 = (float)result[2];

            // Quick tests:
            // Debug.Log($"{result[0]} {result[1]}  {result[2]}");
            // GetCombiendDirection(Vector3.zero, new Vector3(6, -4, 27), new Vector3(1, 0, 2), new Vector3(-1, -2, -5), new Vector3(1, 5, -1));
            // should print 5, 3, -2
            // GetCombiendDirection(new Vector3(0, 0, 0), new Vector3(-4, 14, -2), new Vector3(2, 3, 5), new Vector3(-3, 2, -1), new Vector3(-4,  5, -3));
            // should print 1, 2, 3

            Vector3 targetPoint = p1 + t1 * direction1 + 0.5f * t3 * direction3;

            Vector3 center = (p2 + p1) / 2;
            Vector3 targetDirection = targetPoint - center;

            // if d1 and d2 are diverging, targetPoint will be behind the center.
            if (Vector3.Dot(targetDirection, direction1) < 0)
            {
                return -targetDirection;
            }
            return targetDirection;
        }

        private Vector3 GetCombiendDirection(Vector3 p1, Vector3 p2, Vector3 direction1, Vector3 direction2)
        {
            return GetCombiendDirection(p1, p2, direction1, direction2, Vector3.Cross(direction1, direction2));
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
		}
                else
                {
                    newPosition = parent.position;
                }

                newRotation = Quaternion.LookRotation(GetCombiendDirection(parent.position, secondParent.position, parent.forward, secondParent.forward),
                                                      GetCombiendDirection(parent.position, secondParent.position, parent.up, secondParent.up));
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
