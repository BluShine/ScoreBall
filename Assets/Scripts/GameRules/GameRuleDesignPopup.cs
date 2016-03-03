using UnityEngine;
using System.Collections.Generic;

public class GameRuleDesignPopup : MonoBehaviour {
	//only have one popup open at a time, close the active one if a new one is opened
	public static GameRuleDesignPopup activePopup = null;

	private RectTransform iconContainer;
	private bool iconContainerInitialized = false;

	private float arrowWidth;
	private Vector2 iconAreaBottomLeft;
	private Vector2 iconAreaTopRight;
	private Vector2 iconContainerSpacingSize;

	private List<GameObject> popupIcons = new List<GameObject>();

	public void Start() {
if (GameRuleDesigner.instance == null)
return;
		//store a bunch of size information for positioning things
		RectTransform arrow = (RectTransform)(transform.GetChild(0));
		arrowWidth = arrow.sizeDelta.x;
		RectTransform iconArea = (RectTransform)(transform.GetChild(1));
		iconAreaBottomLeft = iconArea.offsetMin;
		iconAreaTopRight = iconArea.offsetMax; //x and y are both negative for an inset corner
		iconContainer = (RectTransform)(transform.GetChild(2));
		//throw the container out to under the canvas
		iconContainer.SetParent(GameRuleDesigner.instance.uiCanvas);
		RectTransform iconContainerSpacing = (RectTransform)(iconContainer.GetChild(0));
		iconContainerSpacingSize = iconContainerSpacing.sizeDelta;
		//we only needed these to get their size, we can get rid of them now
		Destroy(iconArea.gameObject);
		Destroy(iconContainerSpacing.gameObject);
	}
	//GameRuleDesigner gave us a prefab, instantiate it and store the instantiated object
	//we will finish construction the first time this popup is shown
	//in the meantime, hide the icon
	public void addIcon(GameObject popupIcon) {
		popupIcon = (GameObject)(Instantiate(popupIcon));
		popupIcons.Add(popupIcon);
		popupIcon.SetActive(false);
	}
	public void TogglePopup() {
		bool opening;
		//this one is being closed
		if (activePopup == this) {
			opening = false;
			activePopup = null;
		//there's another one open, close it
		} else if (activePopup != null) {
			opening = true;
			activePopup.TogglePopup();
			activePopup = this;
		//we're opening since all popups are closed
		} else {
			opening = true;
			activePopup = this;
		}
		if (!iconContainerInitialized)
			initializeIconContainer();
		GameObject iconContainerGameObject = iconContainer.gameObject;
		iconContainerGameObject.SetActive(!iconContainerGameObject.activeSelf);
	}
	public void initializeIconContainer() {
if (popupIcons.Count == 0)
return;
		//finish rearranging the icons
		//because unity doesn't call Start when objects are instantiated, this actually happens after addIcon()
		//place them in a square or almost-square grid
		float maxWidth = 0.0f;
		float maxHeight = 0.0f;
		//find out what our grid dimensions are
		foreach (GameObject icon in popupIcons) {
			Vector2 iconSize = ((RectTransform)(icon.transform)).sizeDelta;
			maxWidth = Mathf.Max(maxWidth, iconSize.x);
			maxHeight = Mathf.Max(maxHeight, iconSize.y);
		}
		//now using this grid size, figure out how big we should display
		//we want to target a size that's about the same ratio as the screen size
		Vector2 screenSize = GameRuleDesigner.instance.uiCanvas.sizeDelta;
		float screenRatio = screenSize.x / screenSize.y;
		//use big fancy math to find how many rows+columns we should have
		//screenratio = screenw / screenh = totalwidth / totalheight
		//totalwidth = cols * width + spacingX * (cols-1) + boundsX = cols * (width + spacingX) - spacingX + boundsX
		//totalheight = rows * height + spacingY * (rows-1) + boundsY = rows * (height + spacingY) - spacingY + boundsY
		//rows = iconcount / cols
		//totalheight = (iconcount / cols) * (height + spacingY) - spacingY + boundsY
		//screenratio = (cols * (width + spacingX) - spacingX + boundsX) / ((iconcount / cols) * (height + spacingY) - spacingY + boundsY)
		//screenratio * ((iconcount / cols) * (height + spacingY) - spacingY + boundsY) = cols * (width + spacingX) - spacingX + boundsX
		//screenratio * (iconcount * (height + spacingY) + (-spacingY + boundsY) * cols) = cols^2 * (width + spacingX) + (-spacingX + boundsX) * cols
		//screenratio * iconcount * (height + spacingY) + screenratio * (-spacingY + boundsY) * cols = cols^2 * (width + spacingX) + (-spacingX + boundsX) * cols
		//0 = cols^2 * (width + spacingX) + (-spacingX + boundsX) * cols - screenratio * (-spacingY + boundsY) * cols - screenratio * iconcount * (height + spacingY)
		//0 = cols^2 * (width + spacingX) + (screenratio * (spacingY - boundsY) - spacingX + boundsX) * cols - screenratio * iconcount * (height + spacingY)
		//x = (-b + sqrt(b^2 - 4ac)) / (2a)
		int iconsCount = popupIcons.Count;
		Vector2 iconAreaBounds = iconAreaBottomLeft * 2.0f; //use only BottomLeft because TopRight gives extra space on the right
		float colsA = maxWidth + iconContainerSpacingSize.x;
		float colsB = screenRatio * (iconContainerSpacingSize.y - iconAreaBounds.y) - iconContainerSpacingSize.x + iconAreaBounds.x;
		float colsC = -screenRatio * iconsCount * (maxHeight + iconContainerSpacingSize.y);
		int cols = (int)((Mathf.Sqrt(colsB * colsB - 4.0f * colsA * colsC) - colsB) / (2.0 * colsA));
		int rows = (iconsCount + cols - 1) / cols;
		//resize the panel
		float containerWidth = cols * (maxWidth + iconContainerSpacingSize.x) - iconContainerSpacingSize.x + iconAreaBounds.x;
		float containerHeight = rows * (maxHeight + iconContainerSpacingSize.y) - iconContainerSpacingSize.y + iconAreaBounds.y;
		iconContainer.sizeDelta = new Vector2(containerWidth, containerHeight);

		//now attempt to position it
		//first things first, we need to scale it
		//we want to give it the scale of the icon container for the whole panel
		float designPanelScale = GameRuleDesigner.instance.ruleDesignPanel.GetChild(0).localScale.x;
		//but if it's too big for the screen, we need to use that scale into our position
		float scale = Mathf.Min(designPanelScale, Mathf.Min(screenSize.x / containerWidth, screenSize.y / containerHeight));
		iconContainer.localScale = new Vector3(scale, scale);
		//we have now scaled it
		//next, we want to get the center of this popup in canvas space so that we can try to center the icon container there
		Vector2 targetPosition = GameRuleDesigner.instance.uiCanvas.InverseTransformPoint(transform.position);
		//the popup pivot is in the left-center, so add half the width to make it center-center
		//we also need to factor in the design panel scale
		targetPosition.x += ((RectTransform)transform).sizeDelta.x * 0.5f * designPanelScale;
		//position the container relative to the center of the ui canvas
		Vector2 halfScreenSize = screenSize * 0.5f;
		float halfFinalWidth = containerWidth * scale * 0.5f;
		float halfFinalHeight = containerHeight * scale * 0.5f;
		float finalWidth = containerWidth * scale;
		float finalHeight = containerHeight * scale;
		iconContainer.localPosition = new Vector3(
			Mathf.Max(halfFinalWidth - halfScreenSize.x, Mathf.Min(targetPosition.x, halfScreenSize.x - halfFinalWidth)),
			Mathf.Max(halfFinalHeight - halfScreenSize.y, Mathf.Min(targetPosition.y, halfScreenSize.y - halfFinalHeight))
		);

		//we have finally resized and repositioned the icon container
		//now, layout all the icons
		float gridWidth = maxWidth + iconContainerSpacingSize.x;
		float gridHeight = maxHeight + iconContainerSpacingSize.y;
		float halfContainerWidth = containerWidth * 0.5f;
		float halfContainerHeight = containerHeight * 0.5f;
		for (int i = 0; i < iconsCount; i++) {
			RectTransform popupIcon = (RectTransform)(popupIcons[i].transform);
			popupIcon.gameObject.SetActive(true);
			popupIcon.SetParent(iconContainer);
			//freakin' unity gives us a default scale of 0
			popupIcon.localScale = new Vector3(1.0f, 1.0f);
			//set its position, taking into account that the container's pivot is center-center and the icon's is left-center
			popupIcon.localPosition = new Vector3(
				iconAreaBottomLeft.x - halfContainerWidth + (i % cols) * gridWidth,
				iconAreaBottomLeft.y - halfContainerHeight + (i / cols) * gridHeight + popupIcon.sizeDelta.y * 0.5f
			);
		}

		iconContainerInitialized = true;
	}
	public void PickIcon() {
		//get the spot in the container that was clicked
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(iconContainer, Input.mousePosition, null, out localPoint);
		localPoint.x += iconContainer.rect.width * 0.5f;
		localPoint.y = iconContainer.rect.height * 0.5f - localPoint.y;
Debug.Log("Picked icon at " + localPoint.x + ", " + localPoint.y);
		TogglePopup();
	}
	//we need to destroy the icon container too
	public void destroyPopup() {
		//if we never initialized the icon container, we need to destroy all the icons floating in the hierarchy
		if (!iconContainerInitialized) {
			foreach (GameObject popupIcon in popupIcons)
				Destroy(popupIcon);
		}
		Destroy(iconContainer.gameObject);
		Destroy(this.gameObject);
	}
}
