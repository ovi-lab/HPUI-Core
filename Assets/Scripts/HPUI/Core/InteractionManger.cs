using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Threading;
using System.Linq;
using System.IO;
// using ViconDataStreamSDK.DotNET;

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
    
	private ButtonController[] buttons;
	private List<ButtonPair> values;
	protected List<ButtonController> btns = new List<ButtonController>();
	protected bool configurationComplete;
    
	protected ButtonController winningBtn;
	private float winningValue;
	private bool executeProcess = true;

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
	
	    getButtons();
	    // customSubjectHandler = FindObjectOfType<CustomSubjectScript>();

	    startAmend();        
	}

	public void getButtons()
	{
	    processGetButtonsFlag = true;
	}

        public void RegisterBtn(ButtonController btn)
        {
            if (processGetButtonsFlag)
                processGetButtonsPostProcessAction += () => _registerBtn(btn);
            else
                _registerBtn(btn);
        }
        
        public void _registerBtn(ButtonController btn)
        {
            if (btns.Contains(btn))
                return;
            btn.setValueCallback = setValue;
            btn.postContactCallback = postContactCallback;
            btns.Add(btn);
        }

        event System.Action processGetButtonsPostProcessAction;
        
	void processGetButtons()
	{
	    buttons = FindObjectsOfType<ButtonController>();

	    btns.Clear();
	    foreach (ButtonController btn in buttons)
	    {
		if (btn.transform.root.transform.name == "Right_hand_button_layout" || btn.transform.root.GetComponent<InteractableButtonsRoot>() != null)
		{
		    _registerBtn(btn);
		}
	    }
	    processGetButtonsFlag = false;
            
            if (processGetButtonsPostProcessAction != null)
            {    
                processGetButtonsPostProcessAction();
                processGetButtonsPostProcessAction = null;
            }
	    Debug.Log("Got heaps of buttons "  +  btns.Count);
	}

	protected virtual void startAmend()
	{
	}

	protected virtual void processData(string content)
	{}

	void setValue(float value, ButtonController btn)
	{
	    values.Add(new ButtonPair(btn, value));
	}

	void postContactCallback(ButtonController btn)
	{
	    if (contactEventTriggerd && processPostContactEventCallback(btn))
	    {
		contactEventTriggerd = false;
		btn.setSelectionHighlight(false);
	    }
	}

	void setButtonState(ButtonController btn, ButtonController.State state)
	{
	    if (btn.state != state)
	    {
		btn.state = state;
		btn.stateChanged = true;
	    }
	}

	void LateUpdate()
	{
	    if (processGetButtonsFlag)
		processGetButtons();
	    interactionPreProcess();
	    if (executeProcess)
	    {
		executeProcess = false;
		// bool did = false;
		preContactLoopCallback();
		try
		{
		    foreach (ButtonController btn in btns)
		    {
			// This is for cases when the button is being set active while this loop is running
			if (!btn.initialized)
			    continue;

			buttonCallback(btn);
			if (btn.stateChanged && btn.gameObject.activeInHierarchy)
			{
			    switch (btn.state)
			    {
				case ButtonController.State.proximate:
				    //btn.proximateAction.Invoke();
				    btn.invokeProximate();
				    break;
				case ButtonController.State.contact:
				    //btn.contactAction.Invoke();
				    processContactEventCallback(btn);
				    break;
				default:
				    //btn.defaultAction.Invoke();
				    btn.invokeDefault();
				    break;
			    }
			    btn.stateChanged = false;
			}
		    }
		    StartCoroutine(Timer());
		}
		// When the getButtons methos is called from somewhere cancell this round of checking
		catch (InvalidOperationException e)
		{}
	    }
	    values.Clear();
	}

	private void interactionPreProcess()
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
			setButtonState(winningBtn, entry.btn.failedState);
		    }
		    winningBtn = entry.btn;
		    winningValue = entry.value;
		}
		else
		{
		    setButtonState(entry.btn, entry.btn.failedState);
		}
	    }

	    if (winningBtn && winningBtn.contactDataValid())
	    {
		// Debug.Log("!!!!!!!  " + winningBtn.buttonParentName + winningBtn.fingerParentName + "  " + winningValue + "  " + btns.Contains(winningBtn));
		setButtonState(winningBtn, ButtonController.State.contact);
	    }
	}

	protected virtual void buttonCallback(ButtonController btn)
	{
	}

	protected virtual void preContactLoopCallback()
	{
	}

    
	protected virtual bool processPostContactEventCallback(ButtonController btn)
	{
	    return true;
	}
    
	protected virtual void processContactEventCallback(ButtonController btn)
	{
	    invokeContact(btn);
	}

	protected void invokeContact(ButtonController btn)
	{
	    btn.invokeContact();
	    contactEventTriggerd = true;
	}

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
