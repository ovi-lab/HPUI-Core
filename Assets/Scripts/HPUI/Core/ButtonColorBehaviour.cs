using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core
{
    public class ButtonColorBehaviour : MonoBehaviour
    {
	public Color highlightColor;
	private Color secondaryHighlightColor;
	public Color sucessHighlightColor;
	public Color selectionColor;
	private Color defaultColor;
	private Color secondaryDefaultColor;
	public SpriteRenderer spriteRenderer;
	public bool externalSpriteRender {get; private set;}
	// Start is called before the first frame update
	void Start()
	{
	    if (!spriteRenderer)
	    {
		spriteRenderer = GetComponent<SpriteRenderer>();
		externalSpriteRender = false;
	    }
	    else
	    {
		externalSpriteRender = true;
	    }
	    defaultColor = spriteRenderer.color;
	    secondaryDefaultColor = spriteRenderer.color;
	    secondaryHighlightColor = highlightColor;
	}

	// Update is called once per frame
	public void invokeColorBehaviour()
	{
	    //Debug.Log("----------------------------------------------------------Color on " + GetComponentInParent<TransformLinker>().parent.name);
	    spriteRenderer.color = highlightColor;
	}

	public void resetColor()
	{
	    //Debug.Log("----------------------------------------------------------Color off " + GetComponentInParent<TransformLinker>().parent.name);
	    spriteRenderer.color = defaultColor;
	}

	public void setSelectionDefault(bool selection)
	{
	    // Debug.Log("-----------------  " + selectionColor + "  " + secondaryDefaultColor + " " + GetComponentInParent<TransformLinker>().parent.name);
	    if (selection)
	    {
		defaultColor = selectionColor;
	    }
	    else
	    {
		defaultColor = secondaryDefaultColor;
	    }
	}

	public void setSelectionDefault(bool selection, Color color)
	{
	    if (selection)
	    {
		defaultColor = color;
	    }
	    else
	    {
		defaultColor = secondaryDefaultColor;
	    }
	}

	public void setSelectionHighlight(bool selection)
	{
	    // Debug.Log("-----------------  " + selectionColor + "  " + secondaryDefaultColor + " " + GetComponentInParent<TransformLinker>().parent.name);
	    if (selection)
	    {
		highlightColor = sucessHighlightColor;
	    }
	    else
	    {
		highlightColor = secondaryHighlightColor;
	    }
	}
    }
}
