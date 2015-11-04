using UnityEngine;

public class GameRuleGenerator {
	public static GameRule GenerateNewRule(GameObject display) {
		GameRuleCondition condition = randomCondition();
		bool isComparison = condition is GameRuleComparisonCondition;
		bool playerCondition = true;
		if (!isComparison && ((GameRuleEventHappenedCondition)(condition)).eventType >= GameRuleEventType.BallEventTypeStart)
			playerCondition = false;
		return new GameRule(condition, randomAction(isComparison, playerCondition), display);
	}
	public static GameRuleCondition randomCondition() {
		return /*Random.Range(0, 2) == 0 ? randomComparisonCondition() :*/ randomEventHappenedCondition();
	}
	public static GameRuleComparisonCondition randomComparisonCondition() {
		//only player-value comparison conditions for now
		return new GameRulePlayerValueComparisonCondition(randomPlayerValue(), randomConditionOperator(), Random.Range(0, 2) == 0 ? randomValue() : randomPlayerValue());
	}
	public static GameRulePlayerValue randomPlayerValue() {
		return new GameRulePlayerScoreValue();
	}
	public static GameRuleConditionOperator randomConditionOperator() {
		int rand = Random.Range(0, 6);
		if (rand == 0)
			return GameRuleConditionOperator.lessThanOperator;
		else if (rand == 1)
			return GameRuleConditionOperator.greaterThanOperator;
		else if (rand == 2)
			return GameRuleConditionOperator.lessOrEqualOperator;
		else if (rand == 3)
			return GameRuleConditionOperator.greaterOrEqualOperator;
		else if (rand == 4)
			return GameRuleConditionOperator.intEqualOperator;
		else
			return GameRuleConditionOperator.intNotEqualOperator;
	}
	public static GameRuleValue randomValue() {
		return new GameRuleIntConstantValue(Random.Range(-100, 101));
	}
	public static GameRuleEventHappenedCondition randomEventHappenedCondition() {
		//2/3 player, 1/3 ball
		if (Random.Range(0, 3) < 2)
			return randomPlayerEventHappenedCondition(GameRulePlayerSelector.instance/*, false*/);
		else
			return randomBallEventHappenedCondition(GameRuleBallSelector.instance);
	}
	public static GameRuleEventHappenedCondition randomPlayerEventHappenedCondition(GameRuleSelector selector/*, bool thirdperson*/) {
		int rand = Random.Range(0, 9);
		GameRuleEventType eventType;
		string param = null;
		if (rand == 0)
			eventType = GameRuleEventType.PlayerShootBall;
		else if (rand == 1)
			eventType = GameRuleEventType.PlayerGrabBall;
		else if (rand == 2)
			eventType = GameRuleEventType.PlayerTacklePlayer;
		else if (rand == 3)
			eventType = GameRuleEventType.PlayerHitPlayer;
//		else if (rand == 4)
//			eventType = GameRuleEventType.PlayerHitSportsObject;
		else if (rand == 4) {
			eventType = GameRuleEventType.PlayerHitFieldObject;
			param = "wall";
		} else if (rand == 5) {
			eventType = GameRuleEventType.PlayerHitFieldObject;
			param = "goal";
		} else if (rand == 6)
			eventType = GameRuleEventType.PlayerStealBall;
		else if (rand == 7)
			eventType = GameRuleEventType.PlayerHitInTheFaceByBall;
		else
			eventType = GameRuleEventType.PlayerTouchBall;
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	public static GameRuleEventHappenedCondition randomBallEventHappenedCondition(GameRuleSelector selector) {
		int rand = Random.Range(0, 3);
		GameRuleEventType eventType;
		string param = null;
		if (rand == 0)
/*			eventType = GameRuleEventType.BallHitSportsObject;
		else if (rand == 1)*/ {
			eventType = GameRuleEventType.BallHitFieldObject;
			param = "wall";
		} else if (rand == 1) {
			eventType = GameRuleEventType.BallHitFieldObject;
			param = "goal";
		} else
			eventType = GameRuleEventType.BallHitBall;
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	public static GameRuleAction randomAction(bool isComparison, bool playerCondition) {
		GameRuleSelector selector = playerCondition ?
			randomPlayerSourceSelector() :
			randomBallSourceSelector();
		return new GameRuleAction(selector, randomActionActionForTarget(selector, playerCondition));
	}
	public static GameRuleSelector randomPlayerSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRulePlayerSelector.instance;
		else
			return GameRuleOpponentSelector.instance;
	}
	public static GameRuleSelector randomBallSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRuleBallSelector.instance;
		else
			return GameRuleBallShooterSelector.instance;
	}
	public static GameRuleActionAction randomActionActionForTarget(GameRuleSelector selector, bool playerCondition) {
		if (selector.targetType() == typeof(Ball))
			return randomBallActionAction();
		else
			return randomPlayerActionAction(false/*, playerCondition*/);
	}
	public static GameRuleActionAction randomPlayerActionAction(bool isComparison/*, bool playerCondition*/) {
		isComparison = false;
		int rand = Random.Range(0, 4);
		if (rand == 0) {
			int points = Random.Range(-5, 5);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		}
		else if (rand == 1)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (rand == 2)
			return new GameRuleDuplicateActionAction();
		else
			return new GameRuleFreezeUntilConditionActionAction(
				randomPlayerEventHappenedCondition(
					randomPlayerSourceSelector()));
	}
	public static GameRuleActionAction randomBallActionAction() {
		int rand = Random.Range(0, 2);
		if (rand == 0)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else
			return new GameRuleDuplicateActionAction();
	}
}
