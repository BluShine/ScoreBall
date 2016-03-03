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

//certain events are only valid if caused/received by certain kinds of objects
//each of these represents a valid pairing of objects
public class GameRuleEventPotentialObjects {
	public System.Type source;
	public System.Type target;
	public GameRuleEventPotentialObjects(System.Type s, System.Type t) {
		source = s;
		target = t;
	}
}
public class GameRuleEvent {
	public static List<GameRuleEventType> eventTypesList = buildEventTypesList();
	public static List<GameRuleEventType> buildEventTypesList() {
		List<GameRuleEventType> values = new List<GameRuleEventType>();
		foreach (GameRuleEventType eventType in System.Enum.GetValues(typeof(GameRuleEventType)))
			values.Add(eventType);
		return values;
	}
	public static Dictionary<GameRuleEventType, List<System.Type>> potentialSourcesList;
	public static Dictionary<GameRuleEventType, List<System.Type>> potentialTargetsList;
	public static Dictionary<System.Type, List<GameRuleEventType>> potentialEventTypesList;
	public static Dictionary<GameRuleEventType, GameRuleEventPotentialObjects[]> potentialObjectsList = buildPotentialObjectsList();
	public static Dictionary<GameRuleEventType, GameRuleEventPotentialObjects[]> buildPotentialObjectsList() {
		Dictionary<GameRuleEventType, GameRuleEventPotentialObjects[]> values =
			new Dictionary<GameRuleEventType, GameRuleEventPotentialObjects[]>();
		GameRuleEventPotentialObjects[] playerBallPotentials =
			new GameRuleEventPotentialObjects[] {new GameRuleEventPotentialObjects(typeof(TeamPlayer), typeof(Ball))};
		GameRuleEventPotentialObjects playerPlayer = new GameRuleEventPotentialObjects(typeof(TeamPlayer), typeof(TeamPlayer));

		values[GameRuleEventType.Kick] = playerBallPotentials;
		values[GameRuleEventType.Grab] = playerBallPotentials;
		values[GameRuleEventType.Bump] = new GameRuleEventPotentialObjects[] {
			playerPlayer,
			new GameRuleEventPotentialObjects(typeof(Ball), typeof(Ball)),
			new GameRuleEventPotentialObjects(typeof(TeamPlayer), typeof(FieldObject)),
			new GameRuleEventPotentialObjects(typeof(Ball), typeof(FieldObject)),
		};
		values[GameRuleEventType.Smack] = new GameRuleEventPotentialObjects[] {playerPlayer};

		//before we leave, build the source and target lists per event type
		//also build the list of all potential event types per source
		potentialSourcesList = new Dictionary<GameRuleEventType, List<System.Type>>();
		potentialTargetsList = new Dictionary<GameRuleEventType, List<System.Type>>();
		potentialEventTypesList = new Dictionary<System.Type, List<GameRuleEventType>>();
		foreach (KeyValuePair<GameRuleEventType, GameRuleEventPotentialObjects[]> eventTypePotentialObjects in values) {
			GameRuleEventType eventType = eventTypePotentialObjects.Key;
			List<System.Type> potentialSources = (potentialSourcesList[eventType] = new List<System.Type>());
			List<System.Type> potentialTargets = (potentialTargetsList[eventType] = new List<System.Type>());
			foreach (GameRuleEventPotentialObjects potentialObject in eventTypePotentialObjects.Value) {
				//split the potential objects lists into source and target lists
				if (!potentialSources.Contains(potentialObject.source))
					potentialSources.Add(potentialObject.source);
				if (!potentialTargets.Contains(potentialObject.target))
					potentialTargets.Add(potentialObject.target);

				//add this event type to the list of potential events for the source
				if (!potentialEventTypesList.ContainsKey(potentialObject.source))
					(potentialEventTypesList[potentialObject.source] = new List<GameRuleEventType>()).Add(eventType);
				else {
					List<GameRuleEventType> potentialEventTypes = potentialEventTypesList[potentialObject.source];
					if (!potentialEventTypes.Contains(eventType))
						potentialEventTypesList[potentialObject.source].Add(eventType);
				}
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
		if (!GameRules.derivesFrom(gre.source.GetType(), sourceType) || !GameRules.derivesFrom(eventTargetType, targetType))
			return false;

		//field objects have a few more things we need to check
		if (eventTargetType == typeof(FieldObject)) {
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
		return ToString(GameRuleSourceSelector.stringIdentifier(sourceType));
	}
	//an until-condition will provide its own string for the source
	public string ToString(string sourceString) {
		return sourceString + GameRuleEvent.getEventText(eventType) +
			(targetType == typeof(FieldObject) ? param : GameRuleSourceSelector.stringIdentifier(targetType));
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		if (requiredObjectForSource != null)
			requiredObjectsList.Add(requiredObjectForSource);
		if (requiredObjectForTarget != null)
			requiredObjectsList.Add(requiredObjectForTarget);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleSourceSelector.iconIdentifier(sourceType));
		iconList.Add(GameRuleEvent.getEventIcon(eventType));
		if (targetType == typeof(FieldObject))
			addFieldObjectIcon(param, iconList);
		else
			iconList.Add(GameRuleSourceSelector.iconIdentifier(targetType));
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
		//pack the event type
		serializer.packToString(eventType, GameRuleEvent.eventTypesList);
		//pack our source and target types
		serializer.packToString(sourceType, GameRuleEvent.potentialSourcesList[eventType]);
		serializer.packToString(targetType, GameRuleEvent.potentialTargetsList[eventType]);
		//pack the param if applicable
		if (targetType == typeof(FieldObject))
			serializer.packToString(param, FieldObject.standardFieldObjects);
	}
	public static new GameRuleEventHappenedCondition unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleEventType et = deserializer.unpackFromString(GameRuleEvent.eventTypesList);
		System.Type st = deserializer.unpackFromString(GameRuleEvent.potentialSourcesList[et]);
		System.Type tt = deserializer.unpackFromString(GameRuleEvent.potentialTargetsList[et]);
		string p = (tt == typeof(FieldObject)) ? deserializer.unpackFromString(FieldObject.standardFieldObjects) : null;
		return new GameRuleEventHappenedCondition(et, st, tt, p);
	}
}
