using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core
{
    public class ButtonColorBehaviour : MonoBehaviour
    {
	public Color highlightColor;
        public Color hoverColor;
	// private Color secondaryHighlightColor;
	// public Color sucessHighlightColor;
	// public Color selectionColor;
	private Color defaultColor;
        public Color DefaultColor {
            get
            {
                return defaultColor;
            }
            set
            {
                defaultColor = value;
                ResetColor();
            }
        }
	//private Color secondaryDefaultColor;
	public Renderer buttonRenderer;
	public bool externalRender {get; private set;}

        public string colorPropertyName = "_Color";
        // Start is called before the first frame update
        void Start()
	{
	    if (!buttonRenderer)
	    {
		buttonRenderer = GetComponent<Renderer>();
		externalRender = false;
	    }
	    else
	    {
		externalRender = true;
	    }
	    defaultColor = buttonRenderer.material.GetColor(colorPropertyName);
	    //secondaryDefaultColor = spriteRenderer.color;
	    //secondaryHighlightColor = highlightColor;
	}

	// Update is called once per frame
	public void InvokeColorBehaviour()
	{
	    //Debug.Log("----------------------------------------------------------Color on " + GetComponentInParent<TransformLinker>().parent.name);
	    buttonRenderer.material.SetColor(colorPropertyName, highlightColor);
	}

        public void InvokeHoverColorBehaviour()
        {
            buttonRenderer.material.SetColor(colorPropertyName, hoverColor);
        }

	public void ResetColor()
	{
	    //Debug.Log("----------------------------------------------------------Color off " + GetComponentInParent<TransformLinker>().parent.name);
	    buttonRenderer.material.SetColor(colorPropertyName, defaultColor);
	}
    }
}
