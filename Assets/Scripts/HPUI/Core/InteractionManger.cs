using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Events;

namespace ubc.ok.ovilab.HPUI.Core
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

        [SerializeField]
        [Tooltip("Event called before each button is processed during a step with the button being processed as an argument.")]
        ButtonControllerEvent ButtonPreProcessEvent = new ButtonControllerEvent();
        [SerializeField]
        [Tooltip("Event called before any button is processed during a step.")]
        UnityEvent ButtonsPreProcessEvent = new UnityEvent();
        [SerializeField]
        [Tooltip("Event called after GetButtons routine runs.")]
        ButtonControllersEvent PostGetButtonsEvent = new ButtonControllersEvent();

        private ButtonController[] buttons;
	private List<ButtonPair> buttonStateValues;
	protected List<ButtonController> btns = new List<ButtonController>();
	protected bool configurationComplete;

        //This is the button that would get the contact event trigger when mulitple buttons come into contact with the trigger
	protected ButtonController winningBtn; 
	private float winningValue;

	public SpriteRenderer feedback;
	public Color sensorTriggerColor = new Color(1, 0.3f, 0.016f, 1);
	public Color successEventColor = Color.yellow;
	private Color defaultColor;

	private bool processGetButtonsFlag = false;
	
	// Start is called before the first frame update
	protected virtual void Start()
	{
	    // Collecting all the button elements that need to be interacted with
	    // NOTE: Take care with the indirect case as it can collect those elements only meant for displaying.
	    configurationComplete = false;
            _instance = this;
	    buttonStateValues = new List<ButtonPair>();

	    if (feedback != null)
		defaultColor = feedback.color;
	
	    GetButtons();
	    // customSubjectHandler = FindObjectOfType<CustomSubjectScript>();
	}

        /// <summary>
        /// Collect all the ButtonController objects in the scene.
        /// </summary>
	public void GetButtons()
	{
            // The flag is used to defer the process of getting all the buttons.
	    processGetButtonsFlag = true;
	}

        /// <summary>
        /// Add a ButtonController to the list of tracked buttons and setup hooks.
        /// </summary>
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
            btns.Add(btn);
        }

        event System.Action ProcessGetButtonsPostProcessAction;

        /// <summary>
        /// Get all the buttons that are active in the scene.
        /// </summary>
	void ProcessGetButtons()
	{
	    buttons = FindObjectsOfType<ButtonController>();

	    btns.Clear();
	    foreach (ButtonController btn in buttons)
	    {
		if (btn.transform.root.GetComponent<InteractableButtonsRoot>() != null)
		{
		    RegisterBtnUnsafe(btn);
		}
	    }
	    processGetButtonsFlag = false;

            // Once all the found buttons are registered, register buttons that may have been added while the above was running.
            if (ProcessGetButtonsPostProcessAction != null)
            {    
                ProcessGetButtonsPostProcessAction();
                // Clearing out the missed calls so that they don't get called again
                ProcessGetButtonsPostProcessAction = null;
            }
            
            PostGetButtonsEvent.Invoke(btns);
            Debug.Log("Got heaps of buttons "  +  btns.Count);
	}

        /// <summary>
        /// Used by ButtonController to set the distance (value) to the trigger.
        /// This will be used to resolve race conditions in InteractionPreProcess.
        /// </summary>
	void SetValue(float value, ButtonController btn)
	{
	    buttonStateValues.Add(new ButtonPair(btn, value));
	}

	void SetButtonState(ButtonController btn, ButtonController.State state)
	{
	    if (btn.state != state)
	    {
		btn.state = state;
		btn.stateChanged = true;
	    }
	}

        // Unity method
	protected virtual void LateUpdate()
	{
            // The GetButtons routine needs to run in case there is an update to the scene
	    if (processGetButtonsFlag)
		ProcessGetButtons();

            // This method is called outside the executeProcess condition so that the actual intractions will get updated in game time.
            // The executeProcess was added (maybe) to handle race conditions?
            InteractionPreProcess();
            // FIXME: Make sure this is not an issue
	    if (true)
	    {
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
	    buttonStateValues.Clear();
	}

        /// <summary>
        /// This method processes multiple ButtonController's being triggered. It first runs the processUpdate method for each registered ButtonController
        /// then adjusts the results if there are multiple buttons that report contact.
        /// </summary>
	private void InteractionPreProcess()
	{
	    foreach(var btn in btns)
	    {
		btn.ProcessUpdate();
	    }
	    winningBtn = null;
	    winningValue = 1000;
            foreach (ButtonPair entry in buttonStateValues.GroupBy(x => x.btn,
                                                                   x => x.value,
                                                                   (key, values) =>
                                                                   new ButtonPair(key, values.Min())))
	    {
		if (entry.value < winningValue)
		{
		    if (winningBtn != null && winningBtn != entry.btn)
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

	    if (winningBtn && winningBtn.ContactDataValid())
	    {
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
	}

        // NOTE: If I remember correctly this was added to avoid race conditions?
        // FIXME: Revisit this
	IEnumerator Timer()
	{
	    yield return new WaitForSeconds(0.05f);
	}

        // To save data and use when resolving race conditions.
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
    }
}
