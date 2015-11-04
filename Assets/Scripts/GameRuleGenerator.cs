using UnityEngine;

public class GameRuleGenerator {
	public static GameRule GenerateNewRule(GameObject display) {
		GameRuleCondition condition = randomCondition();
		bool isComparison = condition is GameRuleComparisonCondition;
		bool playerAction = true;
		if (!isComparison && ((GameRuleEventHappenedCondition)(condition)).eventType >= GameRuleEventType.BallEventTypeStart)
			playerAction = false;
		return new GameRule(condition, randomAction(isComparison, playerAction), display);
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
			return randomPlayerEventHappenedCondition(GameRulePlayerSelector.instance);
		else
			return randomBallEventHappenedCondition(GameRuleBallSelector.instance);
	}
	public static GameRuleEventHappenedCondition randomPlayerEventHappenedCondition(GameRuleSelector selector) {
		int rand = Random.Range(0, 9);
		if (rand == 0)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerShootBall, selector, " shoot the ball");
		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerGrabBall, selector, " grab the ball");
		else if (rand == 2)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTacklePlayer, selector, " tackle your opponent");
		else if (rand == 3)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitPlayer, selector, " bump into your opponent");
		//		else if (rand == 4)
		//			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitSportsObject, selector, " bump into ????");
		else if (rand == 4)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, selector, " hit a ", "wall");
		else if (rand == 5)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, selector, " hit a ", "goal");
		else if (rand == 6)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerStealBall, selector, " steal the ball");
		else if (rand == 7)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitInTheFaceByBall, selector, " get smacked by the ball");
		else
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTouchBall, selector, " touch the ball");
	}
	public static GameRuleEventHappenedCondition randomBallEventHappenedCondition(GameRuleSelector selector) {
		int rand = Random.Range(0, 3);
		if (rand == 0)
			//			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitSportsObject, selector, " bumps into ????");
			//		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, selector, " hits a ", "wall");
		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, selector, " hits a ", "goal");
		else
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitBall, selector, " bumps into another ball");
	}
	public static GameRuleAction randomAction(bool isComparison, bool playerAction) {
		GameRuleSelector selector = playerAction ?
			randomPlayerSourceSelector() :
			randomBallSourceSelector();
		return new GameRuleAction(selector, randomActionActionForTarget(selector));
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
	public static GameRuleActionAction randomActionActionForTarget(GameRuleSelector selector) {
		if (selector.targetType() == typeof(Ball))
			return randomBallActionAction();
		else
			return randomPlayerActionAction(false);
	}
	public static GameRuleActionAction randomPlayerActionAction(bool isComparison) {
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
