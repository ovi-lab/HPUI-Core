using UnityEngine;

namespace HPUI.Core
{
    [DefaultExecutionOrder(110)]
    public class ButtonController : MonoBehaviour
    {

	public delegate void SetValue(float value, ButtonController btn);
	public delegate void ButtonCallback(ButtonController btn);

	public bool initialized {get; private set;} = false;
    
	public ButtonZone proximalZone;
	public ButtonZone contactZone;

	public ButtonControllerEvent proximateAction = new ButtonControllerEvent();
	public ButtonControllerEvent contactAction = new ButtonControllerEvent();
	public ButtonControllerEvent defaultAction = new ButtonControllerEvent();

	public SetValue SetValueCallback { private get; set; }

	[System.NonSerialized]
	public bool stateChanged = false;

	private State _state;
	public State previousState { get; private set; }


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
		if (previousState == State.contact)
		{
		    PostContactCallback();
		}
	    }
	}

	ButtonColorBehaviour colbe;
	ButtonScaleBehaviour scabe;

	[System.NonSerialized]
	public SpriteRenderer button;
    
	public enum State
	{
	    outside,
	    proximate,
	    contact
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
	    button = colbe.spriteRenderer;

	    if (!proximalZone.gameObject.activeSelf)
		proximalZone = null;
	
	    initialized = true;
	}

	public void ResetStates()
	{
	    _state = State.outside;
	    previousState = State.outside;
	    contactZone.state = ButtonZone.State.outside;
	    if (proximalZone != null)
		proximalZone.state = ButtonZone.State.outside;
	}

        /// <summary>
        /// Based the zone that is triggered, set the state of this button. Calling this is deferred to the InteractionManger
        /// manager to avoid race conditions.
        /// </summary>
	public void ProcessUpdate()
	{
	    if (contactZone.state == ButtonZone.State.inside)
	    {
		if (state != State.contact)
		{
		    stateChanged = true;
		}
		SetValueCallback((contactZone.colliderPosition - this.transform.position).magnitude, this);
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

	public bool ContactDataValid()
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

        // This function is called when the state goes from contact to anything else
        private void PostContactCallback()
        {

        }

        // This method does not check if there was a state change.
        public void InvokeProximate()
	{
            proximateAction.Invoke(this);
            colbe.ResetColor();
        }

        // This method does not check if there was a state change.
	public void InvokeDefault()
	{
            defaultAction.Invoke(this);
            colbe.ResetColor();
        }

        // This method does not check if there was a state change.
	public void InvokeContact()
	{
            contactAction.Invoke(this);
            colbe.InvokeColorBehaviour();
        }

	// public void SetDefaultStyle()
	// {
	//     colbe.SetDefaultStyle();
	// }

	// public void SetContactStyle()
	// {
	//     colbe.setSelectionHighlight(selection);
	// }

        public void Hide()
        {
	    ResetStates();
            transform.parent.gameObject.SetActive(false);
        }

        public void Show()
        {
            transform.parent.gameObject.SetActive(true);
        }
    }
}
