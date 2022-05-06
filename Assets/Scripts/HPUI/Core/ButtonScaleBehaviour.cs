using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core
{
    [DefaultExecutionOrder(100)]
    public class ButtonScaleBehaviour : MonoBehaviour
    {
	private Vector3 defaultScale;
	public Transform targetTransform;
	public float scaleFactor = 1.5f;
	// Start is called before the first frame update
	void Start()
	{
	    if (!targetTransform)
		defaultScale = this.transform.localScale;
	}

	public void invokeScaleBehaviour()
	{
	    this.transform.localScale = defaultScale * scaleFactor;
	}

	public void resetScale()
	{
	    this.transform.localScale = defaultScale;
	}
    }
}
