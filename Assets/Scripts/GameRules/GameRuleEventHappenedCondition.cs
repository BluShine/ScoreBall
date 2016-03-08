using UnityEngine;
using System.Collections.Generic;

//GameRuleCondition class definition in GameRuleComparisonCondition.cs

////////////////Game events////////////////
public enum GameRuleEventType : int {
	Kick,
	Grab,
	Bump,
	Smack
}

public class GameRuleEvent {
	//the list of all possible event types for a given event source
	public static Dictionary<System.Type, List<GameRuleEventType>> potentialEventTypesMap;
	//quick access to the keys lists of potentialEventsList
	public static List<GameRuleEventType> eventTypesList;
	public static Dictionary<GameRuleEventType, List<System.Type>> eventTypeSourcesList;
	//events with their possible sources and the sources' lists of possible targets
	public static Dictionary<GameRuleEventType, Dictionary<System.Type, List<System.Type>>> potentialEventsList = buildPotentialEventsList();
	public static Dictionary<GameRuleEventType, Dictionary<System.Type, List<System.Type>>> buildPotentialEventsList() {
		Dictionary<GameRuleEventType, Dictionary<System.Type, List<System.Type>>> values =
			new Dictionary<GameRuleEventType, Dictionary<System.Type, List<System.Type>>>();

		Dictionary<System.Type, List<System.Type>> kickSourcesAndTargets =
			(values[GameRuleEventType.Kick] = new Dictionary<System.Type, List<System.Type>>());
		//grab has the same source and target as kick
		values[GameRuleEventType.Grab] = kickSourcesAndTargets;
		Dictionary<System.Type, List<System.Type>> bumpSourcesAndTargets =
			(values[GameRuleEventType.Bump] = new Dictionary<System.Type, List<System.Type>>());
		Dictionary<System.Type, List<System.Type>> smackSourcesAndTargets =
			(values[GameRuleEventType.Smack] = new Dictionary<System.Type, List<System.Type>>());

		kickSourcesAndTargets[typeof(TeamPlayer)] = new List<System.Type>(new System.Type[] {typeof(Ball)});
		bumpSourcesAndTargets[typeof(TeamPlayer)] = new List<System.Type>(new System.Type[] {typeof(TeamPlayer), typeof(FieldObject)});
		bumpSourcesAndTargets[typeof(Ball)] = new List<System.Type>(new System.Type[] {typeof(Ball), typeof(FieldObject)});
		smackSourcesAndTargets[typeof(TeamPlayer)] = new List<System.Type>(new System.Type[] {typeof(TeamPlayer)});

		//for convenience we save the keys lists
		//we also need to build the potential event types map
		eventTypesList = new List<GameRuleEventType>();
		eventTypeSourcesList = new Dictionary<GameRuleEventType, List<System.Type>>();
		potentialEventTypesMap = new Dictionary<System.Type, List<GameRuleEventType>>();
		foreach (KeyValuePair<GameRuleEventType, Dictionary<System.Type, List<System.Type>>> eventTypeWithSources in values) {
			GameRuleEventType eventType = eventTypeWithSources.Key;
			eventTypesList.Add(eventType);
			List<System.Type> sources = (eventTypeSourcesList[eventType] = new List<System.Type>());
			foreach (System.Type sourceType in eventTypeWithSources.Value.Keys) {
				sources.Add(sourceType);

				//add this event type to the list of potential events for the source
				List<GameRuleEventType> potentialEventTypes;
				if (potentialEventTypesMap.TryGetValue(sourceType, out potentialEventTypes)) {
					if (!potentialEventTypes.Contains(eventType))
						potentialEventTypesMap[sourceType].Add(eventType);
				} else
					(potentialEventTypesMap[sourceType] = new List<GameRuleEventType>()).Add(eventType);
			}
		}

		return values;
	}

	public SportsObject source;
	public FieldObject target;
	public GameRuleEventType eventType;
	public string param;
	public GameRuleEvent(GameRuleEventType et, SportsObject s, FieldObject t) {
		source = s;
		target = t;
		eventType = et;
		if (et == GameRuleEventType.Bump && t.GetType() == typeof(FieldObject))
			param = t.sportName;
		else
			param = null;
	}
	public static string getEventText(GameRuleEventType et) {
		switch (et) {
			case GameRuleEventType.Kick:
				return " kicks ";
			case GameRuleEventType.Grab:
				return " grabs ";
			case GameRuleEventType.Bump:
				return " bumps into ";
			case GameRuleEventType.Smack:
				return " tackles ";
			default:
				throw new System.Exception("Bug: tried to get event text for " + et);
		}
	}
	public static GameObject getEventIcon(GameRuleEventType et) {
		switch (et) {
			case GameRuleEventType.Kick:
				return GameRuleIconStorage.instance.kickIcon;
			case GameRuleEventType.Grab:
				return GameRuleIconStorage.instance.grabIcon;
			case GameRuleEventType.Bump:
				return GameRuleIconStorage.instance.bumpIcon;
			case GameRuleEventType.Smack:
				return GameRuleIconStorage.instance.smackIcon;
			default:
				throw new System.Exception("Bug: tried to get event icon for " + et);
		}
	}
}

////////////////Conditions that trigger actions when an event happen////////////////
public class GameRuleEventHappenedCondition : GameRuleCondition {
	public GameRuleEventType eventType;
	public System.Type sourceType;
	public System.Type targetType;
	public string param;
	public GameRuleRequiredObject requiredObjectForSource;
	public GameRuleRequiredObject requiredObjectForTarget;
	public GameRuleEventHappenedCondition(GameRuleEventType et, System.Type st, System.Type tt, string p) {
		eventType = et;
		param = p;
		sourceType = st;
		targetType = tt;

		//any event originating from a ball needs a ball
		//ideally it doesn't need to be holdable, but at the moment for practical reasons it does
		bool sourceIsBall = GameRules.derivesFrom(sourceType, typeof(Ball));
		if (sourceIsBall)
			requiredObjectForSource = new GameRuleRequiredObject(GameRuleRequiredObjectType.HoldableBall, null);
		//our event type doesn't have a required object as its source
		else
			requiredObjectForSource = null;

		//ball-ball collision needs a second ball
		if (sourceIsBall && GameRules.derivesFrom(targetType, typeof(Ball)))
			requiredObjectForTarget = new GameRuleRequiredObject(GameRuleRequiredObjectType.SecondBall, null);
		//any event that involves a player holding a ball requires a holdable ball
		else if (eventType == GameRuleEventType.Kick ||
			eventType == GameRuleEventType.Grab)
			requiredObjectForSource = new GameRuleRequiredObject(GameRuleRequiredObjectType.HoldableBall, null);
		//any field object collision that isn't with a boundary is with a goal
		else if (targetType == typeof(FieldObject) && param != "boundary")
			requiredObjectForTarget = new GameRuleRequiredObject(GameRuleRequiredObjectType.SpecificGoal, param);
		//our event type doesn't need have a required object as its target
		else
			requiredObjectForTarget = null;
	}
	public override bool eventHappened(GameRuleEvent gre) {
		//make sure that we have the right event
		if (gre.eventType != eventType)
			return false;

		//make sure the types match up
		System.Type eventTargetType = gre.target.GetType();
		if (gre.source.GetType() != sourceType || eventTargetType != targetType)
			return false;

		//field objects have a few more things we need to check
		if (targetType == typeof(FieldObject)) {
			//make sure it's the right field object
			if (param != gre.target.sportName)
				return false;

			//collisions don't happen between players/balls and field objects on the players' teams
			if (gre.source is TeamPlayer && gre.source.team == gre.target.team)
				return false;
			else if (gre.source is Ball) {
				TeamPlayer ballPlayer = ((Ball)(gre.source)).currentPlayer;
				if (ballPlayer != null && ballPlayer.team == gre.target.team)
					return false;
			}
		}

		//all types match up and there are no other disqualifications, the condition happened
		return true;
	}
	public override string ToString() {
		return ToString(GameRuleSourceSelector.selectorIdentifier(sourceType, false));
	}
	//an until-condition will provide its own selector for the source
	public string ToString(GameRuleSelector sourceSelector) {
		return sourceSelector.ToString() + GameRuleEvent.getEventText(eventType) +
			(targetType == typeof(FieldObject) ? param : GameRuleSourceSelector.selectorIdentifier(targetType, sourceSelector).ToString());
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		if (requiredObjectForSource != null)
			requiredObjectsList.Add(requiredObjectForSource);
		if (requiredObjectForTarget != null)
			requiredObjectsList.Add(requiredObjectForTarget);
	}
	public override void addIcons(List<GameObject> iconList) {
		addIcons(iconList, GameRuleSourceSelector.selectorIdentifier(sourceType, false));
	}
	public void addIcons(List<GameObject> iconList, GameRuleSelector sourceSelector) {
		sourceSelector.addIcons(iconList);
		iconList.Add(GameRuleEvent.getEventIcon(eventType));
		if (targetType == typeof(FieldObject))
			addFieldObjectIcon(param, iconList);
		else
			GameRuleSourceSelector.selectorIdentifier(targetType, sourceSelector).addIcons(iconList);
	}
	public static void addFieldObjectIcon(string p, List<GameObject> iconList) {
		if (p == "boundary")
			iconList.Add(GameRuleIconStorage.instance.boundaryIcon);
		else
			iconList.Add(GameRuleSpawnableObjectRegistry.instance.findGoalObject(p).icon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		//pack the condition type
		serializer.packByte(GAME_RULE_CONDITION_BIT_SIZE, GAME_RULE_EVENT_HAPPENED_CONDITION_BYTE_VAL);
		packToStringAsEventHappenedCondition(serializer);
	}
	//until-condition durations know that their condition is an event, so we don't need to save the condition type
	public void packToStringAsEventHappenedCondition(GameRuleSerializer serializer) {
		//pack the event type
		serializer.packToString(eventType, GameRuleEvent.eventTypesList);
		//pack our source and target types
		serializer.packToString(sourceType, GameRuleEvent.eventTypeSourcesList[eventType]);
		serializer.packToString(targetType, GameRuleEvent.potentialEventsList[eventType][sourceType]);
		//pack the param if applicable
		if (targetType == typeof(FieldObject))
			serializer.packToString(param, FieldObject.standardFieldObjects);
	}
	public static new GameRuleEventHappenedCondition unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleEventType et = deserializer.unpackFromString(GameRuleEvent.eventTypesList);
		System.Type st = deserializer.unpackFromString(GameRuleEvent.eventTypeSourcesList[et]);
		System.Type tt = deserializer.unpackFromString(GameRuleEvent.potentialEventsList[et][st]);
		string p = (tt == typeof(FieldObject)) ? deserializer.unpackFromString(FieldObject.standardFieldObjects) : null;
		return new GameRuleEventHappenedCondition(et, st, tt, p);
	}
}
