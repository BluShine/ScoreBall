using UnityEngine;
using System.Collections.Generic;

//this is for any restrictions in game rule generation
public enum GameRuleRestriction {
	//restrict what options can get be choices at all
	OnlyPointsActions,
	OnlyEventHappenedConditions,
	OnlyPlayerTargetSelectors,
	OnlyPositivePointAmounts,
	OnlyPlayerBallInteractionEvents,
	OnlyBallFieldObjectInteractionEvents,

	//restrict what options can remain choices
	NoYouPlayerTargetSelectors,
	NoOpponentPlayerTargetSelectors,
	NoPlayerFreezeUntilConditions
};

public class GameRuleGenerator {
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

	private static List<GameRuleRestriction> restrictions = new List<GameRuleRestriction>();
	private static GameRuleCondition ruleCondition;
	private static GameRuleSelector ruleActionSelector;

	//generate a completely new random rule
	public static GameRule GenerateNewRule(GameObject display) {
		populateInitialRestrictions();
		ruleCondition = randomCondition();
		bool isComparison = ruleCondition is GameRuleComparisonCondition;
		bool ballCondition = false;
		if (!isComparison && ((GameRuleEventHappenedCondition)(ruleCondition)).eventType >= GameRuleEventType.BallEventTypeStart)
			ballCondition = true;
		return new GameRule(ruleCondition, randomAction(isComparison, ballCondition), display);
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
			restrictions.Add(GameRuleRestriction.OnlyEventHappenedConditions);
			restrictions.Add(GameRuleRestriction.OnlyPlayerTargetSelectors);
			restrictions.Add(GameRuleRestriction.OnlyPointsActions);
			restrictions.Add(GameRuleRestriction.OnlyPositivePointAmounts);
		}
	}
	//return whether the restriction is present and remove it if it is
	public static bool hasRestriction(GameRuleRestriction restriction) {
		if (restrictions.Contains(restriction)) {
			restrictions.RemoveAll((GameRuleRestriction otherRestriction) => (otherRestriction == restriction));
			return true;
		}
		return false;
	}

	////////////////GameRuleCondition generation////////////////
	//generate a random rule condition
	public static GameRuleCondition randomCondition() {
		//restrictions require the condition to only happen on an event
		if (hasRestriction(GameRuleRestriction.OnlyEventHappenedConditions))
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
		//build the list of acceptable event types, taking restrictions into account
		List<GameRuleEventType> acceptableEventTypes;
		if (hasRestriction(GameRuleRestriction.OnlyPlayerBallInteractionEvents))
			acceptableEventTypes = new List<GameRuleEventType>(new GameRuleEventType[] {
				GameRuleEventType.PlayerShootBall,
				GameRuleEventType.PlayerGrabBall,
				GameRuleEventType.PlayerTouchBall
			});
		else
			acceptableEventTypes = new List<GameRuleEventType>(playerEventTypesList);
acceptableEventTypes.Remove(GameRuleEventType.PlayerHitSportsObject);
		GameRuleEventType eventType = acceptableEventTypes[Random.Range(0, acceptableEventTypes.Count)];
		//players bumping into each other shouldn't cause them to indefinitely freeze
		if (eventType == GameRuleEventType.PlayerHitPlayer)
			restrictions.Add(GameRuleRestriction.NoPlayerFreezeUntilConditions);
		string param = null;
		if (eventType == GameRuleEventType.PlayerHitFieldObject)
			param = randomFieldObjectType();
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	//generate a random EventHappenedCondition for a ball event source
	public static GameRuleEventHappenedCondition randomBallEventHappenedCondition(GameRuleSelector selector) {
		//build the list of acceptable event types, taking restrictions into account
		List<GameRuleEventType> acceptableEventTypes;
		if (hasRestriction(GameRuleRestriction.OnlyBallFieldObjectInteractionEvents))
			acceptableEventTypes = new List<GameRuleEventType>(new GameRuleEventType[] {
				GameRuleEventType.BallHitFieldObject
			});
		else
			acceptableEventTypes = new List<GameRuleEventType>(ballEventTypesList);
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
		ruleActionSelector = randomSelectorForSource(ballCondition);
		return new GameRuleAction(ruleActionSelector, randomActionActionForTarget(ruleActionSelector, ballCondition));
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
		List<System.Type> acceptableActionTypes;
		if (hasRestriction(GameRuleRestriction.OnlyPointsActions))
			acceptableActionTypes = new List<System.Type>(new System.Type[] {
				typeof(GameRulePointsPlayerActionAction)
			});
		else
			acceptableActionTypes = new List<System.Type>(new System.Type[] {
				typeof(GameRulePointsPlayerActionAction),
				typeof(GameRuleFreezeActionAction),
				typeof(GameRuleDuplicateActionAction),
				typeof(GameRuleFreezeUntilConditionActionAction)
			});
		if (hasRestriction(GameRuleRestriction.NoPlayerFreezeUntilConditions))
			acceptableActionTypes.Remove(typeof(GameRuleFreezeUntilConditionActionAction));

		//pick one of the action types
		System.Type chosenType = acceptableActionTypes[Random.Range(0, acceptableActionTypes.Count)];
		if (chosenType == typeof(GameRulePointsPlayerActionAction)) {
			int minPoints = hasRestriction(GameRuleRestriction.OnlyPositivePointAmounts) ? 0 : -5;
			int points = Random.Range(minPoints, 10);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (chosenType == typeof(GameRuleFreezeActionAction))
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (chosenType == typeof(GameRuleDuplicateActionAction))
			return new GameRuleDuplicateActionAction();
		else if (chosenType == typeof(GameRuleFreezeUntilConditionActionAction)) {
			//make sure the condition doesn't require the frozen player to do anything
			if (ruleActionSelector is GameRulePlayerSelector || ruleActionSelector is GameRuleBallShooterSelector)
				restrictions.Add(GameRuleRestriction.NoYouPlayerTargetSelectors);
			else if (ruleActionSelector is GameRuleOpponentSelector || ruleActionSelector is GameRuleBallShooterOpponentSelector)
				restrictions.Add(GameRuleRestriction.NoOpponentPlayerTargetSelectors);

			GameRuleSelector sourceToTrigger = randomSelectorForSource(ballCondition);

			restrictions.Add(GameRuleRestriction.OnlyPlayerBallInteractionEvents);
			restrictions.Add(GameRuleRestriction.OnlyBallFieldObjectInteractionEvents);
			return new GameRuleFreezeUntilConditionActionAction(
				randomEventHappenedConditionForTarget(
					sourceToTrigger));
		} else
			throw new System.Exception("Bug: Invalid action type!");
	}
	public static GameRuleActionAction randomBallActionAction(bool ballCondition) {
		//build the list of acceptable action types, taking restrictions into account
		System.Type[] acceptableActionTypes = new System.Type[] {
			//balls getting frozen hasn't made for fun gameplay yet
			//typeof(GameRuleFreezeActionAction),
			typeof(GameRuleDuplicateActionAction)
			//typeof(GameRuleFreezeUntilConditionActionAction)
		};

		//pick one of the action types
		System.Type chosenType = acceptableActionTypes[Random.Range(0, acceptableActionTypes.Length)];
		if (chosenType == typeof(GameRuleFreezeActionAction))
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
		//build selectors list, taking restrictions into account
		List<GameRuleSelector> acceptableSelectors = new List<GameRuleSelector>(new GameRuleSelector[] {
			GameRulePlayerSelector.instance,
			GameRuleOpponentSelector.instance
		});
		if (hasRestriction(GameRuleRestriction.NoYouPlayerTargetSelectors))
			acceptableSelectors.Remove(GameRulePlayerSelector.instance);
		if (hasRestriction(GameRuleRestriction.NoOpponentPlayerTargetSelectors))
			acceptableSelectors.Remove(GameRuleOpponentSelector.instance);

		//pick a selector
		return acceptableSelectors[Random.Range(0, acceptableSelectors.Count)];
	}
	//generate a selector based on a condition for events caused by balls
	public static GameRuleSelector randomBallSourceSelector() {
		//build selectors list, taking restrictions into account
		List<GameRuleSelector> acceptableSelectors;
		if (hasRestriction(GameRuleRestriction.OnlyPlayerTargetSelectors))
			acceptableSelectors = new List<GameRuleSelector>(new GameRuleSelector[] {
				GameRuleBallShooterSelector.instance,
				GameRuleBallShooterOpponentSelector.instance
			});
		else
			acceptableSelectors = new List<GameRuleSelector>(new GameRuleSelector[] {
				GameRuleBallSelector.instance,
				GameRuleBallShooterSelector.instance,
				GameRuleBallShooterOpponentSelector.instance
			});
		if (hasRestriction(GameRuleRestriction.NoYouPlayerTargetSelectors))
			acceptableSelectors.Remove(GameRuleBallShooterSelector.instance);
		if (hasRestriction(GameRuleRestriction.NoOpponentPlayerTargetSelectors))
			acceptableSelectors.Remove(GameRuleBallShooterOpponentSelector.instance);


		//pick a selector
		return acceptableSelectors[Random.Range(0, acceptableSelectors.Count)];
	}
}
