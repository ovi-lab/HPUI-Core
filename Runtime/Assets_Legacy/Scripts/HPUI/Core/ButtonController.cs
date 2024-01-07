using UnityEngine;

namespace ubco.ovilab.HPUI.Legacy
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

        // Handled/set by InteractionManger
	public SetValue SetValueCallback { private get; set; }

        // Handled/set by InteractionManger
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

	    if (proximalZone != null && !proximalZone.gameObject.activeSelf)
		proximalZone = null;
	
	    initialized = true;
	}

        /// <summary>
        /// Set the state of the buttons to it's default
        /// </summary>
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

        /// <summary>
        /// Used to validate the contact with the contact surface. Used by InteractionManager.
        /// </summary>
	public bool ContactDataValid()
	{
            // condition1: the distance from the collider's center to the point on the collider which touched the contact is smaller than
            //             the distance from the collider's center the contact point projected on the contact surface.
            //             it's a sanity check.
            // condition2: The collider's center projected on the contact surface is withing a 2.5 range of the contac surface's center.
            //             To ensure the contact is not triggered too far from the button itself.
            //             the 2.5f is a magic number?
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

        // Used for testing purposes
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
            if (proximalZone != null && proximalZone.state == ButtonZone.State.inside)
            {
                proximateAction.Invoke(this);
                colbe?.InvokeHoverColorBehaviour();
            }
        }

        // This method does not check if there was a state change.
	public void InvokeDefault()
	{
            defaultAction.Invoke(this);
            colbe?.ResetColor();
        }

        // This method does not check if there was a state change.
	public void InvokeContact()
	{
            contactAction.Invoke(this);
            colbe?.InvokeColorBehaviour();
        }

	// public void SetDefaultStyle()
	// {
	//     colbe.SetDefaultStyle();
	// }

	// public void SetContactStyle()
	// {
	//     colbe.setSelectionHighlight(selection);
	// }

        /// <summary>
        /// Hide the button element.
        /// </summary>
        public void Hide()
        {
	    ResetStates();
            transform.parent.gameObject.SetActive(false);
        }

        /// <summary>
        /// Display the button element.
        /// </summary>
        public void Show()
        {
            transform.parent.gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        private static GameObject dummyObject;

        public static void TriggerTargetButton(ButtonController targetButton)
        {
            if (dummyObject == null)
            {
                dummyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dummyObject.transform.localScale = Vector3.one * 0.02f;
                dummyObject.GetComponent<MeshRenderer>().enabled = false;
                dummyObject.AddComponent<ButtonTriggerCollider>();
                Rigidbody rb = dummyObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            if (targetButton == null)
            {
                return;
            }

            dummyObject.transform.position = targetButton.transform.position;
            targetButton.contactZone.TriggerBehaviour(dummyObject.GetComponent<Collider>());
            targetButton.contactAction.AddListener((btn) =>
            {
                dummyObject.transform.position = btn.transform.position - btn.transform.forward.normalized * 0.01f;
                btn.contactZone.state = ButtonZone.State.outside;
                if (btn.proximalZone != null)
                {
                    btn.proximalZone.state = ButtonZone.State.outside;
                }
            });
        }
#endif

    }
}
