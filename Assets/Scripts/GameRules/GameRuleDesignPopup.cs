using UnityEngine;
using System.Collections.Generic;

public class GameRuleDesignPopup : MonoBehaviour {
	//only have one popup open at a time, close the active one if a new one is opened
	private static GameRuleDesignPopup activePopup = null;

	private RectTransform iconContainer;
	private bool iconContainerInitialized = false;

	private float arrowWidth;
	private Vector2 iconAreaBottomLeft;
	private Vector2 iconAreaTopRight;
	private Vector2 iconAreaTotalBounds;
	private Vector2 iconContainerSpacingSize;

	private GameRuleDesignComponent componentRepresented;
	private List<GameRuleDesignComponentDescriptor> popupDescriptors;
	private List<RectTransform> popupIcons = new List<RectTransform>();

	//build the popup from the given list of descriptors
	//also receives the component that this popup will edit and the type of component used to find the descriptors
	public void buildPopup(GameRuleDesignComponent cr, List<GameRuleDesignComponentDescriptor> pd, System.Type t) {
if (GameRuleDesigner.instance == null)
return;
		//instead of Start(), this is the place where stuff will get initialized
		//store a bunch of size information for positioning things
		RectTransform arrow = (RectTransform)(transform.GetChild(0));
		arrowWidth = arrow.sizeDelta.x;
		RectTransform iconArea = (RectTransform)(transform.GetChild(1));
		iconAreaBottomLeft = iconArea.offsetMin;
		iconAreaTopRight = iconArea.offsetMax; //x and y are both negative for an inset corner
		iconAreaTotalBounds = iconAreaBottomLeft - iconAreaTopRight;
		iconContainer = (RectTransform)(transform.GetChild(2));
		//throw the container out to under the canvas so that it renders over the whole rule
		iconContainer.SetParent(GameRuleDesigner.instance.uiCanvas);
		RectTransform iconContainerSpacing = (RectTransform)(iconContainer.GetChild(0));
		iconContainerSpacingSize = iconContainerSpacing.sizeDelta;
		//we only needed these to get their size, we can get rid of them now
		Destroy(iconArea.gameObject);
		Destroy(iconContainerSpacing.gameObject);

		//now we can build the popup
		transform.SetParent(GameRuleDesigner.instance.iconContainerTransform);
		//setting the parent screwed up the scale so fix it
		transform.localScale = new Vector3(1.0f, 1.0f);
		componentRepresented = cr;
		popupDescriptors = pd;
		//go through all the descriptors and add their icon to this popup
		foreach (GameRuleDesignComponentDescriptor descriptor in popupDescriptors) {
			RectTransform popupIcon;
			//int descriptors don't have icon prefabs, so we have to construct the icons
			if (descriptor is GameRuleDesignComponentIntDescriptor) {
				List<GameObject> iconList = new List<GameObject>();
				int i = ((GameRuleDesignComponentIntDescriptor)descriptor).intDescribed;
				if (i >= 0) {
					if (t == typeof(GameRulePointsPlayerEffect))
						iconList.Add(GameRuleIconStorage.instance.charPlusIcon);
					GameRuleIconStorage.instance.addDigitIcons(i, iconList);
				} else {
					iconList.Add(GameRuleIconStorage.instance.charMinusIcon);
					GameRuleIconStorage.instance.addDigitIcons(-i, iconList);
				}
				//make a gameobject to hold all of the icons
				popupIcon = groupIcons(iconList);
			//almost all other descriptors we can construct normally
			//if it doesn't have an icon then it's a special case we know about
			} else if (descriptor.displayIcon == null) {
				if (descriptor is GameRuleDesignComponentClassDescriptor) {
					System.Type otherType = ((GameRuleDesignComponentClassDescriptor)descriptor).typeDescribed;
					//a points effect will just show up as +1
					if (otherType == typeof(GameRulePointsPlayerEffect))
						popupIcon = groupIcons(new List<GameObject>(new GameObject[] {
							GameRuleIconStorage.instance.charPlusIcon,
							GameRuleIconStorage.instance.charQmarkIcon
						}));
					else
						throw new System.Exception("Bug: no special popup icon cases for type " + otherType);
				} else
					throw new System.Exception("Bug: no special popup icon cases for " + descriptor);
			//it has an icon, it's normal
			} else
				popupIcon = (RectTransform)(GameObject.Instantiate(descriptor.displayIcon).transform);
			popupIcons.Add(popupIcon);
			popupIcon.gameObject.SetActive(false);
		}
		//if we're doing a points effect, default to the "+1" choice
		if (t == typeof(GameRulePointsPlayerEffect))
			cr.assignDescriptor(popupDescriptors[-GameRuleGenerator.POINTS_EFFECT_MIN_POINTS]);
		else
			cr.assignDescriptor(popupDescriptors[0]);
	}
	//given a bunch of prefabs, spawns an object to hold all of them in sequence
	public static RectTransform groupIcons(List<GameObject> iconList) {
		RectTransform holder = (new GameObject()).AddComponent<RectTransform>();
		holder.anchorMin = new Vector2(0, 0.5f);
		holder.anchorMax = new Vector2(0, 0.5f);
		holder.pivot = new Vector2(0, 0.5f);
		float totalWidth = 0.0f;
		float maxHeight = 0.0f;
		foreach (GameObject icon in iconList) {
			RectTransform iconTransform = (RectTransform)(GameObject.Instantiate(icon).transform);
			iconTransform.SetParent(holder);
			iconTransform.localPosition = new Vector3(totalWidth, 0.0f);
			Vector2 size = iconTransform.sizeDelta;
			totalWidth += size.x;
			maxHeight = Mathf.Max(maxHeight, size.y);
		}
		holder.sizeDelta = new Vector2(totalWidth, maxHeight);
		return holder;
	}
	//display the icon as the selected option for this popup
	public void placeIcon(RectTransform displayIcon) {
		RectTransform t = (RectTransform)transform;
		t.sizeDelta = displayIcon.sizeDelta + iconAreaTotalBounds;
		displayIcon.SetParent(transform);
		displayIcon.localPosition = new Vector3(iconAreaBottomLeft.x, 0.0f);
		displayIcon.localScale = new Vector3(1.0f, 1.0f);
	}
	//the descriptor doesn't have an icon prefab so for its popup display, clone the existing one that's in the icon container
	public RectTransform setPopupIcon(GameRuleDesignComponentDescriptor descriptor) {
Debug.Log("Setting popup icon for " + descriptor);
		int i = popupDescriptors.IndexOf(descriptor);
		if (i == -1)
			throw new System.Exception("Bug: popup not expecting to clone icon for descriptor " + descriptor);

		RectTransform iconToPlace = (RectTransform)(GameObject.Instantiate(popupIcons[i]).transform);
		placeIcon(iconToPlace);
		iconToPlace.gameObject.SetActive(true);
		return iconToPlace;
	}
	public void TogglePopup() {
//		bool opening;
		//this one is being closed
		if (activePopup == this) {
//			opening = false;
			activePopup = null;
		//there's another one open, close it
		} else if (activePopup != null) {
//			opening = true;
			activePopup.TogglePopup();
			activePopup = this;
		//we're opening since all popups are closed
		} else {
//			opening = true;
			activePopup = this;
		}
		if (!iconContainerInitialized)
			initializeIconContainer();
		GameObject iconContainerGameObject = iconContainer.gameObject;
		iconContainerGameObject.SetActive(!iconContainerGameObject.activeSelf);
	}
	//finish rearranging the icons
	//place them in a grid with a size ratio that roughly matches the screen
	//we need to wait until after the rule has been displayed so that the rule panel icon container has the right scale
	public void initializeIconContainer() {
if (popupIcons.Count == 0)
return;
		float maxWidth = 0.0f;
		float maxHeight = 0.0f;
		//find out what our grid dimensions are
		foreach (RectTransform icon in popupIcons) {
			Vector2 iconSize = icon.sizeDelta;
			maxWidth = Mathf.Max(maxWidth, iconSize.x);
			maxHeight = Mathf.Max(maxHeight, iconSize.y);
		}
		//now using this grid size, figure out how big we should display
		//we want to target a size that's about the same ratio as the screen size
		Vector2 screenSize = GameRuleDesigner.instance.uiCanvas.sizeDelta;
		float screenRatio = screenSize.x / screenSize.y;
		//use big fancy math to find how many rows+columns we should have
		//screenratio = screenw / screenh = containerwidth / containerheight
		//containerwidth = cols * width + spacingX * (cols-1) + boundsX = cols * (width + spacingX) - spacingX + boundsX
		//containerheight = rows * height + spacingY * (rows-1) + boundsY = rows * (height + spacingY) - spacingY + boundsY
		//rows = iconcount / cols
		//containerheight = (iconcount / cols) * (height + spacingY) - spacingY + boundsY
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
Debug.Log("Attempted to get " + ((Mathf.Sqrt(colsB * colsB - 4.0f * colsA * colsC) - colsB) / (2.0f * colsA)) + " cols");
		int cols = Mathf.RoundToInt((Mathf.Sqrt(colsB * colsB - 4.0f * colsA * colsC) - colsB) / (2.0f * colsA));
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
Debug.Log("Container scale set to " + scale);
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
		float baseLeftOffset = iconAreaBottomLeft.x - containerWidth * 0.5f; //the left edge of the icon container
		float baseTopOffset = iconAreaTopRight.y + (containerHeight - maxHeight) * 0.5f; //the center y of the top grid row
		for (int i = 0; i < iconsCount; i++) {
			RectTransform popupIcon = popupIcons[i];
			popupIcon.gameObject.SetActive(true);
			popupIcon.SetParent(iconContainer);
			//freakin' unity gives us a default scale of 0
			popupIcon.localScale = new Vector3(1.0f, 1.0f);
			//set its position, taking into account that the container's pivot is center-center and the icon's is left-center
			popupIcon.localPosition = new Vector3(
				baseLeftOffset + (i % cols) * gridWidth + (maxWidth - popupIcon.sizeDelta.x) * 0.5f,
				baseTopOffset - (i / cols) * gridHeight
			);
		}
	}
	public void PickIcon() {
		//get the spot in the container that was clicked
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(iconContainer, Input.mousePosition, null, out localPoint);
		//convert it from unity's coordinate space to top-left coordinate space
		localPoint.x += iconContainer.rect.width * 0.5f;
		localPoint.y = iconContainer.rect.height * 0.5f - localPoint.y;
Debug.Log("Picked icon at " + localPoint.x + ", " + localPoint.y);
		TogglePopup();
	}
	//we need to destroy the icon container too
	public void destroyPopup() {
		//if we never initialized the icon container, we need to destroy all the icons floating in the hierarchy
		if (!iconContainerInitialized) {
			foreach (RectTransform popupIcon in popupIcons)
				Destroy(popupIcon.gameObject);
		}
		Destroy(iconContainer.gameObject);
		Destroy(this.gameObject);
	}
}
