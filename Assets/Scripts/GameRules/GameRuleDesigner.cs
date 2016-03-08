using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//these are empty classes that exist solely for the purpose of producing a System.Type
class RuleStubZoneType {}

public class GameRuleDesigner : MonoBehaviour {
	public static GameRuleDesigner instance;

	private bool finishedInstantiating = false;

	public GameRuleDesignComponentClassDescriptor gameRuleEventHappenedConditionDurationDescriptor;
	public GameRuleDesignComponentClassDescriptor gameRuleEffectActionDescriptor;
	public GameRuleDesignComponentClassDescriptor gameRuleMetaRuleActionDescriptor;
	public GameRuleDesignComponentClassDescriptor gameRuleActionUntilConditionDurationDescriptor;
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> componentSelectionMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();

	//all possible targets that a given event type and event source can have
	public Dictionary<GameRuleEventType, List<GameRuleDesignComponentDescriptor>> potentialSourcesForEventMap =
		new Dictionary<GameRuleEventType, List<GameRuleDesignComponentDescriptor>>();
	public Dictionary<GameRuleEventType, Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>> potentialTargetsForEventSourceMap =
		new Dictionary<GameRuleEventType, Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>>();
	//all possible event types that a given source can have
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> potentialEventsForSourceMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();

	//possible selectors for the source
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> sourceSelectorMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();
	public Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> sourceEffectMap =
		new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>();

	//each zone type has a meta rule
	public Dictionary<GameRuleRequiredObjectType, GameRuleDesignComponentDescriptor> zoneTypeMetaRuleMap =
		new Dictionary<GameRuleRequiredObjectType, GameRuleDesignComponentDescriptor>();

	public GameRuleDesignComponent gameRuleComponent;
	public GameRuleDesignComponent gameRuleConditionComponent;
	public GameRuleDesignComponent gameRuleActionComponent;

	public RectTransform uiCanvas;
	public RectTransform ruleDesignPanel;
	[HideInInspector]
	public RectTransform iconContainerTransform;
	float iconDisplayMaxHeight;
	float iconDisplayMaxWidth;
	Vector2 iconDisplaySizeDelta;
	public GameObject ruleDesignPopupPrefab;
	public GameObject dataStoragePrefab; //this is shared across scenes, so keep it as just a prefab
	public GameObject cancelIconPopupPrefab;
	public Text createRuleButtonText;

	public RuleNetworking ruleNetworking;
	[HideInInspector]
	public string ruleName;

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
			//add descriptors for selectable components to the selection map
			//classes with subcomponents will also produce popups
			//conditions
			List<GameRuleDesignComponentDescriptor> gameRuleConditionSelection = (componentSelectionMap[typeof(GameRuleCondition)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleEventHappenedConditionDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleEventHappenedCondition),
				GameRuleIconStorage.instance.genericEventIcon, 0,
				new System.Type[] {
					typeof(GameRuleEventType)
				});
			gameRuleConditionSelection.Add(gameRuleEventHappenedConditionDurationDescriptor);
			gameRuleConditionSelection.Add(new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleZoneCondition),
				GameRuleIconStorage.instance.genericZoneIcon, 1,
				new System.Type[] {
					typeof(GameRuleSourceSelector),
					typeof(RuleStubZoneType)
				}));

			//actions get specially handled since their class is controlled by the condition
			//they don't have icons because they never appear in popups and are fully represented by the icons in their subcomponents
			//the subcomponents will get assigned in complementComponent
			gameRuleEffectActionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleEffectAction),
				null, -1,
				null);
			//metarules don't even have any popup, they are 100% 1:1 with conditions
			gameRuleMetaRuleActionDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleMetaRuleAction),
				null, -1,
				null);

			//event types for GameRuleEventHappenedConditions
			List<GameRuleDesignComponentDescriptor> gameRuleEventTypeSelection = (componentSelectionMap[typeof(GameRuleEventType)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (GameRuleEventType eventType in GameRuleEvent.eventTypesList) {
				gameRuleEventTypeSelection.Add(new GameRuleDesignComponentEventTypeDescriptor(eventType, 1));
			}

			//selectors
			//these get used for a few things
			//selectors and effects for actions change based on the event type source
			List<GameRuleDesignComponentDescriptor> playerSelectorSelection = (sourceSelectorMap[typeof(TeamPlayer)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (GameRuleSelector selector in GameRuleSelector.getPlayerSourceSelectors()) {
				playerSelectorSelection.Add(new GameRuleDesignComponentSelectorDescriptor(selector));
			}
			List<GameRuleDesignComponentDescriptor> ballSelectorSelection = (sourceSelectorMap[typeof(Ball)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (GameRuleSelector selector in GameRuleSelector.getBallSourceSelectors()) {
				ballSelectorSelection.Add(new GameRuleDesignComponentSelectorDescriptor(selector));
			}

			//zone types
			//each zone type corresponds to one metarule
			List<GameRuleDesignComponentDescriptor> zoneSelection = (componentSelectionMap[typeof(RuleStubZoneType)] = new List<GameRuleDesignComponentDescriptor>());
			zoneSelection.Add(new GameRuleDesignComponentZoneTypeDescriptor(
				GameRuleRequiredObjectType.BoomerangZone,
				GameRuleIconStorage.instance.boomerangZoneIcon));
			//right now this is only for zones which are only on players
			List<GameRuleDesignComponentDescriptor> gameRuleSourceSelectorSelection = (componentSelectionMap[typeof(GameRuleSourceSelector)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleSourceSelectorSelection.Add(new GameRuleDesignComponentSelectorDescriptor(GameRulePlayerSelector.instance));

			//effects
			//first, map each effect to a descriptor
			Dictionary<System.Type, GameRuleDesignComponentDescriptor> effectTypeDescriptorMap = new Dictionary<System.Type, GameRuleDesignComponentDescriptor>();
			effectTypeDescriptorMap[typeof(GameRulePointsPlayerEffect)] = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRulePointsPlayerEffect),
				//this descriptor doesn't have an icon but components for it will make one
				null, 0,
				new System.Type[] {
					//we'll just use this as the key for our list of possible point values
					typeof(GameRulePointsPlayerEffect)
				});
			effectTypeDescriptorMap[typeof(GameRuleDuplicateEffect)] = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleDuplicateEffect),
				GameRuleIconStorage.instance.duplicatedIcon, 0, null);
			effectTypeDescriptorMap[typeof(GameRuleFreezeEffect)] = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleFreezeEffect),
				GameRuleIconStorage.instance.frozenIcon, 0,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			effectTypeDescriptorMap[typeof(GameRuleDizzyEffect)] = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleDizzyEffect),
				GameRuleIconStorage.instance.dizzyIcon, 0,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			effectTypeDescriptorMap[typeof(GameRuleBounceEffect)] = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleBounceEffect),
				GameRuleIconStorage.instance.bouncyIcon, 0,
				new System.Type[] {
					typeof(GameRuleActionDuration)
				});
			//next, go through and pull all the effect types from their corresponding list
			List<GameRuleDesignComponentDescriptor> playerEffectSelection = (sourceEffectMap[typeof(TeamPlayer)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (System.Type effectType in GameRuleEffect.playerSourceEffects) {
				playerEffectSelection.Add(effectTypeDescriptorMap[effectType]);
			}
			List<GameRuleDesignComponentDescriptor> ballEffectSelection = (sourceEffectMap[typeof(Ball)] = new List<GameRuleDesignComponentDescriptor>());
			foreach (System.Type effectType in GameRuleEffect.ballSourceEffects) {
				ballEffectSelection.Add(effectTypeDescriptorMap[effectType]);
			}

			//metarules are controlled by the zones
			zoneTypeMetaRuleMap[GameRuleRequiredObjectType.BoomerangZone] = new GameRuleDesignComponentMetaRuleDescriptor(
				GameRulePlayerSwapMetaRule.instance);
			
			//event sources and targets
			GameRuleDesignComponentEventParticipantDescriptor teamPlayerDescriptor = new GameRuleDesignComponentEventParticipantDescriptor(
				typeof(TeamPlayer),
				GameRuleIconStorage.instance.playerIcon);
			GameRuleDesignComponentEventParticipantDescriptor ballDescriptor = new GameRuleDesignComponentEventParticipantDescriptor(
				typeof(Ball),
				GameRuleIconStorage.instance.genericBallIcon);
			List<GameRuleDesignComponentDescriptor> fieldObjectDescriptors = new List<GameRuleDesignComponentDescriptor>();
			foreach (GameRuleSpawnableObject spawnableObject in GameRuleSpawnableObjectRegistry.instance.goalSpawnableObjects) {
				fieldObjectDescriptors.Add(new GameRuleDesignComponentFieldObjectDescriptor(
					spawnableObject.spawnedObject.GetComponent<FieldObject>().sportName,
					spawnableObject.icon));
			}
			fieldObjectDescriptors.Add(new GameRuleDesignComponentFieldObjectDescriptor(
				"boundary",
				GameRuleIconStorage.instance.boundaryIcon));
			//go through all the event types and build up the lists of the potential source and target descriptors per event type
			foreach (GameRuleEventType eventType in GameRuleEvent.eventTypesList) {
				List<GameRuleDesignComponentDescriptor> sourcesList = (potentialSourcesForEventMap[eventType] = new List<GameRuleDesignComponentDescriptor>());
				Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>> targetsMap =
					(potentialTargetsForEventSourceMap[eventType] = new Dictionary<System.Type, List<GameRuleDesignComponentDescriptor>>());

				foreach (KeyValuePair<System.Type, List<System.Type>> sourceAndTargets in GameRuleEvent.potentialEventsList[eventType]) {
					//add this source to the list of sources for the event type
					System.Type sourceType = sourceAndTargets.Key;
					if (sourceType == typeof(TeamPlayer))
						sourcesList.Add(teamPlayerDescriptor);
					else if (sourceType == typeof(Ball))
						sourcesList.Add(ballDescriptor);
					else
						throw new System.Exception("Bug: invalid event source type " + sourceType);

					//add all the targets to this source's list of targets for this event type
					List<GameRuleDesignComponentDescriptor> targetsList = (targetsMap[sourceType] = new List<GameRuleDesignComponentDescriptor>());
					foreach (System.Type targetType in sourceAndTargets.Value) {
						if (targetType == typeof(TeamPlayer))
							targetsList.Add(teamPlayerDescriptor);
						else if (targetType == typeof(Ball))
							targetsList.Add(ballDescriptor);
						else if (targetType == typeof(FieldObject))
							targetsList.AddRange(fieldObjectDescriptors);
						else
							throw new System.Exception("Bug: invalid event source type " + sourceType);
					}
				}
			}

			//point descriptors for a points effect
			List<GameRuleDesignComponentDescriptor> pointAmountSelection = (componentSelectionMap[typeof(GameRulePointsPlayerEffect)] = new List<GameRuleDesignComponentDescriptor>());
			for (int i = GameRulePointsPlayerEffect.POINTS_SERIALIZATION_MAX_VALUE - GameRulePointsPlayerEffect.POINTS_SERIALIZATION_MASK;
				i <= GameRulePointsPlayerEffect.POINTS_SERIALIZATION_MAX_VALUE;
				i++) {
				pointAmountSelection.Add(new GameRuleDesignComponentIntDescriptor(i));
			}

			//action durations
			List<GameRuleDesignComponentDescriptor> gameRuleActionDurationSelection = (componentSelectionMap[typeof(GameRuleActionDuration)] = new List<GameRuleDesignComponentDescriptor>());
			gameRuleActionDurationSelection.Add(new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionFixedDuration),
				GameRuleIconStorage.instance.clockIcon, 0,
				new System.Type[] {
					//we'll just use this as the key for our list of possible second durations
					typeof(GameRuleActionFixedDuration)
				}));
			gameRuleActionUntilConditionDurationDescriptor = new GameRuleDesignComponentClassDescriptor(
				typeof(GameRuleActionUntilConditionDuration),
				GameRuleIconStorage.instance.genericEventIcon, 0,
				//we need an event happend condition but in a different form, we'll fill this in when this descriptor is assigned
				null);
			gameRuleActionDurationSelection.Add(gameRuleActionUntilConditionDurationDescriptor);

			//now that we have our descriptors, we can build our selection map
			//only some classes make easy popups, the others will need special handling
			//duration length descriptors for fixed durations
			List<GameRuleDesignComponentDescriptor> fixedDurationSelection = (componentSelectionMap[typeof(GameRuleActionFixedDuration)] = new List<GameRuleDesignComponentDescriptor>());
			for (int i = 0; i <= GameRuleActionFixedDuration.DURATION_SERIALIZATION_MASK; i++) {
				fixedDurationSelection.Add(new GameRuleDesignComponentIntDescriptor(i));
			}

			//for until-condition durations, each source type has a list of valid events that it can use
			foreach (KeyValuePair<System.Type, List<GameRuleEventType>> sourceTypeWithEventTypes in GameRuleEvent.potentialEventTypesMap) {
				List<GameRuleDesignComponentDescriptor> eventTypesForSource = (potentialEventsForSourceMap[sourceTypeWithEventTypes.Key] = new List<GameRuleDesignComponentDescriptor>());
				foreach (GameRuleEventType eventType in sourceTypeWithEventTypes.Value) {
					eventTypesForSource.Add(new GameRuleDesignComponentEventTypeDescriptor(eventType, 0));
				}
			}

			//and finally we'll build our complete rule that the user can change
			gameRuleConditionComponent = new GameRuleDesignComponent(null, typeof(GameRuleCondition));
			gameRuleActionComponent = new GameRuleDesignComponent(null, gameRuleConditionComponent);
			gameRuleComponent = new GameRuleDesignComponent(
				null,
				new GameRuleDesignComponentClassDescriptor(
					typeof(GameRule),
					GameRuleIconStorage.instance.resultsInIcon, 1, null));
			gameRuleComponent.subComponents = new GameRuleDesignComponent[] {gameRuleConditionComponent, gameRuleActionComponent};
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

		//and then go and update the create rule button text
		ruleName = gameRuleComponent.getGameRuleString();
		createRuleButtonText.text = ruleName + "\nCreate Rule";
	}
	public void createRule() {
		ruleNetworking.sendRule(ruleName);
	}
}

public abstract class GameRuleDesignComponentDescriptor {
	public GameObject displayIcon;
	public int displayIconIndex; //-1 means no image will show up
	public System.Type[] subComponents; //this is used to retrieve from componentSelectionMap
	public GameRuleDesignComponentDescriptor(GameObject di, int dii, System.Type[] sc) {
		displayIcon = di;
		displayIconIndex = dii;
		subComponents = sc;
	}
}
public class GameRuleDesignComponentClassDescriptor : GameRuleDesignComponentDescriptor {
	public System.Type typeDescribed;
	public GameRuleDesignComponentClassDescriptor(System.Type td, GameObject di, int dii, System.Type[] sc) :
		base(di, dii, sc) {
		typeDescribed = td;
	}
}
public class GameRuleDesignComponentSelectorDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleSelector selectorDescribed;
	public GameRuleDesignComponentSelectorDescriptor(GameRuleSelector sd) :
		base(null, 0, null) {
		List<GameObject> iconList = new List<GameObject>();
		sd.addIcons(iconList);
		displayIcon = iconList[0];
		selectorDescribed = sd;
	}
}
public class GameRuleDesignComponentMetaRuleDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleMetaRule metaRuleDescribed;
	public GameRuleDesignComponentMetaRuleDescriptor(GameRuleMetaRule md) :
		//meta rules don't have an icon but components for them will make one
		base(null, 0, null) {
		metaRuleDescribed = md;
	}
}
public class GameRuleDesignComponentEventTypeDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleEventType eventTypeDescribed;
	public GameRuleDesignComponentEventTypeDescriptor(GameRuleEventType etd, int dii) :
		base(GameRuleEvent.getEventIcon(etd), dii, null) {
		eventTypeDescribed = etd;
	}
}
public class GameRuleDesignComponentZoneTypeDescriptor : GameRuleDesignComponentDescriptor {
	public GameRuleRequiredObjectType zoneTypeDescribed;
	public GameRuleDesignComponentZoneTypeDescriptor(GameRuleRequiredObjectType ztd, GameObject di) :
		base(di, 0, null) {
		zoneTypeDescribed = ztd;
	}
}
public class GameRuleDesignComponentFieldObjectDescriptor : GameRuleDesignComponentDescriptor {
	public string fieldObjectDescribed;
	public GameRuleDesignComponentFieldObjectDescriptor(string fod, GameObject di) :
		base(di, 0, null) {
		fieldObjectDescribed = fod;
	}
}
public class GameRuleDesignComponentIntDescriptor : GameRuleDesignComponentDescriptor {
	public int intDescribed;
	public GameRuleDesignComponentIntDescriptor(int id) :
		base(null, 0, null) {
		intDescribed = id;
	}
}
//there is no difference between this class and GameRuleDesignComponentClassDescriptor other than class type
//we need this though so that we can tell when we have a source or target of an event
public class GameRuleDesignComponentEventParticipantDescriptor : GameRuleDesignComponentDescriptor {
	public System.Type typeDescribed;
	public GameRuleDesignComponentEventParticipantDescriptor(System.Type td, GameObject di) :
		base(di, 0, null) {
		typeDescribed = td;
	}
}
public class GameRuleDesignComponent {
	public GameRuleDesignComponent parent;
	public GameRuleDesignComponentDescriptor descriptor = null;
	public RectTransform componentIcon = null; //the icon that represents this component, if there's no popup
	public GameRuleDesignPopup componentPopup = null; //a popup of different descriptors that this component can be
	public RectTransform componentDisplayed;
	public GameRuleDesignComponent[] subComponents;
	public GameRuleDesignComponent(GameRuleDesignComponent p, GameRuleDesignComponentDescriptor d) {
		parent = p;
		assignDescriptor(d);
	}
	//construct from a list of descriptors
	//the variables for this component will change to reflect which component it represents
	public GameRuleDesignComponent(GameRuleDesignComponent p, System.Type t) {
		parent = p;
		assignPopup(GameRuleDesigner.instance.componentSelectionMap[t], t);
	}
	//construct from a specific list of descriptors not obainable from the component selection map
	public GameRuleDesignComponent(GameRuleDesignComponent p, List<GameRuleDesignComponentDescriptor> popupDescriptors) {
		parent = p;
		assignPopup(popupDescriptors, null);
	}
	//this component is controlled by the other component
	public GameRuleDesignComponent(GameRuleDesignComponent p, GameRuleDesignComponent otherComponent) {
		parent = p;
		complementComponent(otherComponent);
	}
	//we need to do special initialization and we need to construct this without it doing anything
	public GameRuleDesignComponent(GameRuleDesignComponent p) {
		parent = p;
	}
	public void assignDescriptor(GameRuleDesignComponentDescriptor newDescriptor) {
		//if there's no change then don't change anything
		if (newDescriptor == descriptor)
			return;

		//if we're replacing a descriptor we need to get rid of everything it had
		bool hadOldDescriptor = descriptor != null;
		if (hadOldDescriptor)
			destroyComponent(false);
		descriptor = newDescriptor;

		//if the descriptor is part of a popup then it will manage the icon
		if (componentPopup != null)
			componentIcon = componentPopup.setPopupIcon(newDescriptor);
		//most components that are not part of a popup have an icon that represents it
		else if (newDescriptor.displayIcon != null) {
			componentIcon = getIconMaybeOpponent(newDescriptor);
			componentIcon.SetParent(GameRuleDesigner.instance.iconContainerTransform);
			componentIcon.localScale = new Vector3(1.0f, 1.0f);
		}

		//if it's got subcomponents, each one of them needs a popup
		//we also need to pick a default for each one
		//each event type has its own list that it will use
		if (newDescriptor is GameRuleDesignComponentEventTypeDescriptor) {
			GameRuleEventType eventType = ((GameRuleDesignComponentEventTypeDescriptor)newDescriptor).eventTypeDescribed;
			//event-happened conditions and until-conditions both have event types
			//based on which one this is, the structure of the subcomponents is different
			//if we're in a regular event-happened condition, we have a source type and target type
			if (parent.descriptor == GameRuleDesigner.instance.gameRuleEventHappenedConditionDurationDescriptor) {
				//if we had an old descriptor, we want to check if the source type changed
				System.Type oldSourceType;
				if (hadOldDescriptor)
					oldSourceType = ((GameRuleDesignComponentEventParticipantDescriptor)(subComponents[0].descriptor)).typeDescribed;
				//otherwise, we need to initialize the subcomponents array
				else {
					oldSourceType = null;
					subComponents = new GameRuleDesignComponent[2];
				}
				//constructing the component auto-assigns the target descriptor
				(subComponents[0] = new GameRuleDesignComponent(this)).assignPopup(GameRuleDesigner.instance.potentialSourcesForEventMap[eventType], null);
				if (hadOldDescriptor) {
					System.Type newSourceType = ((GameRuleDesignComponentEventParticipantDescriptor)(subComponents[0].descriptor)).typeDescribed;
					//the source type switched, we need to refresh the effect action's subcomponents
					if (oldSourceType != newSourceType) {
						GameRuleDesigner.instance.gameRuleActionComponent.destroyComponent(false);
						GameRuleDesigner.instance.gameRuleActionComponent.assignEffectActionSelector(newSourceType);
					}
				}
			//if we're in an until-condition duration, our parent is the source type and our only subcomponent is the target type
			} else if (parent.parent.descriptor == GameRuleDesigner.instance.gameRuleActionUntilConditionDurationDescriptor) {
				if (!hadOldDescriptor)
					subComponents = new GameRuleDesignComponent[1];
				subComponents[0] = new GameRuleDesignComponent(this, GameRuleDesigner.instance.potentialTargetsForEventSourceMap
					[eventType]
					//the source of the until-condition trigger is the target of a selector on the source of the original condition
					[((GameRuleDesignComponentSelectorDescriptor)(parent.descriptor)).selectorDescribed.targetType()]);
			} else
				throw new System.Exception("Bug: event type changed with parent " + parent.descriptor);
		//this is the source or the target of an event-happened-condition, which could be the condition or an until-condition duration
		//if it's the condition, we need to change other stuff
		} else if (newDescriptor is GameRuleDesignComponentEventParticipantDescriptor) {
			//we changed the general condition source
			//we need to update the target type and possibly also an action type
			if (parent.parent.descriptor == GameRuleDesigner.instance.gameRuleEventHappenedConditionDurationDescriptor &&
				parent.subComponents[0] == this) {
				System.Type sourceType = ((GameRuleDesignComponentEventParticipantDescriptor)newDescriptor).typeDescribed;
				//if the source changed from an old one then we need to update the action
				//if this is the first time then the action gets set elsewhere
				if (hadOldDescriptor) {
					parent.subComponents[1].destroyComponent(true);
					GameRuleDesigner.instance.gameRuleActionComponent.destroyComponent(false);
					GameRuleDesigner.instance.gameRuleActionComponent.assignEffectActionSelector(sourceType);
				}
				parent.subComponents[1] = new GameRuleDesignComponent(parent, GameRuleDesigner.instance.potentialTargetsForEventSourceMap
					[((GameRuleDesignComponentEventTypeDescriptor)(parent.descriptor)).eventTypeDescribed]
					[sourceType]);
			}
		//when we assign an until-condition, we need to pick the source type and it will pick the event type which will pick the target type
		} else if (descriptor == GameRuleDesigner.instance.gameRuleActionUntilConditionDurationDescriptor) {
			subComponents = new GameRuleDesignComponent[] {
				new GameRuleDesignComponent(this, GameRuleDesigner.instance.sourceSelectorMap[
					((GameRuleDesignComponentEventParticipantDescriptor)(GameRuleDesigner.instance.gameRuleConditionComponent.subComponents[0].subComponents[0].descriptor)).typeDescribed])
			};
		//we changed a selector descriptor, this is an effect action, until-condition duration, or a zone source
		} else if (newDescriptor is GameRuleDesignComponentSelectorDescriptor) {
			System.Type parentType = ((GameRuleDesignComponentClassDescriptor)(parent.descriptor)).typeDescribed;
			//when we change the selector for an effect action, we need to update the possible effects
			if (parentType == typeof(GameRuleEffectAction)) {
				if (hadOldDescriptor)
					parent.subComponents[1].destroyComponent(true);
				parent.subComponents[1] = new GameRuleDesignComponent(parent, GameRuleDesigner.instance.sourceEffectMap[
					((GameRuleDesignComponentSelectorDescriptor)(newDescriptor)).selectorDescribed.targetType()]);
			//we changed the source of an until-condition, we need to refresh the event type
			} else if (parentType == typeof(GameRuleActionUntilConditionDuration)) {
				subComponents = new GameRuleDesignComponent[] {
					new GameRuleDesignComponent(this, GameRuleDesigner.instance.potentialEventsForSourceMap[
						((GameRuleDesignComponentSelectorDescriptor)(descriptor)).selectorDescribed.targetType()])
				};
			}
		//the rest of the descriptors build their subcomponents normally
		} else if (newDescriptor.subComponents != null) {
			subComponents = new GameRuleDesignComponent[newDescriptor.subComponents.Length];
			for (int i = 0; i < subComponents.Length; i++) {
				//the component will build itself from the popup types list
				subComponents[i] = new GameRuleDesignComponent(this, newDescriptor.subComponents[i]);
			}

			//a few components need to send changes to other components
			//zone type was changed, assign the right metarule
			if (newDescriptor is GameRuleDesignComponentZoneTypeDescriptor) {
				GameRuleRequiredObjectType zoneType = ((GameRuleDesignComponentZoneTypeDescriptor)newDescriptor).zoneTypeDescribed;
				GameRuleDesigner.instance.gameRuleActionComponent.assignMetaRuleSubComponent(zoneType);
			//the condition changed, update the action with the corresponding descriptor
			} else if (this == GameRuleDesigner.instance.gameRuleConditionComponent) {
				if (hadOldDescriptor)
					GameRuleDesigner.instance.gameRuleActionComponent.complementComponent(this);
			}
		} else
			subComponents = null;

		//save the component that actually gets displayed to render this component
		if (componentPopup != null)
			componentDisplayed = (RectTransform)(componentPopup.transform);
		else if (componentIcon != null)
			componentDisplayed = componentIcon;
		else
			componentDisplayed = null;
	}
	public void destroyComponent(bool destroyPopup) {
		if (componentIcon != null)
			GameObject.Destroy(componentIcon.gameObject);
		if (destroyPopup && componentPopup != null)
			componentPopup.destroyPopup();
		if (subComponents != null) {
			foreach (GameRuleDesignComponent subComponent in subComponents)
				subComponent.destroyComponent(true);
		}
	}
	public void assignPopup(List<GameRuleDesignComponentDescriptor> popupDescriptors, System.Type t) {
		//check how many descriptors there are, if there's only 1 we can just pick that one right now
		if (popupDescriptors.Count == 1)
			assignDescriptor(popupDescriptors[0]);
		//otherwise, build the popup using the descriptor list, also give it the type so it can check stuff
		//it will assign the default descriptor
		else {
			componentPopup = GameObject.Instantiate(GameRuleDesigner.instance.ruleDesignPopupPrefab).GetComponent<GameRuleDesignPopup>();
			componentPopup.buildPopup(this, popupDescriptors, t);
		}
	}
	public void complementComponent(GameRuleDesignComponent otherComponent) {
		GameRuleDesignComponentDescriptor otherDescriptor = otherComponent.descriptor;
		if (otherDescriptor is GameRuleDesignComponentClassDescriptor) {
			System.Type otherType = ((GameRuleDesignComponentClassDescriptor)otherDescriptor).typeDescribed;
			if (otherType == typeof(GameRuleEventHappenedCondition)) {
				assignDescriptor(GameRuleDesigner.instance.gameRuleEffectActionDescriptor);
				subComponents = new GameRuleDesignComponent[2];
				assignEffectActionSelector(((GameRuleDesignComponentEventParticipantDescriptor)(otherComponent.subComponents[0].subComponents[0].descriptor)).typeDescribed);
			} else if (otherType == typeof(GameRuleZoneCondition)) {
				assignDescriptor(GameRuleDesigner.instance.gameRuleMetaRuleActionDescriptor);
				subComponents = new GameRuleDesignComponent[1];
				assignMetaRuleSubComponent(((GameRuleDesignComponentZoneTypeDescriptor)(otherComponent.subComponents[1].descriptor)).zoneTypeDescribed);
			} else
				throw new System.Exception("Bug: cannot complement design component descriptor type " + otherType);
		} else
			throw new System.Exception("Bug: cannot complement design component descriptor " + otherDescriptor);
	}
	//set the source type for the effect action
	//the process of doing that will set the effect
	public void assignEffectActionSelector(System.Type sourceType) {
		subComponents[0] = new GameRuleDesignComponent(this, GameRuleDesigner.instance.sourceSelectorMap[sourceType]);
	}
	public void assignMetaRuleSubComponent(GameRuleRequiredObjectType zoneType) {
		GameRuleDesignComponentMetaRuleDescriptor metaRuleDescriptor = (GameRuleDesignComponentMetaRuleDescriptor)(GameRuleDesigner.instance.zoneTypeMetaRuleMap[zoneType]);
		GameRuleDesignComponent metaRuleComponent = new GameRuleDesignComponent(this, metaRuleDescriptor);

		List<GameObject> iconList = new List<GameObject>();
		metaRuleDescriptor.metaRuleDescribed.addIcons(iconList);
		metaRuleComponent.componentDisplayed = (metaRuleComponent.componentIcon = GameRuleDesignPopup.groupIcons(iconList));
		metaRuleComponent.componentIcon.SetParent(GameRuleDesigner.instance.iconContainerTransform);
		metaRuleComponent.componentIcon.localScale = new Vector3(1.0f, 1.0f);

		subComponents[0] = metaRuleComponent;
	}
	//if this is an event target then its icon may need to be substituted for its opponent
	public RectTransform getIconMaybeOpponent(GameRuleDesignComponentDescriptor d) {
		//it's an event target or an event source, if it's a target go and get the right icon for it
		if (d is GameRuleDesignComponentEventParticipantDescriptor) {
			List<GameObject> iconList = new List<GameObject>();
			//this is part of an event-happened condition
			if (parent.parent.descriptor == GameRuleDesigner.instance.gameRuleEventHappenedConditionDurationDescriptor) {
				//it's just the source, construct normally
				if (parent.subComponents[0] == this)
					return GameRuleDesignPopup.instantiateIcon(d.displayIcon);
				//it's the target, go grab the icon
				else
					GameRuleSourceSelector.selectorIdentifier(
						((GameRuleDesignComponentEventParticipantDescriptor)d).typeDescribed, true).addIcons(iconList);
			//this is the target of an until-condition duration
			} else if (parent.parent.parent.descriptor == GameRuleDesigner.instance.gameRuleActionUntilConditionDurationDescriptor) {
				GameRuleSourceSelector.selectorIdentifier(
					((GameRuleDesignComponentEventParticipantDescriptor)d).typeDescribed,
					((GameRuleDesignComponentSelectorDescriptor)parent.parent.descriptor).selectorDescribed).addIcons(iconList);
			//this should never happen
			} else
				throw new System.Exception("Bug: event participant part of " + parent.descriptor);
			return GameRuleDesignPopup.instantiateIcon(iconList[0]);
		//it's normal
		} else
			return GameRuleDesignPopup.instantiateIcon(d.displayIcon);
	}
	//return the rightmost x and max height of the icons for this component
	public Vector2 getDisplaySize() {
		if (subComponents != null) {
			Vector2 result = componentDisplayed == null ? new Vector2() : componentDisplayed.sizeDelta;
			for (int i = 0; i < subComponents.Length; i++) {
				Vector2 componentDisplaySize = subComponents[i].getDisplaySize();
				result.x += componentDisplaySize.x;
				result.y = Mathf.Max(result.y, componentDisplaySize.y);
			}
			return result;
		} else
			return componentDisplayed.sizeDelta;
	}
	//receives the left x to use for icons and returns the right x of the icons placed
	public float relocateIcons(float leftX) {
		//if componentDisplayed is null (like for GameRuleEffectAction), then it will have subcomponents and displayIconIndex will be -1
		if (subComponents != null) {
			int displayIconIndex = descriptor.displayIconIndex;
			for (int i = 0; i < subComponents.Length; i++) {
				if (i == displayIconIndex) {
					componentDisplayed.anchoredPosition = new Vector2(leftX, 0);
					leftX += componentDisplayed.sizeDelta.x;
				}
				leftX = subComponents[i].relocateIcons(leftX);
			}
			return leftX;
		} else {
			componentDisplayed.anchoredPosition = new Vector2(leftX, 0);
			return leftX + componentDisplayed.sizeDelta.x;
		}
	}

	//converting components into an actual rule string
	public string getGameRuleString() {
		GameRule rule = new GameRule(subComponents[0].getCondition(), subComponents[1].getAction());
		string name = GameRuleSerializer.packRuleToString(rule);
		Debug.Log("If " + rule.condition.ToString() + " => Then " + rule.action.ToString() + " - " + name);
		return name;
	}
	public GameRuleCondition getCondition() {
		System.Type conditionType = ((GameRuleDesignComponentClassDescriptor)descriptor).typeDescribed;
		if (conditionType == typeof(GameRuleComparisonCondition))
			throw new System.Exception("We haven't implemented comparison conditions yet!");
		else if (conditionType == typeof(GameRuleEventHappenedCondition)) {
			System.Type st = ((GameRuleDesignComponentEventParticipantDescriptor)(subComponents[0].subComponents[0].descriptor)).typeDescribed;
			System.Type tt;
			string p;
			GameRuleEventType et = subComponents[0].getEventTypeAndTarget(out tt, out p, 1);
			return new GameRuleEventHappenedCondition(et, st, tt, p);
		} else if (conditionType == typeof(GameRuleZoneCondition))
			return new GameRuleZoneCondition(
				((GameRuleDesignComponentZoneTypeDescriptor)(subComponents[1].descriptor)).zoneTypeDescribed,
				(GameRuleSourceSelector)(((GameRuleDesignComponentSelectorDescriptor)(subComponents[0].descriptor)).selectorDescribed)
			);
		else
			throw new System.Exception("Bug: invalid design serialization condition type " + conditionType);
	}
	public GameRuleEventType getEventTypeAndTarget(out System.Type tt, out string p, int indexOfTarget) {
		GameRuleDesignComponentDescriptor targetDescriptor = subComponents[indexOfTarget].descriptor;
		if (targetDescriptor is GameRuleDesignComponentEventParticipantDescriptor) {
			tt = ((GameRuleDesignComponentEventParticipantDescriptor)(subComponents[indexOfTarget].descriptor)).typeDescribed;
			p = null;
		} else if (targetDescriptor is GameRuleDesignComponentFieldObjectDescriptor) {
			tt = typeof(FieldObject);
			p = ((GameRuleDesignComponentFieldObjectDescriptor)(subComponents[indexOfTarget].descriptor)).fieldObjectDescribed;
		} else
			throw new System.Exception("Bug: invalid design serialization event target descriptor " + targetDescriptor);
		return ((GameRuleDesignComponentEventTypeDescriptor)descriptor).eventTypeDescribed;
	}
	public GameRuleAction getAction() {
		System.Type actionType = ((GameRuleDesignComponentClassDescriptor)descriptor).typeDescribed;
		if (actionType == typeof(GameRuleEffectAction)) {
			return new GameRuleEffectAction(
				((GameRuleDesignComponentSelectorDescriptor)(subComponents[0].descriptor)).selectorDescribed,
				subComponents[1].getEffect()
			);
		} else if (actionType == typeof(GameRuleMetaRuleAction))
			return new GameRuleMetaRuleAction(((GameRuleDesignComponentMetaRuleDescriptor)(subComponents[0].descriptor)).metaRuleDescribed);
		else
			throw new System.Exception("Bug: invalid design serialization action type " + actionType);
	}
	public GameRuleEffect getEffect() {
		System.Type effectType = ((GameRuleDesignComponentClassDescriptor)descriptor).typeDescribed;
		if (effectType == typeof(GameRulePointsPlayerEffect))
			return new GameRulePointsPlayerEffect(((GameRuleDesignComponentIntDescriptor)(subComponents[0].descriptor)).intDescribed);
		else if (effectType == typeof(GameRuleFreezeEffect))
			return new GameRuleFreezeEffect(subComponents[0].getDuration());
		else if (effectType == typeof(GameRuleDuplicateEffect))
			return new GameRuleDuplicateEffect();
		else if (effectType == typeof(GameRuleDizzyEffect))
			return new GameRuleDizzyEffect(subComponents[0].getDuration());
		else if (effectType == typeof(GameRuleBounceEffect))
			return new GameRuleBounceEffect(subComponents[0].getDuration());
		else
			throw new System.Exception("Bug: invalid design serialization effect type " + effectType);
	}
	public GameRuleActionDuration getDuration() {
		System.Type durationType = ((GameRuleDesignComponentClassDescriptor)descriptor).typeDescribed;
		if (durationType == typeof(GameRuleActionFixedDuration))
			return new GameRuleActionFixedDuration(((GameRuleDesignComponentIntDescriptor)(subComponents[0].descriptor)).intDescribed);
		else if (durationType == typeof(GameRuleActionUntilConditionDuration)) {
			GameRuleSelector ts = ((GameRuleDesignComponentSelectorDescriptor)(subComponents[0].descriptor)).selectorDescribed;
			System.Type tt;
			string p;
			GameRuleEventType et = subComponents[0].subComponents[0].getEventTypeAndTarget(out tt, out p, 0);
			return new GameRuleActionUntilConditionDuration(ts, new GameRuleEventHappenedCondition(et, ts.targetType(), tt, p));
		} else
			throw new System.Exception("Bug: invalid design serialization duration type " + durationType);
	}
}
