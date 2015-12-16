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
	PlayerHitInTheFaceByBall,
	PlayerTouchBall,
	PlayerEventTypeEnd,

	BallEventTypeStart,
	BallHitSportsObject,
	BallHitFieldObject,
	BallHitBall,
	BallEventTypeEnd
}

public class GameRuleEvent {
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
			case GameRuleEventType.PlayerHitInTheFaceByBall:
				return (thirdperson ? "gets" : "get") + " smacked by the ball";
			case GameRuleEventType.PlayerTouchBall:
				return (thirdperson ? "touches" : "touch") + " the ball";
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
			eventType == GameRuleEventType.PlayerStealBall ||
			eventType == GameRuleEventType.PlayerHitInTheFaceByBall ||
			eventType == GameRuleEventType.PlayerTouchBall)
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
}
