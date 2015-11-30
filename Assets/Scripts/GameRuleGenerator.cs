using UnityEngine;
using System.Collections.Generic;

//this is for any restrictions in game rule generation
public enum GameRuleRestriction {
	NeedsPointsAction,
	NeedsEventHappenedCondition,
	NeedsPlayerTargetSelector,
	OnlyPositivePointAmounts
};

public class GameRuleGenerator {
	private static List<GameRuleRestriction> restrictions = new List<GameRuleRestriction>();
	private static GameRuleCondition condition;
	private static List<GameRuleEventType> playerEventTypesList = new List<GameRuleEventType>();
	private static List<GameRuleEventType> ballEventTypesList = new List<GameRuleEventType>();
	private static List<GameRuleEventType> eventTypesList = buildEventTypesList();
	private static List<GameRuleEventType> buildEventTypesList() {
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

	//generate a completely new random rule
	public static GameRule GenerateNewRule(GameObject display) {
		populateInitialRestrictions();
		condition = randomCondition();
		bool isComparison = condition is GameRuleComparisonCondition;
		bool ballCondition = false;
		if (!isComparison && ((GameRuleEventHappenedCondition)(condition)).eventType >= GameRuleEventType.BallEventTypeStart)
			ballCondition = true;
		return new GameRule(condition, randomAction(isComparison, ballCondition), display);
	}
	//add any restrictions that can be determined before any generation has happened
	public static void populateInitialRestrictions() {
		//reset the restrictions
		restrictions.Clear();

		//if there is no rule that gives points, that is the top priority
		bool needsPointsRule = true;
		foreach (GameRule gameRule in GameRules.instance.rulesList) {
			GameRuleActionAction innerAction = gameRule.action.innerAction;
			if (innerAction is GameRulePointsPlayerActionAction &&
				((GameRulePointsPlayerActionAction)(innerAction)).pointsGiven > 0) {
				needsPointsRule = false;
				break;
			}
		}
		if (needsPointsRule) {
			restrictions.Add(GameRuleRestriction.NeedsEventHappenedCondition);
			restrictions.Add(GameRuleRestriction.NeedsPlayerTargetSelector);
			restrictions.Add(GameRuleRestriction.NeedsPointsAction);
			restrictions.Add(GameRuleRestriction.OnlyPositivePointAmounts);
		}
	}

	////////////////GameRuleCondition generation////////////////
	//generate a random rule condition
	public static GameRuleCondition randomCondition() {
		//restrictions require the condition to only happen on an event
		if (restrictions.Contains(GameRuleRestriction.NeedsEventHappenedCondition))
			return randomEventHappenedCondition();

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
		List<GameRuleEventType> acceptableEventTypes = new List<GameRuleEventType>(playerEventTypesList);
acceptableEventTypes.Remove(GameRuleEventType.PlayerHitSportsObject);
		GameRuleEventType eventType = acceptableEventTypes[Random.Range(0, acceptableEventTypes.Count)];
		string param = null;
		if (eventType == GameRuleEventType.PlayerHitFieldObject)
			param = randomFieldObjectType();
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	//generate a random EventHappenedCondition for a ball event source
	public static GameRuleEventHappenedCondition randomBallEventHappenedCondition(GameRuleSelector selector) {
		List<GameRuleEventType> acceptableEventTypes = new List<GameRuleEventType>(ballEventTypesList);
acceptableEventTypes.Remove(GameRuleEventType.BallHitSportsObject);
		GameRuleEventType eventType = acceptableEventTypes[Random.Range(0, acceptableEventTypes.Count)];
		string param = null;
		if (eventType == GameRuleEventType.BallHitFieldObject)
			param = randomFieldObjectType();
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	//generate a random field object type for a field object collision event
	public static string randomFieldObjectType() {
		int rand = Random.Range(0, 2);
		if (rand == 0)
			return "goal";
		else
			return "wall";
	}

	////////////////GameRuleAction generation////////////////
	//generate a random action based on the given condition information
	public static GameRuleAction randomAction(bool isComparison, bool ballCondition) {
		GameRuleSelector sourceToTarget = randomSelectorForSource(ballCondition);
		return new GameRuleAction(sourceToTarget, randomActionActionForTarget(sourceToTarget, ballCondition));
	}
	//generate a random action to happen to the target of the selector
	//pass along whether the original condition was a ball event-happened condition in case the action needs to know
	public static GameRuleActionAction randomActionActionForTarget(GameRuleSelector sourceToTarget, bool ballCondition) {
		if (sourceToTarget.targetType() == typeof(Ball))
			return randomBallActionAction(ballCondition);
		else
			return randomPlayerActionAction(false, ballCondition);
	}

	public static GameRuleActionAction randomPlayerActionAction(bool isComparison, bool ballCondition) {
isComparison = false;
		//build the list of acceptable action types, taking restrictions into account
		System.Type[] acceptableActionTypes;
		if (restrictions.Contains(GameRuleRestriction.NeedsPointsAction))
			acceptableActionTypes = new System.Type[] {typeof(GameRulePointsPlayerActionAction)};
		else
			acceptableActionTypes = new System.Type[] {
				typeof(GameRulePointsPlayerActionAction),
				typeof(GameRuleFreezeActionAction),
				typeof(GameRuleDuplicateActionAction),
				typeof(GameRuleFreezeUntilConditionActionAction)
			};

		//pick one of the action types
		System.Type chosenType = acceptableActionTypes[Random.Range(0, acceptableActionTypes.Length)];
		if (chosenType == typeof(GameRulePointsPlayerActionAction)) {
			int minPoints = restrictions.Contains(GameRuleRestriction.OnlyPositivePointAmounts) ? 0 : -5;
			int points = Random.Range(minPoints, 10);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (chosenType == typeof(GameRuleFreezeActionAction))
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (chosenType == typeof(GameRuleDuplicateActionAction))
			return new GameRuleDuplicateActionAction();
		else if (chosenType == typeof(GameRuleFreezeUntilConditionActionAction))
			return new GameRuleFreezeUntilConditionActionAction(
				randomEventHappenedConditionForTarget(
					randomSelectorForSource(ballCondition)));
		else
			throw new System.Exception("Bug: Invalid action type!");
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
	//generate a selector based on the source of the condition
	public static GameRuleSelector randomSelectorForSource(bool ballCondition) {
		if (ballCondition)
			return randomBallSourceSelector();
		else
			return randomPlayerSourceSelector();
	}
	//generate a selector based on a condition for events caused by players
	public static GameRuleSelector randomPlayerSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRulePlayerSelector.instance;
		else
			return GameRuleOpponentSelector.instance;
	}
	//generate a selector based on a condition for events caused by balls
	public static GameRuleSelector randomBallSourceSelector() {
		//build selectors list, taking restrictions into account
		GameRuleSelector[] acceptableSelectors;
		if (restrictions.Contains(GameRuleRestriction.NeedsPlayerTargetSelector))
			acceptableSelectors = new GameRuleSelector[] {
				GameRuleBallShooterSelector.instance,
				GameRuleBallShooterOpponentSelector.instance
			};
		else
			acceptableSelectors = new GameRuleSelector[] {
				GameRuleBallSelector.instance,
				GameRuleBallShooterSelector.instance,
				GameRuleBallShooterOpponentSelector.instance
			};


		//pick a selector
		return acceptableSelectors[Random.Range(0, acceptableSelectors.Length)];
	}
}
