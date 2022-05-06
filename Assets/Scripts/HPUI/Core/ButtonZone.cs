using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HPUI.Core
{
    public class ButtonZone : MonoBehaviour
    {
	[SerializeField]
	public Type Zone;
	public State state { get; set; }
	public Vector3 colliderPosition { get; private set; }
	public Vector3 selfPosition { get; private set; }
	public Vector3 contactPoint { get; private set; }
	public Matrix4x4 worldToLocalMatrix { get; private set; }
	public Matrix4x4 parentWorldToLocalMatrix { get; private set; }
	public Vector3 selfScale { get; private set; }
	public Vector3 colliderScale { get; private set; }
	public Vector3 selfForward { get; private set; }
	public Vector3 contactPlanePoint { get; private set; }
	public Vector3 colliderSurfacePoint { get; private set; }

	private Collider other;
	private Vector3 _selfPosition, otherPosition;
	private Quaternion _selfRotation, otherRotation;

	public enum Type
	{
	    proximal,
	    contact
	}

	public enum State
	{
	    inside,
	    outside
	}

	// Start is called before the first frame update
	void Start()
	{
	    state = State.outside;
	}

	// void Update()
	// {
	// 	if (Zone == Type.contact){
	// 	    //Debug.Log("--- " + this.GetComponent<Collider>().bounds.center.ToString("F5")  + "  " + this.GetComponent<Collider>().bounds.size.ToString("F5") + "  " + this.GetComponent<Collider>().bounds.min.ToString("F5")  + "  " + this.GetComponent<Collider>().bounds.max.ToString("F5"));
	// 	    if (transform.childCount > 0){
	// 		Debug.Log((transform.GetChild(0).position - transform.GetChild(1).position).magnitude);
	// 	    }
	// 	}
	// }


	public void replicateObject(string suffix)
	{
	    var ob = Instantiate(other.transform.GetChild(0).gameObject, otherPosition, otherRotation);
	    ob.transform.localScale = other.transform.lossyScale;
	    ob.transform.name += suffix;
	    ob.SetActive(false);
	    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);	
	    sphere.transform.parent = ob.transform;
	    sphere.transform.localScale = Vector3.one;
	    sphere.transform.position = Vector3.zero;
	    ob = Instantiate(this.gameObject, _selfPosition, _selfRotation);
	    ob.transform.localScale = this.transform.lossyScale;
	    ob.transform.name += suffix;
	    ob.SetActive(false);
	}
    
	void OnTriggerEnter(Collider other)
	{
	    triggerBehaviour(other);
	}

	void OnTriggerStay(Collider other)
	{
	    triggerBehaviour(other);
	}

	void triggerBehaviour(Collider other)
	{
	    if (other.GetComponent<ThumbCollider>() == null)
		return;
	    
	    state = State.inside;
	    if (Zone == Type.contact){
		this._selfPosition = this.transform.position;
		this._selfRotation = this.transform.rotation;
		this.other = other;
		this.otherPosition = other.transform.position;
		this.otherRotation = other.transform.rotation;
		colliderPosition = other.transform.position;

		// var pos  = this.transform.localPosition;
		// pos.z = 0.00025f;
	    
		selfPosition =  this.transform.position; //this.transform.TransformPoint(pos);
		contactPoint = this.GetComponent<Collider>().ClosestPoint(other.transform.position);
		worldToLocalMatrix = this.transform.worldToLocalMatrix;
		parentWorldToLocalMatrix = this.transform.parent.parent.worldToLocalMatrix;
		selfScale = this.transform.lossyScale;
		selfForward = this.transform.forward;
		colliderScale = other.transform.lossyScale;
		contactPlanePoint = (new Plane(this.transform.forward, selfPosition)).ClosestPointOnPlane(other.transform.position);
		// Debug.Log("--- " + other.transform.name + "     " + this.transform.name);
		if (other.transform.childCount > 0)
		    colliderSurfacePoint = other.transform.GetChild(0).position;
		else
		    colliderSurfacePoint = Vector3.zero;
		// if (Zone == Type.contact){
		//     Debug.Log(this.transform.name + " " +  other.transform.name);
		//     Debug.Log(GetComponentInParent<TransformLinker>().parent.name+ "--- " + GetComponentInParent<TransformLinker>().transform.name +" "+ colliderPosition.ToString("F5") + "-- " + selfPosition.ToString("F5") +"   "+contactPoint.ToString("F5"));
		//     Debug.Log("--- " + this.GetComponent<Collider>().bounds.center.ToString("F5")  + "  " + this.GetComponent<Collider>().bounds.size.ToString("F5") + "  " + this.GetComponent<Collider>().bounds.min.ToString("F5")  + "  " + this.GetComponent<Collider>().bounds.max.ToString("F5"));
		//     // 
		// }
	    }
	}

	void OnTriggerExit(Collider other)
	{
	    state = State.outside;
	}
    }
}
