using UnityEngine;

public class GameRuleGenerator {
	public static GameRule GenerateNewRule(GameObject display) {
		GameRuleCondition condition = randomCondition();
		bool isComparison = condition is GameRuleComparisonCondition;
		bool ballCondition = false;
		if (!isComparison && ((GameRuleEventHappenedCondition)(condition)).eventType >= GameRuleEventType.BallEventTypeStart)
			ballCondition = true;
		return new GameRule(condition, randomAction(isComparison, ballCondition), display);
	}
	public static GameRuleCondition randomCondition() {
		return /*Random.Range(0, 2) == 0 ? randomComparisonCondition() :*/ randomEventHappenedCondition();
	}

	////////////////GameRuleComparisonCondition generation////////////////
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

	////////////////GameRuleEventHappenedCondition generation////////////////
	//generate a random EventHappenedCondition for a rule
	public static GameRuleEventHappenedCondition randomEventHappenedCondition() {
		//2/3 player, 1/3 ball
		if (Random.Range(0, 3) < 2)
			return randomPlayerEventHappenedCondition(GameRulePlayerSelector.instance);
		else
			return randomBallEventHappenedCondition(GameRuleBallSelector.instance);
	}
	//generate a random EventHappenedCondition for an until-condition action
	//the given selector selects the object that triggers the end of the until-condition action
	public static GameRuleEventHappenedCondition randomEventHappenedConditionForTarget(GameRuleSelector sourceToTrigger) {
		if (sourceToTrigger.targetType() == typeof(Ball))
			return randomBallEventHappenedCondition(sourceToTrigger);
		else
			return randomPlayerEventHappenedCondition(sourceToTrigger);
	}
	//generate a random EventHappenedCondition for a player event source
	public static GameRuleEventHappenedCondition randomPlayerEventHappenedCondition(GameRuleSelector selector) {
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

	////////////////GameRuleAction generation////////////////
	public static GameRuleAction randomAction(bool isComparison, bool ballCondition) {
		GameRuleSelector sourceToTarget = randomSelectorForSource(ballCondition);
		return new GameRuleAction(sourceToTarget, randomActionActionForTarget(sourceToTarget, ballCondition));
	}
	public static GameRuleActionAction randomActionActionForTarget(GameRuleSelector sourceToTarget, bool ballCondition) {
		if (sourceToTarget.targetType() == typeof(Ball))
			return randomBallActionAction(ballCondition);
		else
			return randomPlayerActionAction(false, ballCondition);
	}
	public static GameRuleActionAction randomPlayerActionAction(bool isComparison, bool ballCondition) {
isComparison = false;
		int rand = Random.Range(0, 4);
		if (rand == 0) {
			int points = Random.Range(-5, 10);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (rand == 1)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (rand == 2)
			return new GameRuleDuplicateActionAction();
		else
			return new GameRuleFreezeUntilConditionActionAction(
				randomEventHappenedConditionForTarget(
					randomSelectorForSource(ballCondition)));
	}
	public static GameRuleActionAction randomBallActionAction(bool ballCondition) {
		int rand = Random.Range(0, 3);
		if (rand == 0)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (rand == 1)
			return new GameRuleDuplicateActionAction();
		else {
			return new GameRuleFreezeUntilConditionActionAction(
				randomEventHappenedConditionForTarget(
					randomSelectorForSource(ballCondition)));
		}
	}

	////////////////GameRuleSelectors for actions and conditions////////////////
	public static GameRuleSelector randomSelectorForSource(bool ballCondition) {
		if (ballCondition)
			return randomBallSourceSelector();
		else
			return randomPlayerSourceSelector();
	}
	public static GameRuleSelector randomPlayerSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRulePlayerSelector.instance;
		else
			return GameRuleOpponentSelector.instance;
	}
	public static GameRuleSelector randomBallSourceSelector() {
		int rand = Random.Range(0, 2);
		if (rand == 0)
			return GameRuleBallSelector.instance;
		else if (rand == 1)
			return GameRuleBallShooterSelector.instance;
		else
			return GameRuleBallShooterOpponentSelector.instance;
	}
}
