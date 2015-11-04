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
}

////////////////Conditions that trigger actions when an event happen////////////////
public class GameRuleEventHappenedCondition : GameRuleCondition {
	public GameRuleEventType eventType;
	public string conditionString;
	public string param;
	public GameRuleSelector selector;
	public GameRuleEventHappenedCondition(GameRuleEventType et, GameRuleSelector grs, string cs, string p = null) {
		eventType = et;
		conditionString = cs;
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
		return selector.ToString() + conditionString + param;
	}
}
