using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HPUI.Core
{
    [DefaultExecutionOrder(110)]
    public class ButtonController : MonoBehaviour
    {

	public delegate void SetValue(float value, ButtonController btn);
	public delegate void PostContactCallback(ButtonController btn);

	public bool initialized {get; private set;} = false;
    
	public ButtonZone proximalZone;
	public ButtonZone contactZone;

	[SerializeField]
	public RelativePosition relativePosition = RelativePosition.none;

	public ButtonControllerEvent proximateAction = new ButtonControllerEvent();
	public ButtonControllerEvent contactAction = new ButtonControllerEvent();
	public ButtonControllerEvent defaultAction = new ButtonControllerEvent();

	public SetValue setValueCallback { private get; set; }
	public PostContactCallback postContactCallback {private get; set;}

	[System.NonSerialized]
	public bool stateChanged = false;

	private State _state;
	public State previousState { get; private set; }

	// public string fingerParentName { get; private set; }
	// public string buttonParentName { get; private set; }

	// [System.NonSerialized]
	public int id = -1;

	[SerializeField]
	public State failedState = State.proximate;

	public State state
	{
	    get
	    {
		return _state;
	    }
	    set
	    {
		previousState = _state;
		_state = value;
		if (previousState == State.contact && postContactCallback != null )
		{
		    postContactCallback(this);
		}
	    }
	}

	ButtonColorBehaviour colbe;
	ButtonScaleBehaviour scabe;
	bool _isSelection = false;

	[System.NonSerialized]
	public SpriteRenderer button;
    
	public bool isSelectionBtn
	{
	    get{
		return _isSelection;
	    }
	    private set{
		_isSelection = value;
	    }
	}

	public enum State
	{
	    outside,
	    proximate,
	    contact
	}

	public enum RelativePosition
	{
	    onSkin,
	    offSkin,
	    none
	}

	public bool contactRecieved()
	{
	    return state == State.contact;
	}

	// Start is called before the first frame update
	void Start()
	{
	    state = State.outside;
	    scabe = GetComponent<ButtonScaleBehaviour>();
	    colbe = GetComponent<ButtonColorBehaviour>();
	    isSelectionBtn = false;
	    button = colbe.spriteRenderer;

	    if (!proximalZone.gameObject.activeSelf)
		proximalZone = null;
	
	    initialized = true;
	}

	public void resetStates()
	{
	    _state = State.outside;
	    previousState = State.outside;
	    contactZone.state = ButtonZone.State.outside;
	    if (proximalZone != null)
		proximalZone.state = ButtonZone.State.outside;
	}

	// Update is called once per frame
	// void Update()
	public void processUpdate()
	{
	    if (contactZone.state == ButtonZone.State.inside)
	    {
		if (state != State.contact)
		{
		    stateChanged = true;
		}
		setValueCallback((contactZone.colliderPosition - this.transform.position).magnitude, this);
	    }
	    else if (proximalZone != null && proximalZone.state == ButtonZone.State.inside)
	    {
		if (state != State.proximate)
		    stateChanged = true;
		state = State.proximate;
	    }
	    else
	    {
		if (state != State.outside)
		    stateChanged = true;
		state = State.outside;
	    }
	}

	public bool contactDataValid()
	{
	    var r = (contactZone.colliderSurfacePoint - contactZone.colliderPosition).magnitude;
	    var p = (contactZone.contactPlanePoint - contactZone.colliderPosition).magnitude;
	    var condition1 = (r * 0.99f) > p;
	    var colliderLocalPosition = contactZone.worldToLocalMatrix.MultiplyPoint(contactZone.colliderPosition);
	    bool condition2 = colliderLocalPosition.x <= 2.5f && colliderLocalPosition.y <= 2.5f && colliderLocalPosition.x >= -2.5f && colliderLocalPosition.y >= -2.5f;
	    // Debug.Log(condition1 && condition2);
	    // Debug.Log(r + " - " + p + " = " + (r-p) + "   %:" + (p/r) + "               " + buttonParentName + "_" + fingerParentName);
	    // Debug.Log(contactZone.worldToLocalMatrix.MultiplyPoint(contactZone.colliderPosition).ToString("F6"));
	    return condition1 && condition2;
	}

	public void replicateObject(string suffix)
	{
	    contactZone.replicateObject(suffix);
	}
    
	public void invokeProximate()
	{
	    proximateAction.Invoke(this);
	    colbe.resetColor();
	    // scabe.resetScale();
	}

	public void invokeDefault()
	{
	    defaultAction.Invoke(this);
	    colbe.resetColor();
	    // scabe.resetScale();
	}

	public void invokeContact()
	{
	    contactAction.Invoke(this);
	    colbe.invokeColorBehaviour();
	    // scabe.invokeScaleBehaviour();
	}

	public void setSelectionDefault(bool selection)
	{
	    isSelectionBtn = selection;
	    colbe.setSelectionDefault(selection);
	}

	public void setSelectionDefault(bool selection, Color color)
	{
	    isSelectionBtn = selection;
	    colbe.setSelectionDefault(selection, color);
	}

	public void setSelectionHighlight(bool selection)
	{
	    colbe.setSelectionHighlight(selection);
	}

        public void Hide()
        {
	    resetStates();
            transform.parent.gameObject.SetActive(false);
        }

        public void Show()
        {
            transform.parent.gameObject.SetActive(true);
        }
    }

    [Serializable]
    public class ButtonControllerEvent : UnityEvent<ButtonController> {}
}
