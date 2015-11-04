//GameRuleCondition class definition in GameRuleComparisonCondition.cs

////////////////Game events////////////////
public enum GameRuleEventType : int {
	PlayerEventTypeStart = 0,
	PlayerShootBall,
	PlayerGrabBall,
	PlayerTacklePlayer,
	PlayerHitPlayer,
	PlayerHitSportsObject,
	PlayerHitFieldObject,
	PlayerStealBall,
	PlayerHitInTheFaceByBall,
	PlayerTouchBall,

	BallEventTypeStart = 1000,
	BallHitSportsObject,
	BallHitFieldObject,
	BallHitBall
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
	public GameRuleSelector selector;
	public GameRuleEventHappenedCondition(GameRuleEventType et, GameRuleSelector grs, string p) {
		eventType = et;
		param = p;
		selector = grs;
	}
	public override bool conditionHappened(GameRuleEvent gre) {
		return gre.eventType == eventType && gre.param == param;
	}
	public bool conditionHappened(GameRuleEvent gre, SportsObject target) {
		return conditionHappened(gre) && selector.target(target) == gre.getEventSource();
	}
	public override string ToString() {
		return selector.ToString() + " " + GameRuleEvent.getEventText(eventType, false) + param;
	}
}
