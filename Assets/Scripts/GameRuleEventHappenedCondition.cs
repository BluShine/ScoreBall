using System.Collections.Generic;

//GameRuleCondition class definition in GameRuleComparisonCondition.cs

////////////////Game events////////////////
public enum GameRuleEventType : int {
	NoEventType = -1,

	PlayerEventTypeStart,
	PlayerShootBall,
	PlayerGrabBall,
	PlayerTacklePlayer,
	PlayerHitPlayer,
	PlayerHitSportsObject,
	PlayerHitFieldObject,
	PlayerStealBall,
	PlayerEventTypeEnd,

	BallEventTypeStart,
	BallHitSportsObject,
	BallHitFieldObject,
	BallHitBall,
	BallEventTypeEnd
}

public class GameRuleEvent {
	public static List<GameRuleEventType> playerEventTypesList = new List<GameRuleEventType>();
	public static List<GameRuleEventType> ballEventTypesList = new List<GameRuleEventType>();
	public static List<GameRuleEventType> eventTypesList = buildEventTypesList();
	public static List<GameRuleEventType> buildEventTypesList() {
		List<GameRuleEventType> values = new List<GameRuleEventType>();
		foreach (GameRuleEventType eventType in System.Enum.GetValues(typeof(GameRuleEventType))) {
			if (eventType > GameRuleEventType.PlayerEventTypeStart && eventType < GameRuleEventType.PlayerEventTypeEnd) {
				playerEventTypesList.Add(eventType);
				values.Add(eventType);
			} else if (eventType > GameRuleEventType.BallEventTypeStart && eventType < GameRuleEventType.BallEventTypeEnd) {
				ballEventTypesList.Add(eventType);
				values.Add(eventType);
			}
		}
		return values;
	}

	public TeamPlayer instigator;
	public TeamPlayer victim;
	public GameRuleEventType eventType;
	public Ball ball;
	public Ball secondaryBall;
	public SportsObject sportsObj;
	public FieldObject fieldObj;
	public string param;
	public GameRuleEvent(GameRuleEventType gret, TeamPlayer tp = null, TeamPlayer vct = null,
		Ball bl = null, Ball bl2 = null, SportsObject so = null, FieldObject fo = null) {
		eventType = gret;
		instigator = tp;
		victim = vct;
		ball = bl;
		secondaryBall = bl2;
		sportsObj = so;
		fieldObj = fo;
		if (gret == GameRuleEventType.PlayerHitFieldObject || gret == GameRuleEventType.BallHitFieldObject)
			param = fo.sportName;
		else
			param = null;
	}
	public SportsObject getEventSource() {
		if (eventType >= GameRuleEventType.BallEventTypeStart)
			return ball;
		else
			return instigator;
	}
	public static string getEventText(GameRuleEventType et, bool thirdperson) {
		switch (et) {
			case GameRuleEventType.PlayerShootBall:
				return (thirdperson ? "shoots" : "shoot") + " the ball";
			case GameRuleEventType.PlayerGrabBall:
				return (thirdperson ? "grabs" : "grab") + " the ball";
			case GameRuleEventType.PlayerTacklePlayer:
				return (thirdperson ? "tackles" : "tackle") + " your opponent";
			case GameRuleEventType.PlayerHitPlayer:
				return (thirdperson ? "bumps" : "bump") + " into your opponent";
//			case GameRuleEventType.PlayerHitSportsObject:
//				return (thirdperson ? "bumps" : "bump") + " into ????");
			case GameRuleEventType.PlayerHitFieldObject:
				return (thirdperson ? "hits" : "hit") + " a ";
			case GameRuleEventType.PlayerStealBall:
				return (thirdperson ? "steals" : "steal") + " the ball";
//			case GameRuleEventType.BallHitSportsObject:
//				return "bumps into ????";
			case GameRuleEventType.BallHitFieldObject:
				return "hits a ";
			case GameRuleEventType.BallHitBall:
				return "bumps into another ball";
			default:
				return "";
		}
	}
}

////////////////Conditions that trigger actions when an event happen////////////////
public class GameRuleEventHappenedCondition : GameRuleCondition {
	public GameRuleEventType eventType;
	public string param;
	public GameRuleSelector selector; // this is unused except for in text display and until-condition actions
	public GameRuleEventHappenedCondition(GameRuleEventType et, GameRuleSelector grs, string p) {
		eventType = et;
		param = p;
		selector = grs;
	}
	public override bool conditionHappened(GameRuleEvent gre) {
		//make sure that we have the right event
		if (gre.eventType != eventType || gre.param != param)
			return false;

		//collisions don't happen between players/balls and field objects on the players' teams
		if (gre.eventType == GameRuleEventType.BallHitFieldObject) {
			TeamPlayer ballPlayer = gre.ball.currentPlayer;
			if (ballPlayer != null && ballPlayer.team == gre.fieldObj.team)
				return false;
		} else if (gre.eventType == GameRuleEventType.PlayerHitFieldObject) {
			if (gre.instigator.team == gre.fieldObj.team)
				return false;
		}

		//right event and no other disqualifications, the collision happened
		return true;
	}
	public bool conditionHappened(GameRuleEvent gre, SportsObject triggerSource) {
		return conditionHappened(gre) && selector.target(triggerSource) == gre.getEventSource();
	}
	public override string ToString() {
		return selector.ToString() + " " + GameRuleEvent.getEventText(eventType, selector.conjugate == 1) + param;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		//any event involving a ball needs a ball
		if ((eventType > GameRuleEventType.BallEventTypeStart && eventType < GameRuleEventType.BallEventTypeEnd) ||
			eventType == GameRuleEventType.PlayerShootBall ||
			eventType == GameRuleEventType.PlayerGrabBall ||
			eventType == GameRuleEventType.PlayerStealBall)
			requiredObjectsList.Add(GameRuleRequiredObject.Ball);

		//ball-ball collision needs a second ball
		if (eventType == GameRuleEventType.BallHitBall)
			requiredObjectsList.Add(GameRuleRequiredObject.SecondBall);

		//if the field object is a goal we need a goal
		if (eventType == GameRuleEventType.PlayerHitFieldObject || eventType == GameRuleEventType.BallHitFieldObject) {
			if (param == "footgoal")
				requiredObjectsList.Add(GameRuleRequiredObject.FootGoal);
			else if (param == "goalposts")
				requiredObjectsList.Add(GameRuleRequiredObject.GoalPosts);
			else if (param == "backboardhoop")
				requiredObjectsList.Add(GameRuleRequiredObject.BackboardHoop);
			else if (param == "smallwall")
				requiredObjectsList.Add(GameRuleRequiredObject.SmallWall);
			else if (param == "fullgoalwall")
				requiredObjectsList.Add(GameRuleRequiredObject.FullGoalWall);
			else if (param != "boundary")
				throw new System.Exception("Bug: could not determine object required");
		}
	}
	public override void packToString(GameRuleSerializer serializer) {
		//pack the condition type
		serializer.packByte(GAME_RULE_CONDITION_BIT_SIZE, 1);
		//pack the event type
		serializer.packToString(eventType, GameRuleEvent.eventTypesList);
		//pack the param if applicable
		if (eventType == GameRuleEventType.PlayerHitFieldObject || eventType == GameRuleEventType.BallHitFieldObject)
			serializer.packToString(param, FieldObject.standardFieldObjects);
		//pack the selector type
		selector.packToString(serializer);
	}
	public static new GameRuleEventHappenedCondition unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleEventType et = deserializer.unpackFromString(GameRuleEvent.eventTypesList);
		string p = null;
		if (et == GameRuleEventType.PlayerHitFieldObject || et == GameRuleEventType.BallHitFieldObject)
			p = deserializer.unpackFromString(FieldObject.standardFieldObjects);
		GameRuleSelector s = GameRuleSelector.unpackFromString(deserializer);
		return new GameRuleEventHappenedCondition(et, s, p);
	}
}
