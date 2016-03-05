using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//these are empty classes that exist solely for the purpose of producing a System.Type
class RuleStubZoneType {}
class RuleStubEventSource {}
class RuleStubEventTarget {}

public class GameRuleDesigner : MonoBehaviour {
	public static GameRuleDesigner instance;

	private static bool finishedInstantiating = false;

	public GameRuleDesignComponentClassDescriptor gameRuleEffectActionDescriptor;
	public GameRuleDesignComponentClassDescriptor gameRuleMetaRuleActionDescriptor;
	public GameRuleDesignComponentClassDescriptor gameRulePlayerSwapMetaRuleDescriptor;
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> componentSelectionMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();
	public GameRuleDesignComponent gameRuleConditionComponent;
	public GameRuleDesignComponent gameRuleActionComponent;
	public GameRuleDesignComponent gameRuleComponent;

	public RectTransform uiCanvas;
	public RectTransform ruleDesignPanel;
	[HideInInspector]
	public RectTransform iconContainerTransform;
	float iconDisplayMaxHeight;
	float iconDisplayMaxWidth;
	Vector2 iconDisplaySizeDelta;
	public GameObject ruleDesignPopupPrefab;
	public GameObject dataStoragePrefab; //this is shared across scenes, so keep it as just a prefab

	public void Start() {
		iconContainerTransform = (RectTransform)(ruleDesignPanel.GetChild(0));
		iconDisplayMaxHeight = iconContainerTransform.rect.height;
		iconDisplaySizeDelta = iconContainerTransform.sizeDelta;
		iconDisplayMaxWidth = uiCanvas.rect.width + iconDisplaySizeDelta.x;
		Destroy(iconContainerTransform.GetComponent<Image>());
		Instantiate(dataStoragePrefab);

		instance = this;
	}
	public void Update() {
		//our instance can't do anything until the other instances are ready
		if (!finishedInstantiating && GameRuleIconStorage.instance != null && GameRuleSpawnableObjectRegistry.instance != null) {
			//build the descriptors for selectable components before building the selection map
			//classes with subcomponents will also produce popups
			//conditions
			GameRuleDesignComponentClassDescriptor gameRuleEventHappenedConditionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleEventHappenedCondition),
				GameRuleIconStorage.instance.genericEventIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleEventType)
				});
			GameRuleDesignComponentClassDescriptor gameRuleZoneConditionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleZoneCondition),
				GameRuleIconStorage.instance.genericZoneIcon, 1, true,
				new System.Type[] {
					typeof(GameRuleSourceSelector),
					typeof(RuleStubZoneType)
				});
			//actions get specially handled since their class is controlled by the condition
			//they don't have icons because they never appear in popups and are fully represented by the icons in their subcomponents
			gameRuleEffectActionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleEffectAction),
				null, -1, false,
				new System.Type[] {
					typeof(GameRuleSelector),
					typeof(GameRuleEffect)
				});
			//metarules don't even have any popup, they are 100% 1:1 with conditions
			gameRuleMetaRuleActionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleMetaRuleAction),
				null, -1, false,
				new System.Type[] {
					typeof(GameRuleMetaRule)
				});
			//selectors
			GameRuleDesignComponentClassDescriptor gameRulePlayerSelectorDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRulePlayerSelector),
				GameRuleIconStorage.instance.playerIcon, 0, true, null);
			GameRuleDesignComponentClassDescriptor gameRuleOpponentSelectorDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleOpponentSelector),
				GameRuleIconStorage.instance.opponentIcon, 0, true, null);
			GameRuleDesignComponentClassDescriptor gameRuleBallShooterSelectorDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleBallShooterSelector),
				GameRuleIconStorage.instance.playerIcon, 0, true, null);
			GameRuleDesignComponentClassDescriptor gameRuleBallShooterOpponentSelectorDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleBallShooterOpponentSelector),
				GameRuleIconStorage.instance.opponentIcon, 0, true, null);
			GameRuleDesignComponentClassDescriptor gameRuleBallSelectorDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleBallSelector),
				GameRuleIconStorage.instance.genericBallIcon, 0, true, null);
			//zone types
			GameRuleDesignComponentZoneTypeDescriptor boomerangZoneDescriptor = new GameRuleDesignComponentZoneTypeDescriptor(
				GameRuleRequiredObjectType.BoomerangZone,
				GameRuleIconStorage.instance.boomerangZoneIcon);
			//effects
			GameRuleDesignComponentClassDescriptor gameRulePointsPlayerEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRulePointsPlayerEffect),
				//this descriptor doesn't have an icon but components for it will make one
				null, 0, false,
				new System.Type[] {
					//we'll just use this as the key for our list of possible point values
					typeof(GameRulePointsPlayerEffect)
				});
			GameRuleDesignComponentClassDescriptor gameRuleDuplicateEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleDuplicateEffect),
				GameRuleIconStorage.instance.duplicatedIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			GameRuleDesignComponentClassDescriptor gameRuleFreezeEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleFreezeEffect),
				GameRuleIconStorage.instance.frozenIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			GameRuleDesignComponentClassDescriptor gameRuleDizzyEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleDizzyEffect),
				GameRuleIconStorage.instance.dizzyIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			GameRuleDesignComponentClassDescriptor gameRuleBounceEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleBounceEffect),
				GameRuleIconStorage.instance.bouncyIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			//metarules are controlled by the zones
			gameRulePlayerSwapMetaRuleDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRulePlayerSwapMetaRule),
				//this descriptor doesn't have an icon but components for it will make one
				null, 0, false, null);
			//action durations
			GameRuleDesignComponentClassDescriptor gameRuleActionFixedDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionFixedDuration),
				GameRuleIconStorage.instance.clockIcon, 0, true,
				new System.Type[] {
					//we'll just use this as the key for our list of possible second durations
					typeof(GameRuleActionFixedDuration)
				});
			GameRuleDesignComponentClassDescriptor gameRuleActionUntilConditionDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionUntilConditionDuration),
				GameRuleIconStorage.instance.genericEventIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleEventHappenedCondition)
				});
			//event sources and targets
			GameRuleDesignComponentClassDescriptor teamPlayerDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(TeamPlayer),
				GameRuleIconStorage.instance.playerIcon, 0, true, null);
			GameRuleDesignComponentClassDescriptor ballDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(Ball),
				GameRuleIconStorage.instance.genericBallIcon, 0, true, null);

			//now that we have our descriptors, we can build our selection map
			//only some classes make easy popups, the others will need special handling
			List<GameRuleDesignComponentDescriptor> gameRuleConditionSelection = (componentSelectionMap[typeof(GameRuleCondition)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleConditionSelection.Add(gameRuleEventHappenedConditionDescriptor);
			gameRuleConditionSelection.Add(gameRuleZoneConditionDescriptor);
			List<GameRuleDesignComponentDescriptor> gameRuleSelectorSelection = (componentSelectionMap[typeof(GameRuleSelector)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleSelectorSelection.Add(gameRulePlayerSelectorDescriptor);
			gameRuleSelectorSelection.Add(gameRuleOpponentSelectorDescriptor);
			gameRuleSelectorSelection.Add(gameRuleBallShooterSelectorDescriptor);
			gameRuleSelectorSelection.Add(gameRuleBallShooterOpponentSelectorDescriptor);
			gameRuleSelectorSelection.Add(gameRuleBallSelectorDescriptor);
			//event types for GameRuleEventHappenedConditions
			List<GameRuleDesignComponentDescriptor> gameRuleEventTypeSelection = (componentSelectionMap[typeof(GameRuleEventType)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (GameRuleEventType eventType in GameRuleEvent.eventTypesList) {
				gameRuleEventTypeSelection.Add(new GameRuleDesignComponentEventTypeDescriptor(eventType));
			}
			List<GameRuleDesignComponentDescriptor> gameRuleSourceSelectorSelection = (componentSelectionMap[typeof(GameRuleSourceSelector)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleSourceSelectorSelection.Add(gameRulePlayerSelectorDescriptor);
			gameRuleSourceSelectorSelection.Add(gameRuleBallSelectorDescriptor);
			List<GameRuleDesignComponentDescriptor> zoneTypeSelection = (componentSelectionMap[typeof(RuleStubZoneType)] = new List<GameRuleDesignComponentDescriptor>());
			zoneTypeSelection.Add(boomerangZoneDescriptor);
			List<GameRuleDesignComponentDescriptor> gameRuleEffectSelection = (componentSelectionMap[typeof(GameRuleEffect)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleEffectSelection.Add(gameRulePointsPlayerEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleDuplicateEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleFreezeEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleDizzyEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleBounceEffectDescriptor);
			List<GameRuleDesignComponentDescriptor> pointAmountSelection = (componentSelectionMap[typeof(GameRulePointsPlayerEffect)] = new List<GameRuleDesignComponentDescriptor>());
			for (int i = GameRuleGenerator.POINTS_EFFECT_MIN_POINTS; i <= GameRuleGenerator.POINTS_EFFECT_MAX_POINTS; i++) {
				if (i == 0)
					i++;
				pointAmountSelection.Add(new GameRuleDesignComponentIntDescriptor(i));
			}
			List<GameRuleDesignComponentDescriptor> gameRuleActionDurationSelection = (componentSelectionMap[typeof(GameRuleActionDuration)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleActionDurationSelection.Add(gameRuleActionFixedDurationDescriptor);
			gameRuleActionDurationSelection.Add(gameRuleActionUntilConditionDurationDescriptor);
			List<GameRuleDesignComponentDescriptor> fixedDurationSelection = (componentSelectionMap[typeof(GameRuleActionFixedDuration)] = new List<GameRuleDesignComponentDescriptor>());
			for (int i = GameRuleGenerator.ACTION_DURATION_SECONDS_SHORTEST; i <= GameRuleGenerator.ACTION_DURATION_SECONDS_LONGEST; i++) {
				fixedDurationSelection.Add(new GameRuleDesignComponentIntDescriptor(i));
			}
			List<GameRuleDesignComponentDescriptor> eventSourceSelection = (componentSelectionMap[typeof(RuleStubEventSource)] = new List<GameRuleDesignComponentDescriptor>());
			eventSourceSelection.Add(teamPlayerDescriptor);
			eventSourceSelection.Add(ballDescriptor);
			List<GameRuleDesignComponentDescriptor> eventTargetSelection = (componentSelectionMap[typeof(RuleStubEventTarget)] = new List<GameRuleDesignComponentDescriptor>());
			eventTargetSelection.Add(teamPlayerDescriptor);
			eventTargetSelection.Add(ballDescriptor);
			//field object types
			//use the registry to build this
			foreach (GameRuleSpawnableObject spawnableObject in GameRuleSpawnableObjectRegistry.instance.goalSpawnableObjects) {
				eventTargetSelection.Add(new GameRuleDesignComponentFieldObjectDescriptor(
					spawnableObject.spawnedObject.GetComponent<FieldObject>().sportName,
					spawnableObject.icon));
			}
			eventTargetSelection.Add(new GameRuleDesignComponentFieldObjectDescriptor(
				"boundary",
				GameRuleIconStorage.instance.boundaryIcon));

			//and finally we'll build our complete rule that the user can change
			gameRuleConditionComponent = new GameRuleDesignComponent(typeof(GameRuleCondition));
			gameRuleActionComponent = new GameRuleDesignComponent(gameRuleConditionComponent);
			gameRuleComponent = new GameRuleDesignComponent(
				new GameRuleDesignComponentClassDescriptor(
					typeof(GameRule),
					GameRuleIconStorage.instance.resultsInIcon, 1, true, null));
			gameRuleComponent.subComponents = new GameRuleDesignComponent[] {gameRuleConditionComponent, gameRuleActionComponent};
			redisplayRule();

			finishedInstantiating = true;
		}
	}
	public void redisplayRule() {
		//start by relocating the icons appropriately
		Vector2 displaySize = gameRuleComponent.getDisplaySize();
Debug.Log("Whole rule display size " + displaySize.x + ", " + displaySize.y);
		//now we need to resize the container around the icons and the panel so that they're the right final size
		//find the biggest scale such that the icons fit both the max width and max height
		float scale = Mathf.Min(iconDisplayMaxWidth / displaySize.x, iconDisplayMaxHeight / displaySize.y);
		//resize the display panel based on how big the icons will appear
		Vector2 designPanelSize = new Vector2(displaySize.x * scale - iconDisplaySizeDelta.x, displaySize.y * scale - iconDisplaySizeDelta.y);
		ruleDesignPanel.sizeDelta = designPanelSize;
		//resize the icon container by setting the size delta
		iconContainerTransform.sizeDelta = new Vector2(displaySize.x * (1.0f - scale) + iconDisplaySizeDelta.x, displaySize.y * (1.0f - scale) + iconDisplaySizeDelta.y);
		//now scale it
		iconContainerTransform.localScale = new Vector3(scale, scale);
Debug.Log("Icon container scaled to " + scale);
		//and finally relocate the icons
		gameRuleComponent.relocateIcons(0.0f);
	}
//	public static GameRule buildRule(GameRuleDesignComponent ????????????) {

//	}
//	public static GameRuleCondition buildCondition(GameRuleDesignComponent ????????????) {

//	}
	public void createRule() {
Debug.Log("Create rule");
	}
}

public abstract class GameRuleDesignComponentDescriptor {
	public GameObject displayIcon;
	public int displayIconIndex; //-1 means no image will show up
	public bool showIcon; //if false, the popup for this component will be small and without an icon
	public System.Type[] subComponents; //this is used to retrieve from componentSelectionMap
	public GameRuleDesignComponentDescriptor(GameObject di, int dii, bool si, System.Type[] sc) {
		displayIcon = di;
		displayIconIndex = dii;
		showIcon = si;
		subComponents = sc;
	}
}
public class GameRuleDesignComponentClassDescriptor : GameRuleDesignComponentDescriptor {
	public System.Type typeDescribed;
	public GameRuleDesignComponentClassDescriptor(System.Type td, GameObject di, int dii, bool si, System.Type[] sc):
		base(di, dii, si, sc) {
		typeDescribed = td;
	}
}
public class GameRuleDesignComponentEventTypeDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleEventType eventTypeDescribed;
	public GameRuleDesignComponentEventTypeDescriptor(GameRuleEventType etd):
		base(GameRuleEvent.getEventIcon(etd), 1, true, new System.Type[] {
			typeof(RuleStubEventSource),
			typeof(RuleStubEventTarget)
		}) {
		eventTypeDescribed = etd;
	}
}
public class GameRuleDesignComponentZoneTypeDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleRequiredObjectType zoneTypeDescribed;
	public GameRuleDesignComponentZoneTypeDescriptor(GameRuleRequiredObjectType ztd, GameObject di):
		base(di, 0, true, null) {
		zoneTypeDescribed = ztd;
	}
}
public class GameRuleDesignComponentFieldObjectDescriptor : GameRuleDesignComponentDescriptor {
	public string fieldObjectDescribed;
	public GameRuleDesignComponentFieldObjectDescriptor(string fod, GameObject di) :
		base(di, 0, true, null) {
		fieldObjectDescribed = fod;
	}
}
public class GameRuleDesignComponentIntDescriptor : GameRuleDesignComponentDescriptor {
	public int intDescribed;
	public GameRuleDesignComponentIntDescriptor(int id) :
		base(null, 0, true, null) {
		intDescribed = id;
	}
}
public class GameRuleDesignComponent {
	public GameRuleDesignComponentDescriptor descriptor = null;
	public RectTransform componentIcon = null; //the icon that represents this component
	public GameRuleDesignPopup componentPopup = null; //a popup of different descriptors that this component can be
	public RectTransform componentDisplayed;
	public GameRuleDesignComponent[] subComponents = null;
	public GameRuleDesignComponent(GameRuleDesignComponentDescriptor d) {
		assignDescriptor(d);
	}
	//construct from a list of descriptors
	//the variables for this component will change to reflect which component it represents
	public GameRuleDesignComponent(System.Type t) {
		//check how many descriptors there are, if there's only 1 we can just pick that one right now
		List<GameRuleDesignComponentDescriptor> popupDescriptors = GameRuleDesigner.instance.componentSelectionMap[t];
		if (popupDescriptors.Count == 1)
			assignDescriptor(popupDescriptors[0]);
		//otherwise, build the popup using the descriptor list, also give it the type so it can check stuff
		//it will assign the default descriptor
		else {
			componentPopup = GameObject.Instantiate(GameRuleDesigner.instance.ruleDesignPopupPrefab).GetComponent<GameRuleDesignPopup>();
			componentPopup.buildPopup(this, popupDescriptors, t);
		}
	}
	//this component is controlled by the other component
	public GameRuleDesignComponent(GameRuleDesignComponent otherComponent) {
		complementComponent(otherComponent);
	}
	public void assignDescriptor(GameRuleDesignComponentDescriptor newDescriptor) {
		//if there's no change then don't change anything
		if (newDescriptor == descriptor)
			return;

		//if we're replacing a descriptor we need to get rid of everything it had
		if (descriptor != null)
			destroyComponent(false);
		//most components have an icon that represents it
		if (newDescriptor.displayIcon != null) {
			componentIcon = (RectTransform)(GameObject.Instantiate(newDescriptor.displayIcon).transform);
			if (componentPopup != null)
				componentPopup.placeIcon(componentIcon);
			else {
				componentIcon.SetParent(GameRuleDesigner.instance.iconContainerTransform);
				componentIcon.localScale = new Vector3(1.0f, 1.0f);
			}
		//some descriptors are part of a popup but don't have an icon prefab
		//the popup will give us the icon to use
		} else if (componentPopup != null)
			componentIcon = componentPopup.setPopupIcon(newDescriptor);
		//if it's got subcomponents, each one of them needs a popup
		//we also need to pick a default for each one
		if (newDescriptor.subComponents != null) {
			subComponents = new GameRuleDesignComponent[newDescriptor.subComponents.Length];
			for (int i = 0; i < subComponents.Length; i++) {
				//the component will build itself from the popup types list
				subComponents[i] = new GameRuleDesignComponent(newDescriptor.subComponents[i]);
			}
		} else
			subComponents = null;
		descriptor = newDescriptor;
		//a popup will also have a component icon so be sure to check for the popup first
		if (componentPopup != null)
			componentDisplayed = (RectTransform)(componentPopup.transform);
		else if (componentIcon != null)
			componentDisplayed = componentIcon;
		else
			componentDisplayed = null;
//if (descriptor is GameRuleDesignComponentClassDescriptor)
//Debug.Log(((GameRuleDesignComponentClassDescriptor)(descriptor)).typeDescribed + ", " + componentIcon + ", " + componentPopup);
if (componentDisplayed != null)
Debug.Log("Component displayed is " + componentDisplayed.gameObject);
else if (descriptor is GameRuleDesignComponentClassDescriptor)
Debug.Log("Component displayed is null for " + ((GameRuleDesignComponentClassDescriptor)(descriptor)).typeDescribed);
else
Debug.Log("Component displayed is null for " + descriptor);
	}
	public void destroyComponent(bool destroyPopup) {
		if (componentIcon != null)
			GameObject.Destroy(componentIcon.gameObject);
		if (destroyPopup)
			componentPopup.destroyPopup();
		foreach (GameRuleDesignComponent subComponent in subComponents)
			subComponent.destroyComponent(true);
	}
	public void complementComponent(GameRuleDesignComponent otherComponent) {
		GameRuleDesignComponentDescriptor otherDescriptor = otherComponent.descriptor;
		if (otherDescriptor is GameRuleDesignComponentClassDescriptor) {
			System.Type otherType = ((GameRuleDesignComponentClassDescriptor)otherDescriptor).typeDescribed;
			if (otherType == typeof(GameRuleEventHappenedCondition))
				assignDescriptor(GameRuleDesigner.instance.gameRuleEffectActionDescriptor);
			else if (otherType == typeof(GameRuleZoneCondition))
				assignDescriptor(GameRuleDesigner.instance.gameRuleMetaRuleActionDescriptor);
			else
				throw new System.Exception("Bug: cannot complement design component descriptor type " + otherType);
		} else
			throw new System.Exception("Bug: cannot complement design component descriptor " + otherDescriptor);
	}
	//return the rightmost x and max height of the icons for this component
	public Vector2 getDisplaySize() {
string objectname = componentDisplayed == null ? null : " " + componentDisplayed.gameObject;
Vector2 displaySize = componentDisplayed == null ? new Vector2() : componentDisplayed.sizeDelta;
if (descriptor is GameRuleDesignComponentClassDescriptor)
Debug.Log(((GameRuleDesignComponentClassDescriptor)(descriptor)).typeDescribed + objectname + " display size " + displaySize.x + ", " + displaySize.y);
else if (descriptor is GameRuleDesignComponentEventTypeDescriptor)
Debug.Log(((GameRuleDesignComponentEventTypeDescriptor)(descriptor)).eventTypeDescribed + objectname + " display size " + displaySize.x + ", " + displaySize.y);
else if (descriptor is GameRuleDesignComponentIntDescriptor)
Debug.Log(((GameRuleDesignComponentIntDescriptor)(descriptor)).intDescribed + objectname + " display size " + displaySize.x + ", " + displaySize.y);
else
Debug.Log(descriptor + objectname + " display size " + displaySize.x + ", " + displaySize.y);
		if (subComponents != null) {
			Vector2 result = componentDisplayed == null ? new Vector2() : componentDisplayed.sizeDelta;
			for (int i = 0; i < subComponents.Length; i++) {
				Vector2 componentDisplaySize = subComponents[i].getDisplaySize();
				result.x += componentDisplaySize.x;
				result.y = Mathf.Max(result.y, componentDisplaySize.y);
			}
Debug.Log("Total display size " + result.x + ", " + result.y);
			return result;
		} else
			return componentDisplayed.sizeDelta;
	}
	//receives the left x to use for icons and returns the right x of the icons placed
	public float relocateIcons(float leftX) {
string objectname = componentDisplayed == null ? null : " " + componentDisplayed.gameObject.name;
string descriptorname;
if (descriptor is GameRuleDesignComponentClassDescriptor)
descriptorname = ((GameRuleDesignComponentClassDescriptor)(descriptor)).typeDescribed + objectname;
else if (descriptor is GameRuleDesignComponentEventTypeDescriptor)
descriptorname = ((GameRuleDesignComponentEventTypeDescriptor)(descriptor)).eventTypeDescribed + objectname;
else if (descriptor is GameRuleDesignComponentIntDescriptor)
descriptorname = ((GameRuleDesignComponentIntDescriptor)(descriptor)).intDescribed + objectname;
else
descriptorname = descriptor + objectname;
		//if componentDisplayed is null (like for GameRuleEffectAction), then it will have subcomponents and displayIconIndex will be -1
		if (subComponents != null) {
if (componentDisplayed == null) Debug.Log("No icon for " + descriptorname);
			int displayIconIndex = descriptor.displayIconIndex;
			for (int i = 0; i < subComponents.Length; i++) {
				if (i == displayIconIndex) {
Debug.Log("Relocating display for " + descriptorname + " with subcomponents to " + leftX);
					componentDisplayed.anchoredPosition = new Vector2(leftX, 0);
					leftX += componentDisplayed.sizeDelta.x;
				}
				leftX = subComponents[i].relocateIcons(leftX);
			}
			return leftX;
		} else {
Debug.Log("Relocating display for " + descriptorname + " without subcomponents to " + leftX);
			componentDisplayed.anchoredPosition = new Vector2(leftX, 0);
			return leftX + componentDisplayed.sizeDelta.x;
		}
	}
}
