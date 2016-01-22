using UnityEngine;
using System.Collections.Generic;

//these are empty classes that exist solely for the purpose of producing a System.Type
class RuleStubPlayerEventHappenedCondition {}
class RuleStubBallEventHappenedCondition {}

//randomly determine which value of a given list should be picked based on each value's weight compared to similar values
class GameRuleChances {
	public static Dictionary<System.Type, int> typeofChances = createTypeofChances();
	public static Dictionary<System.Type, int> createTypeofChances() {
		Dictionary<System.Type, int> d = new Dictionary<System.Type, int>();

		//which kind of condition to do
		d[typeof(GameRuleComparisonCondition)]										= 0;
		d[typeof(GameRuleEventHappenedCondition)]									= 1;

		//there would be a big section here for comparison conditions but we're not using them right now

		//which target for event-happened conditions to do
		d[typeof(RuleStubPlayerEventHappenedCondition)]								= 2;
		d[typeof(RuleStubBallEventHappenedCondition)]								= 1;

		//which action-actions to use
		d[typeof(GameRulePointsPlayerActionAction)]									= 1;
		d[typeof(GameRuleFreezeActionAction)]										= 1;
		d[typeof(GameRuleDuplicateActionAction)]									= 1;
		d[typeof(GameRuleDizzyActionAction)]										= 1;
		d[typeof(GameRuleBounceActionAction)]										= 1;

		//which durations to use
		d[typeof(GameRuleActionFixedDuration)]										= 1;
		d[typeof(GameRuleActionUntilConditionDuration)]								= 1;

		return d;
	}
	public static Dictionary<GameRuleEventType, int> eventTypeChances = createEventTypeChances();
	public static Dictionary<GameRuleEventType, int> createEventTypeChances() {
		Dictionary<GameRuleEventType, int> d = new Dictionary<GameRuleEventType, int>();

		d[GameRuleEventType.PlayerShootBall]							= 1;
		d[GameRuleEventType.PlayerGrabBall]								= 1;
		d[GameRuleEventType.PlayerTacklePlayer]							= 1;
		d[GameRuleEventType.PlayerHitPlayer]							= 1;
		d[GameRuleEventType.PlayerHitSportsObject]						= 1;
		d[GameRuleEventType.PlayerHitFieldObject]						= 1;
		d[GameRuleEventType.PlayerStealBall]							= 1;

		d[GameRuleEventType.BallHitSportsObject]						= 1;
		d[GameRuleEventType.BallHitFieldObject]							= 1;
		d[GameRuleEventType.BallHitBall]								= 1;

		return d;
	}
	public static Dictionary<string, int> fieldObjectChances = createFieldObjectChances();
	public static Dictionary<string, int> createFieldObjectChances() {
		Dictionary<string, int> d = new Dictionary<string, int>();

		d["footgoal"]							= 1;
		d["goalposts"]							= 1;
		d["backboardhoop"]						= 1;
		d["smallwall"]							= 1;
		d["fullgoalwall"]						= 1;
		d["boundary"]							= 1;

		return d;
	}
	public static Dictionary<GameRuleSelector, int> selectorChances = createSelectorChances();
	public static Dictionary<GameRuleSelector, int> createSelectorChances() {
		Dictionary<GameRuleSelector, int> d = new Dictionary<GameRuleSelector, int>();

		d[GameRulePlayerSelector.instance]								= 1;
		d[GameRuleOpponentSelector.instance]							= 1;

		d[GameRuleBallSelector.instance]								= 1;
		d[GameRuleBallShooterSelector.instance]							= 1;
		d[GameRuleBallShooterOpponentSelector.instance]					= 1;

		return d;
	}
	//pick a value from the list based on the rate that each value can be picked
	//chances are relative only to each other
	public static System.Type pickFrom(List<System.Type> typeofList) {
		return pickFrom(typeofList, typeofChances);
	}
	public static GameRuleEventType pickFrom(List<GameRuleEventType> eventTypeList) {
		return pickFrom(eventTypeList, eventTypeChances);
	}
	public static string pickFrom(List<string> fieldObjectList) {
		return pickFrom(fieldObjectList, fieldObjectChances);
	}
	public static GameRuleSelector pickFrom(List<GameRuleSelector> selectorList) {
		return pickFrom(selectorList, selectorChances);
	}
	public static T pickFrom<T>(List<T> valueList, Dictionary<T, int> valueChances) {
		int totalChances = 0;
		int[] valueChancesList = new int[valueList.Count];
		//cache the chance values per type and sum up the chances
		for (int i = valueList.Count - 1; i >= 0; i--)
			totalChances += (valueChancesList[i] = valueChances[valueList[i]]);
		int chosenType = Random.Range(0, totalChances);
		//find the type that chosenType corresponds to
		for (int i = valueChancesList.Length - 1; i >= 0; i--)
			if ((chosenType -= valueChancesList[i]) < 0)
				return valueList[i];
		//we should never get here
		throw new System.Exception("Bug: could not pick a random value from the list!");
	}
}
