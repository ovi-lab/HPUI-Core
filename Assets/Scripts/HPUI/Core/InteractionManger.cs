using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

namespace HPUI.Core
{
    [DefaultExecutionOrder(120)]
    public class InteractionManger : MonoBehaviour
    {
	static InteractionManger _instance;
	public static InteractionManger instance
	{
	    get
	    {
		if (_instance == null)
		    _instance = FindObjectOfType<InteractionManger>();
		return _instance;
	    }
	    private set {}
	}

        bool ProcessPostContactEventCallback { get; set; } = true;

        [SerializeField]
        [Tooltip("Event called before each button is processed during a step with the button being processed as an argument.")]
        ButtonControllerEvent ButtonPreProcessEvent = new ButtonControllerEvent();
        [SerializeField]
        [Tooltip("Event called before any button is processed during a step.")]
        UnityEvent ButtonsPreProcessEvent = new UnityEvent();

        private ButtonController[] buttons;
	private List<ButtonPair> values;
	protected List<ButtonController> btns = new List<ButtonController>();
	protected bool configurationComplete;

        //This is the button that would get the contact event trigger when mulitple buttons come into contact with the trigger
	protected ButtonController winningBtn; 
	private float winningValue;

	private bool executeProcess = true; // NOTE: See the comments on the `Timer` method

	public SpriteRenderer feedback;
	public Color sensorTriggerColor = new Color(1, 0.3f, 0.016f, 1);
	public Color successEventColor = Color.yellow;
	private Color defaultColor;
	private bool contactEventTriggerd = false;

	private bool processGetButtonsFlag = false;
	
	// Start is called before the first frame update
	void Start()
	{
	    // Collecting all the button elements that need to be interacted with
	    // NOTE: Take care with the indirect case as it can collect those elements only meant for displaying.
	    configurationComplete = false;
	    values = new List<ButtonPair>();

	    if (feedback != null)
		defaultColor = feedback.color;
	
	    GetButtons();
	    // customSubjectHandler = FindObjectOfType<CustomSubjectScript>();
	}

	public void GetButtons()
	{
	    processGetButtonsFlag = true;
	}

        public void RegisterBtn(ButtonController btn)
        {
            // making sure RegisterBtnUnsafe method gets called for each button.
            if (processGetButtonsFlag)
                ProcessGetButtonsPostProcessAction += () => RegisterBtnUnsafe(btn);
            else
                RegisterBtnUnsafe(btn);
        }

        // NOTE: Probably safe now, leaving as is, incase.
        // Unsafe because buttons might (?) get added after GetButtons() is called but before ProcessGenerateBtns finishes.
        private void RegisterBtnUnsafe(ButtonController btn)
        {
            if (btns.Contains(btn))
                return;
            btn.SetValueCallback = SetValue;
            btn.PostContactCallback = PostContactCallback;
            btns.Add(btn);
        }

        event System.Action ProcessGetButtonsPostProcessAction;
        
	void ProcessGetButtons()
	{
	    buttons = FindObjectsOfType<ButtonController>();

	    btns.Clear();
	    foreach (ButtonController btn in buttons)
	    {
		if (btn.transform.root.transform.name == "Right_hand_button_layout" || btn.transform.root.GetComponent<InteractableButtonsRoot>() != null)
		{
		    RegisterBtnUnsafe(btn);
		}
	    }
	    processGetButtonsFlag = false;
            
            if (ProcessGetButtonsPostProcessAction != null)
            {    
                ProcessGetButtonsPostProcessAction();
                // Clearing out the missed calls so that they don't get called again
                ProcessGetButtonsPostProcessAction = null;
            }
	    Debug.Log("Got heaps of buttons "  +  btns.Count);
	}

	void SetValue(float value, ButtonController btn)
	{
	    values.Add(new ButtonPair(btn, value));
	}

	void PostContactCallback(ButtonController btn)
	{
	    if (contactEventTriggerd && ProcessPostContactEventCallback)
	    {
		contactEventTriggerd = false;
		btn.SetSelectionHighlight(false);
	    }
	}

	void SetButtonState(ButtonController btn, ButtonController.State state)
	{
	    if (btn.state != state)
	    {
		btn.state = state;
		btn.stateChanged = true;
	    }
	}

	void LateUpdate()
	{
            // The Get Buttons routine needs to run in case there is an update to the scene
	    if (processGetButtonsFlag)
		ProcessGetButtons();
	    InteractionPreProcess();
	    if (executeProcess)
	    {
		executeProcess = false;
                // bool did = false;
                ButtonsPreProcessEvent.Invoke();
                try
		{
		    foreach (ButtonController btn in btns)
		    {
			// This is for cases when the button is being set active while this loop is running
			if (!btn.initialized)
			    continue;

                        ButtonPreProcessEvent.Invoke(btn);
                        if (btn.stateChanged && btn.gameObject.activeInHierarchy)
			{
			    switch (btn.state)
			    {
				case ButtonController.State.proximate:
				    //btn.proximateAction.Invoke();
				    btn.InvokeProximate();
				    break;
				case ButtonController.State.contact:
				    //btn.contactAction.Invoke();
				    ProcessContactEventCallback(btn);
				    break;
				default:
				    //btn.defaultAction.Invoke();
				    btn.InvokeDefault();
				    break;
			    }
			    btn.stateChanged = false;
			}
		    }
		    StartCoroutine(Timer());
		}
		// When the getButtons methos is called from somewhere cancell this round of checking
		catch (InvalidOperationException)
		{}
	    }
	    values.Clear();
	}

	private void InteractionPreProcess()
	{
	    foreach(var btn in btns)
	    {
		btn.processUpdate();
	    }
	    winningBtn = null;
	    winningValue = 1000;
	    foreach (ButtonPair entry in values)
	    {
		if (entry.value < winningValue)
		{
		    if (winningBtn != null)
		    {
			SetButtonState(winningBtn, entry.btn.failedState);
		    }
		    winningBtn = entry.btn;
		    winningValue = entry.value;
		}
		else
		{
		    SetButtonState(entry.btn, entry.btn.failedState);
		}
	    }

	    if (winningBtn && winningBtn.contactDataValid())
	    {
		// Debug.Log("!!!!!!!  " + winningBtn.buttonParentName + winningBtn.fingerParentName + "  " + winningValue + "  " + btns.Contains(winningBtn));
		SetButtonState(winningBtn, ButtonController.State.contact);
	    }
	}

        protected virtual void ProcessContactEventCallback(ButtonController btn)
	{
	    InvokeContact(btn);
	}

	protected void InvokeContact(ButtonController btn)
	{
	    btn.InvokeContact();
	    contactEventTriggerd = true;
	}

        // NOTE: If I remember correctly this was added to avoid race conditions?
        // FIXME: Revisit this
	IEnumerator Timer()
	{
	    yield return new WaitForSeconds(0.1f);
	    executeProcess = true;
	}

	private class ButtonPair
	{
	    public ButtonController btn { get; set; }
	    public float value { get; set; }

	    public ButtonPair(ButtonController btn, float value)
	    {
		this.btn = btn;
		this.value = value;
	    }
	}
    
	void OnDestroy()
	{
	}
    }
}
