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

	GameRuleDesignComponentClassDescriptor gameRuleEffectActionDescriptor;
	GameRuleDesignComponentClassDescriptor gameRuleMetaRuleActionDescriptor;
	GameRuleDesignComponentClassDescriptor gameRulePlayerSwapMetaRuleDescriptor;
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> componentSelectionMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();
	GameRuleDesignComponent gameRuleComponent;

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
				GameRuleIconStorage.instance.genericEventIcon, 1, false,
				new System.Type[] {
					typeof(GameRuleSelector),
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
					typeof(GameRuleEffect)
				});
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
			//event types for GameRuleEventHappenedConditions
			GameRuleDesignComponentEventTypeDescriptor kickDescriptor = new GameRuleDesignComponentEventTypeDescriptor(
				GameRuleEventType.Kick,
				GameRuleIconStorage.instance.kickIcon, 1, true,
				new System.Type[] {
					typeof(RuleStubEventSource),
					typeof(RuleStubEventTarget)
				});
			GameRuleDesignComponentEventTypeDescriptor grabDescriptor = new GameRuleDesignComponentEventTypeDescriptor(
				GameRuleEventType.Grab,
				GameRuleIconStorage.instance.grabIcon, 1, true,
				new System.Type[] {
					typeof(RuleStubEventSource),
					typeof(RuleStubEventTarget)
				});
			GameRuleDesignComponentEventTypeDescriptor bumpDescriptor = new GameRuleDesignComponentEventTypeDescriptor(
				GameRuleEventType.Bump,
				GameRuleIconStorage.instance.bumpIcon, 1, true,
				new System.Type[] {
					typeof(RuleStubEventSource),
					typeof(RuleStubEventTarget)
				});
			GameRuleDesignComponentEventTypeDescriptor smackDescriptor = new GameRuleDesignComponentEventTypeDescriptor(
				GameRuleEventType.Smack,
				GameRuleIconStorage.instance.smackIcon, 1, true,
				new System.Type[] {
					typeof(RuleStubEventSource),
					typeof(RuleStubEventTarget)
				});
			//zone types
			GameRuleDesignComponentZoneTypeDescriptor boomerangZoneDescriptor = new GameRuleDesignComponentZoneTypeDescriptor(
				GameRuleRequiredObjectType.BoomerangZone,
				GameRuleIconStorage.instance.boomerangZoneIcon, 0, true, null);
			//effects
			GameRuleDesignComponentClassDescriptor gameRulePointsPlayerEffectDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRulePointsPlayerEffect),
				null, -1, false,
				new System.Type[] {
					typeof(int)
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
				null, -1, false, null);
			//ints don't get a descriptor set because different int handlers get different ranges with different appearences
			//action durations
			GameRuleDesignComponentClassDescriptor gameRuleActionFixedDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionFixedDuration),
				GameRuleIconStorage.instance.clockIcon, 0, true,
				new System.Type[] {
					typeof(int)
				});
			GameRuleDesignComponentClassDescriptor gameRuleActionUntilConditionDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionUntilConditionDuration),
				GameRuleIconStorage.instance.clockIcon, 0, true,
				new System.Type[] {
					typeof(GameRuleEventHappenedCondition)
				});

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
			List<GameRuleDesignComponentDescriptor> gameRuleEventTypeSelection = (componentSelectionMap[typeof(GameRuleEventType)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleEventTypeSelection.Add(kickDescriptor);
			gameRuleEventTypeSelection.Add(grabDescriptor);
			gameRuleEventTypeSelection.Add(bumpDescriptor);
			gameRuleEventTypeSelection.Add(smackDescriptor);
			List<GameRuleDesignComponentDescriptor> gameRuleSourceSelectorSelection = (componentSelectionMap[typeof(GameRuleSourceSelector)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleSourceSelectorSelection.Add(gameRulePlayerSelectorDescriptor);
			gameRuleSourceSelectorSelection.Add(gameRuleBallSelectorDescriptor);
			List<GameRuleDesignComponentDescriptor> zoneTypeSelection = (componentSelectionMap[typeof(RuleStubZoneType)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleEventTypeSelection.Add(boomerangZoneDescriptor);
			List<GameRuleDesignComponentDescriptor> gameRuleEffectSelection = (componentSelectionMap[typeof(GameRuleEffect)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleEffectSelection.Add(gameRulePointsPlayerEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleDuplicateEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleFreezeEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleDizzyEffectDescriptor);
			gameRuleEffectSelection.Add(gameRuleBounceEffectDescriptor);
			List<GameRuleDesignComponentDescriptor> fieldObjectSelection = (componentSelectionMap[typeof(FieldObject)] = new List<GameRuleDesignComponentDescriptor>());
			//field object types
			//use the registry to build this
			foreach (GameRuleSpawnableObject spawnableObject in GameRuleSpawnableObjectRegistry.instance.goalSpawnableObjects) {
				fieldObjectSelection.Add(new GameRuleDesignComponentFieldObjectDescriptor(
					spawnableObject.spawnedObject.GetComponent<FieldObject>().sportName,
					spawnableObject.icon, 0, true, null));
			}
			fieldObjectSelection.Add(new GameRuleDesignComponentFieldObjectDescriptor(
				"boundary",
				GameRuleIconStorage.instance.boundaryIcon, 0, true, null));

			//and finally we'll build our complete rule that the user can change
			gameRuleComponent = new GameRuleDesignComponent(
				new GameRuleDesignComponentClassDescriptor(
					typeof(GameRule),
					GameRuleIconStorage.instance.resultsInIcon, 1, false,
					new System.Type[] {
						typeof(GameRuleCondition),
						typeof(GameRuleAction)
					}));
			GameObject componentPopup1 = (GameObject)(Instantiate(ruleDesignPopupPrefab));
			componentPopup1.transform.SetParent(GameRuleDesigner.instance.iconContainerTransform);
			componentPopup1.transform.localScale = new Vector3(1.0f, 1.0f);
			GameObject componentPopup2 = (GameObject)(Instantiate(ruleDesignPopupPrefab));
			componentPopup2.transform.SetParent(GameRuleDesigner.instance.iconContainerTransform);
			componentPopup2.transform.localScale = new Vector3(1.0f, 1.0f);
			gameRuleComponent.subComponents = new GameRuleDesignComponent[] {
				new GameRuleDesignComponent(gameRuleEventHappenedConditionDescriptor, componentPopup1),
				new GameRuleDesignComponent(gameRuleEffectActionDescriptor, componentPopup2)
			};
			redisplayRule();

			finishedInstantiating = true;
		}
	}
	public void redisplayRule() {
		//start by relocating the icons appropriately
		Vector2 displaySize = gameRuleComponent.getDisplaySize();
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
	public GameRuleDesignComponentEventTypeDescriptor(GameRuleEventType etd, GameObject di, int dii, bool si, System.Type[] sc):
		base(di, dii, si, sc) {
		eventTypeDescribed = etd;
	}
}
public class GameRuleDesignComponentZoneTypeDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleRequiredObjectType zoneTypeDescribed;
	public GameRuleDesignComponentZoneTypeDescriptor(GameRuleRequiredObjectType ztd, GameObject di, int dii, bool si, System.Type[] sc):
		base(di, dii, si, sc) {
		zoneTypeDescribed = ztd;
	}
}
public class GameRuleDesignComponentFieldObjectDescriptor : GameRuleDesignComponentDescriptor {
	public string fieldObjectDescribed;
	public GameRuleDesignComponentFieldObjectDescriptor(string fod, GameObject di, int dii, bool si, System.Type[] sc) :
		base(di, dii, si, sc) {
		fieldObjectDescribed = fod;
	}
}
public class GameRuleDesignComponent {
	public GameRuleDesignComponentDescriptor descriptor = null;
	public GameObject componentIcon = null;
	public GameObject componentPopup = null;
	public GameRuleDesignComponent[] subComponents = null;
	Vector2 displaySize;
	public GameRuleDesignComponent(GameRuleDesignComponentDescriptor d, GameObject c) {
		descriptor = d;
		componentPopup = c;
		displaySize = ((RectTransform)(c.transform)).sizeDelta;
		GameRuleDesignPopup popup = componentPopup.GetComponent<GameRuleDesignPopup>();
		popup.addIcon(GameRuleIconStorage.instance.resultsInIcon);
		popup.addIcon(GameRuleIconStorage.instance.resultsInIcon);
		popup.addIcon(GameRuleIconStorage.instance.resultsInIcon);
		popup.addIcon(GameRuleIconStorage.instance.resultsInIcon);
	}
	public GameRuleDesignComponent(GameRuleDesignComponentDescriptor d) {
		assignDescriptor(d);
	}
	public void assignDescriptor(GameRuleDesignComponentDescriptor newDescriptor) {
		componentIcon = (GameObject)GameObject.Instantiate(newDescriptor.displayIcon);
		componentIcon.transform.SetParent(GameRuleDesigner.instance.iconContainerTransform);
		componentIcon.transform.localScale = new Vector3(1.0f, 1.0f);
//		componentPopup = (GameObject)GameObject.Instantiate(GameRuleDesigner.instance.ruleDesignPopupPrefab);
//		componentPopup.transform.SetParent(parentIcon.transform);
//		componentPopup.transform.localPosition = new Vector3(0.0f, 0.0f);
//		componentPopup.transform.localScale = new Vector3(1.0f, 1.0f);
		//if we're replacing a descriptor we need to get rid of the old stuff
		if (descriptor != null) {

		}
		descriptor = newDescriptor;
		if (newDescriptor.subComponents != null)
			subComponents = new GameRuleDesignComponent[newDescriptor.subComponents.Length];
		displaySize = ((RectTransform)((componentIcon != null ? componentIcon : componentPopup).transform)).sizeDelta;
	}
	//return the rightmost x and max height of the icons for this component
	public Vector2 getDisplaySize() {
		if (subComponents != null) {
			Vector2 result = displaySize;
			for (int i = 0; i < subComponents.Length; i++) {
				Vector2 componentDisplaySize = subComponents[i].getDisplaySize();
				result.x += componentDisplaySize.x;
				result.y = Mathf.Max(result.y, componentDisplaySize.y);
			}
			return result;
		} else
			return displaySize;
	}
	//receives the left x to use for icons and returns the right x of the icons placed
	public float relocateIcons(float leftX) {
		GameObject componentDisplayed = componentIcon != null ? componentIcon : componentPopup;
		if (subComponents != null) {
			int displayIconIndex = descriptor.displayIconIndex;
			for (int i = 0; i < subComponents.Length; i++) {
				if (i == displayIconIndex) {
					((RectTransform)(componentDisplayed.transform)).anchoredPosition = new Vector2(leftX, 0);
					leftX += displaySize.x;
				}
				leftX = subComponents[i].relocateIcons(leftX);
			}
			return leftX;
		} else {
			((RectTransform)(componentDisplayed.transform)).anchoredPosition = new Vector2(leftX, 0);
			return leftX + displaySize.x;
		}
	}
}
