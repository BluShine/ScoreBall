using UnityEngine;
using System.Collections.Generic;

public class GameRuleDesignPopup : MonoBehaviour {
	//only have one popup open at a time, close the active one if a new one is opened
	private static GameRuleDesignPopup activePopup = null;

	private RectTransform iconContainer;
	private bool iconContainerInitialized = false;

	private Vector2 iconAreaBottomLeft;
	private Vector2 iconAreaTopRight;
	private Vector2 iconAreaTotalBounds;
	private Vector2 iconContainerSpacingSize;

	private int cols;
	private int rows;
	private Vector2 cellSize;
	private Vector2 cellSizeWithSpacing;

	private GameRuleDesignComponent componentRepresented;
	private List<GameRuleDesignComponentDescriptor> popupDescriptors;
	private List<RectTransform> popupIcons = new List<RectTransform>();

	//build the popup from the given list of descriptors
	//also receives the component that this popup will edit and the type of component used to find the descriptors
	public void buildPopup(GameRuleDesignComponent cr, List<GameRuleDesignComponentDescriptor> pd, System.Type describedType) {
		//instead of Start(), this is the place where stuff will get initialized
		//store a bunch of size information for positioning things
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
					if (describedType == typeof(GameRulePointsPlayerEffect))
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
					//a points effect shows up as "+?"
					if (otherType == typeof(GameRulePointsPlayerEffect))
						popupIcon = groupIcons(new List<GameObject>(new GameObject[] {
							GameRuleIconStorage.instance.charPlusIcon,
							GameRuleIconStorage.instance.charQmarkIcon
						}));
					else
						throw new System.Exception("Bug: no special popup icon cases for type " + otherType);
				} else
					throw new System.Exception("Bug: no special popup icon cases for " + descriptor);
			//it has an icon, it's probably normal
			} else
				popupIcon = componentRepresented.getIconMaybeOpponent(descriptor);
			popupIcon.SetParent(iconContainer);
			popupIcons.Add(popupIcon);
		}
		//we add a "cancel" icon at the end to ensure there's room to cancel out of the popup
		RectTransform cancelIcon = instantiateIcon(GameRuleDesigner.instance.cancelIconPopupPrefab);
		cancelIcon.SetParent(iconContainer);
		popupIcons.Add(cancelIcon);

		//if we're doing a points effect, default to the "+1" choice
		if (describedType == typeof(GameRulePointsPlayerEffect))
			cr.assignDescriptor(popupDescriptors[
				GameRulePointsPlayerEffect.POINTS_SERIALIZATION_MASK - GameRulePointsPlayerEffect.POINTS_SERIALIZATION_MAX_VALUE + 1]);
		//if we're doing a fixed duration, default to the rule generator min value
		else if (describedType == typeof(GameRuleActionFixedDuration))
			cr.assignDescriptor(popupDescriptors[GameRuleGenerator.ACTION_DURATION_SECONDS_SHORTEST]);
		else
			cr.assignDescriptor(popupDescriptors[0]);
	}
	//icons are gameobjects with an outer gameobject and an inner one with the image
	//we only care about the inner one, so only instantiate that
	//and give back the rect transform because that's usually what we want
	public static RectTransform instantiateIcon(GameObject icon) {
		return (RectTransform)(GameObject.Instantiate(icon.transform.GetChild(0).gameObject).transform);
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
			RectTransform iconTransform = instantiateIcon(icon);
			iconTransform.SetParent(holder);
			iconTransform.localPosition = new Vector3(totalWidth, 0.0f);
			Vector2 size = iconTransform.sizeDelta;
			totalWidth += size.x;
			maxHeight = Mathf.Max(maxHeight, size.y);
		}
		holder.sizeDelta = new Vector2(totalWidth, maxHeight);
		return holder;
	}
	//the descriptor doesn't have an icon prefab so for its popup display, clone the existing one that's in the icon container
	public RectTransform setPopupIcon(GameRuleDesignComponentDescriptor descriptor) {
		int i = popupDescriptors.IndexOf(descriptor);
		if (i == -1)
			throw new System.Exception("Bug: popup not expecting to clone icon for descriptor " + descriptor);

		RectTransform iconToPlace = (RectTransform)(GameObject.Instantiate(popupIcons[i].gameObject).transform);
		placeIcon(iconToPlace);
		iconToPlace.gameObject.SetActive(true);
		return iconToPlace;
	}
	//display the icon as the selected option for this popup
	public void placeIcon(RectTransform displayIcon) {
		RectTransform t = (RectTransform)transform;
		t.sizeDelta = displayIcon.sizeDelta + iconAreaTotalBounds;
		displayIcon.SetParent(transform);
		displayIcon.localPosition = new Vector3(iconAreaBottomLeft.x, 0.0f);
		displayIcon.localScale = new Vector3(1.0f, 1.0f);
	}
	public void TogglePopup() {
		//this one is being closed
		if (activePopup == this)
			activePopup = null;
		//this one is being opened
		else {
			//there's another one open, close it
			if (activePopup != null)
				activePopup.TogglePopup();
			activePopup = this;

			if (!iconContainerInitialized)
				initializeIconContainer();
			else
				resizeAndRepositionIconContainer();
		}
		GameObject iconContainerGameObject = iconContainer.gameObject;
		iconContainerGameObject.SetActive(!iconContainerGameObject.activeSelf);
	}
	//finish rearranging the icons
	//place them in a grid with a size ratio that roughly matches the screen
	//we need to wait until after the rule has been displayed so that the rule panel icon container has the right scale
	public void initializeIconContainer() {
		cellSize.x = 0.0f;
		cellSize.y = 0.0f;
		//find out what our grid dimensions are
		foreach (RectTransform icon in popupIcons) {
			Vector2 iconSize = icon.sizeDelta;
			cellSize.x = Mathf.Max(cellSize.x, iconSize.x);
			cellSize.y = Mathf.Max(cellSize.y, iconSize.y);
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
		float colsA = cellSize.x + iconContainerSpacingSize.x;
		float colsB = screenRatio * (iconContainerSpacingSize.y - iconAreaBounds.y) - iconContainerSpacingSize.x + iconAreaBounds.x;
		float colsC = -screenRatio * iconsCount * (cellSize.y + iconContainerSpacingSize.y);
		cols = Mathf.RoundToInt((Mathf.Sqrt(colsB * colsB - 4.0f * colsA * colsC) - colsB) / (2.0f * colsA));
		rows = (iconsCount + cols - 1) / cols;
		//resize the panel
		float containerWidth = cols * (cellSize.x + iconContainerSpacingSize.x) - iconContainerSpacingSize.x + iconAreaBounds.x;
		float containerHeight = rows * (cellSize.y + iconContainerSpacingSize.y) - iconContainerSpacingSize.y + iconAreaBounds.y;
		iconContainer.sizeDelta = new Vector2(containerWidth, containerHeight);

		//resize and reposition the icon container before placing the icons
		resizeAndRepositionIconContainer();

		//we have finally resized and repositioned the icon container
		//now, layout all the icons
		cellSizeWithSpacing = cellSize + iconContainerSpacingSize;
		float baseLeftOffset = iconAreaBottomLeft.x - containerWidth * 0.5f; //the left edge of the icon container
		float baseTopOffset = iconAreaTopRight.y + (containerHeight - cellSize.y) * 0.5f; //the center y of the top grid row
		for (int i = 0; i < iconsCount; i++) {
			RectTransform popupIcon = popupIcons[i];
			popupIcon.gameObject.SetActive(true);
			//freakin' unity gives us a default scale of 0
			popupIcon.localScale = new Vector3(1.0f, 1.0f);
			//set its position, taking into account that the container's pivot is center-center and the icon's is left-center
			popupIcon.localPosition = new Vector3(
				baseLeftOffset + (i % cols) * cellSizeWithSpacing.x + (cellSize.x - popupIcon.sizeDelta.x) * 0.5f,
				baseTopOffset - (i / cols) * cellSizeWithSpacing.y
			);
		}
		iconContainerInitialized = true;
	}
	public void resizeAndRepositionIconContainer() {
		Vector2 screenSize = GameRuleDesigner.instance.uiCanvas.sizeDelta;
		Vector2 containerSize = iconContainer.sizeDelta;
		//first things first, we need to scale it
		//we want to give it the scale of the icon container for the whole panel
		float designPanelScale = GameRuleDesigner.instance.ruleDesignPanel.GetChild(0).localScale.x;
		//but if it's too big for the screen, we need to use that scale into our position
		float scale = Mathf.Min(designPanelScale, Mathf.Min(screenSize.x / containerSize.x, screenSize.y / containerSize.y));
		iconContainer.localScale = new Vector3(scale, scale);
		//we have now scaled it
		//next, we want to get the center of this popup in canvas space so that we can try to center the icon container there
		Vector2 targetPosition = GameRuleDesigner.instance.uiCanvas.InverseTransformPoint(transform.position);
		//the popup pivot is in the left-center, so add half the width to make it center-center
		//we also need to factor in the design panel scale
		targetPosition.x += ((RectTransform)transform).sizeDelta.x * 0.5f * designPanelScale;
		//and now position the container relative to the center of the ui canvas
		Vector2 halfScreenSize = screenSize * 0.5f;
		float halfFinalWidth = containerSize.x * scale * 0.5f;
		float halfFinalHeight = containerSize.y * scale * 0.5f;
		iconContainer.localPosition = new Vector3(
			Mathf.Max(halfFinalWidth - halfScreenSize.x, Mathf.Min(targetPosition.x, halfScreenSize.x - halfFinalWidth)),
			Mathf.Max(halfFinalHeight - halfScreenSize.y, Mathf.Min(targetPosition.y, halfScreenSize.y - halfFinalHeight))
		);
	}
	public void PickIcon() {
		//get the spot in the container that was clicked
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(iconContainer, Input.mousePosition, null, out localPoint);
		//convert it from unity's coordinate space to top-left coordinate space
		localPoint.x += iconContainer.rect.width * 0.5f;
		localPoint.y = iconContainer.rect.height * 0.5f - localPoint.y;

		//find which descriptor we picked if we picked one
		int iconCol = Mathf.Max(0, Mathf.Min((int)((localPoint.x - iconAreaBottomLeft.x + iconContainerSpacingSize.x / 2) / cellSizeWithSpacing.x), cols - 1));
		int iconRow = Mathf.Max(0, Mathf.Min((int)((localPoint.y + iconAreaTopRight.y + iconContainerSpacingSize.y / 2) / cellSizeWithSpacing.y), rows - 1));
		int iconIndex = iconCol + iconRow * cols;
		//we did indeed pick a descriptor, assign it and the rest of thecode will refresh everything
		if (iconIndex < popupDescriptors.Count) {
			componentRepresented.assignDescriptor(popupDescriptors[iconIndex]);

			//update the panel with our new descriptor
			GameRuleDesigner.instance.redisplayRule();
		}

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
