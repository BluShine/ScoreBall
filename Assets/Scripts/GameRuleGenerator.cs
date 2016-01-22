using UnityEngine;
using System.Collections.Generic;
using System.Text;

//this is for any restrictions in game rule generation
public enum GameRuleRestriction {
	//restrict what options can be choices at all
	OnlyPointsActions,
	OnlyEventHappenedConditions,
	OnlyPlayerTargetSelectors,
	OnlyPositivePointAmounts,
	OnlyNegativePointAmounts,
	OnlyPlayerBallInteractionEvents,
	OnlyBallFieldObjectInteractionEvents,
	OnlyFunActions,

	//restrict what options can remain choices
	NoYouPlayerTargetSelectors,
	NoOpponentPlayerTargetSelectors,
	NoPlayerFreezeUntilConditions,
	NoUntilConditionDurations,

	//tell another function to look for restrictions
	CheckFreezeUntilConditionRestrictions
};

public class GameRuleGenerator {
	const int ACTION_DURATION_SECONDS_SHORTEST = 4;
	const int ACTION_DURATION_SECONDS_LONGEST = 10;

	private static List<GameRuleRestriction> restrictions = new List<GameRuleRestriction>();
	private static GameRuleCondition ruleCondition;
	private static GameRuleSelector ruleActionSelector;

	//generate a completely new random rule
	public static GameRule GenerateNewRule(GameObject display, List<GameRuleRestriction> ruleRestrictions = null) {
		if (ruleRestrictions == null)
			populateInitialRestrictions();
		else
			restrictions = ruleRestrictions;
		ruleCondition = randomCondition();
		bool isComparison = ruleCondition is GameRuleComparisonCondition;
		bool ballCondition = false;
		if (!isComparison && ((GameRuleEventHappenedCondition)(ruleCondition)).eventType >= GameRuleEventType.BallEventTypeStart)
			ballCondition = true;
		return new GameRule(ruleCondition, randomAction(isComparison, ballCondition));
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

		System.Type chosenType = GameRuleChances.pickFrom(new List<System.Type>(new System.Type[] {
			typeof(GameRuleComparisonCondition),
			typeof(GameRuleEventHappenedCondition)
		}));
		if (chosenType == typeof(GameRuleComparisonCondition))
			return randomComparisonCondition();
		else if (chosenType == typeof(GameRuleEventHappenedCondition))
			return randomEventHappenedCondition();
		else
			throw new System.Exception("Bug: Invalid condition sub-type!");
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
		System.Type chosenType = GameRuleChances.pickFrom(new List<System.Type>(new System.Type[] {
			typeof(RuleStubPlayerEventHappenedCondition),
			typeof(RuleStubBallEventHappenedCondition)
		}));
		if (chosenType == typeof(RuleStubPlayerEventHappenedCondition))
			return randomPlayerEventHappenedCondition(GameRulePlayerSelector.instance);
		else if (chosenType == typeof(RuleStubBallEventHappenedCondition))
			return randomBallEventHappenedCondition(GameRuleBallSelector.instance);
		else
			throw new System.Exception("Bug: Invalid event-happened condition rule stub!");
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
				GameRuleEventType.PlayerGrabBall
			});
		else
			acceptableEventTypes = new List<GameRuleEventType>(GameRuleEvent.playerEventTypesList);
acceptableEventTypes.Remove(GameRuleEventType.PlayerHitSportsObject);

		GameRuleEventType eventType = GameRuleChances.pickFrom(acceptableEventTypes);
		//players bumping into each other shouldn't cause them to indefinitely freeze
		if (eventType == GameRuleEventType.PlayerHitPlayer) {
			restrictions.Add(GameRuleRestriction.NoPlayerFreezeUntilConditions);

			//both players get this, so for better wording use only player selectors
			if (selector is GameRuleOpponentSelector)
				selector = GameRulePlayerSelector.instance;
			else if (selector is GameRuleBallShooterOpponentSelector)
				selector = GameRuleBallShooterSelector.instance;
		}
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
			acceptableEventTypes = new List<GameRuleEventType>(GameRuleEvent.ballEventTypesList);
acceptableEventTypes.Remove(GameRuleEventType.BallHitSportsObject);
		GameRuleEventType eventType = GameRuleChances.pickFrom(acceptableEventTypes);
		string param = null;
		if (eventType == GameRuleEventType.BallHitFieldObject)
			param = randomFieldObjectType();
		return new GameRuleEventHappenedCondition(eventType, selector, param);
	}
	//generate a random field object type for a field object collision event
	public static string randomFieldObjectType() {
		return GameRuleChances.pickFrom(FieldObject.standardFieldObjects);
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
		else if (hasRestriction(GameRuleRestriction.OnlyFunActions))
			acceptableActionTypes = new List<System.Type>(new System.Type[] {
				typeof(GameRuleFreezeActionAction),
				typeof(GameRuleDuplicateActionAction),
				typeof(GameRuleDizzyActionAction),
				typeof(GameRuleBounceActionAction)
			});
		else //all acceptable types
			acceptableActionTypes = new List<System.Type>(new System.Type[] {
				typeof(GameRulePointsPlayerActionAction),
				typeof(GameRuleFreezeActionAction),
				typeof(GameRuleDuplicateActionAction),
				typeof(GameRuleDizzyActionAction),
				typeof(GameRuleBounceActionAction)
			});

		//pick one of the action types
		System.Type chosenType = GameRuleChances.pickFrom(acceptableActionTypes);
		if (chosenType == typeof(GameRulePointsPlayerActionAction)) { 
			int minPoints = (hasRestriction(GameRuleRestriction.OnlyPositivePointAmounts) ? 0 : -5);
			int maxPoints = (hasRestriction(GameRuleRestriction.OnlyNegativePointAmounts) ? 0 : 10);
			//if they ask for both positive and negative point amounts, give them 0
			if (maxPoints <= minPoints) {
				maxPoints = 1;
				minPoints = 0;
			}
			int points = Random.Range(minPoints, maxPoints);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (chosenType == typeof(GameRuleFreezeActionAction)) {
			if (hasRestriction(GameRuleRestriction.NoPlayerFreezeUntilConditions))
				restrictions.Add(GameRuleRestriction.NoUntilConditionDurations);
			//freeze conditions are allowed, but we need to make sure not to make anything game-breaking if we pick it
			else
				restrictions.Add(GameRuleRestriction.CheckFreezeUntilConditionRestrictions);
			return new GameRuleFreezeActionAction(randomActionDuration(ballCondition));
		} else if (chosenType == typeof(GameRuleDuplicateActionAction))
			return new GameRuleDuplicateActionAction();
		else if (chosenType == typeof(GameRuleDizzyActionAction))
			return new GameRuleDizzyActionAction(randomActionDuration(ballCondition));
		else if (chosenType == typeof(GameRuleBounceActionAction))
			return new GameRuleBounceActionAction(randomActionDuration(ballCondition));
		else
			throw new System.Exception("Bug: Invalid player action type!");
	}
	public static GameRuleActionAction randomBallActionAction(bool ballCondition) {
		//build the list of acceptable action types, taking restrictions into account
		System.Type[] acceptableActionTypes = new System.Type[] {
			//balls getting frozen hasn't made for fun gameplay yet
			//typeof(GameRuleFreezeActionAction),
			typeof(GameRuleDuplicateActionAction),
			typeof(GameRuleBounceActionAction)
		};

		//pick one of the action types
		System.Type chosenType = GameRuleChances.pickFrom(new List<System.Type>(acceptableActionTypes));
		if (chosenType == typeof(GameRuleFreezeActionAction))
			return new GameRuleFreezeActionAction(randomActionDuration(ballCondition));
		else if (chosenType == typeof(GameRuleDuplicateActionAction))
			return new GameRuleDuplicateActionAction();
		else if (chosenType == typeof(GameRuleBounceActionAction))
			return new GameRuleBounceActionAction(randomActionDuration(ballCondition));
		else
			throw new System.Exception("Bug: Invalid ball action type!");
	}

	////////////////GameRuleActionDurations for actions that last for a duration////////////////
	public static GameRuleActionDuration randomActionDuration(bool ballCondition) {
		//build the list of acceptable action types, taking restrictions into account
		List<System.Type> acceptableDurationTypes;
		acceptableDurationTypes = new List<System.Type>(new System.Type[] {
			typeof(GameRuleActionFixedDuration),
			typeof(GameRuleActionUntilConditionDuration)
		});
		if (hasRestriction(GameRuleRestriction.NoUntilConditionDurations))
			acceptableDurationTypes.Remove(typeof(GameRuleActionUntilConditionDuration));

		//pick one of the action types
		System.Type chosenType = GameRuleChances.pickFrom(acceptableDurationTypes);
		//just a duration
		if (chosenType == typeof(GameRuleActionFixedDuration))
			return new GameRuleActionFixedDuration(
				Random.Range(ACTION_DURATION_SECONDS_SHORTEST, ACTION_DURATION_SECONDS_LONGEST + 1));
		//duration until an event happens- some things will restrict what this can do
		else if (chosenType == typeof(GameRuleActionUntilConditionDuration)) {
			bool restrictFreezeUntilConditions =
				hasRestriction(GameRuleRestriction.CheckFreezeUntilConditionRestrictions);

			//if the player freezes until an event, make sure the event is not on that player
			if (restrictFreezeUntilConditions) {
				if (ruleActionSelector is GameRulePlayerSelector || ruleActionSelector is GameRuleBallShooterSelector)
					restrictions.Add(GameRuleRestriction.NoYouPlayerTargetSelectors);
				else if (ruleActionSelector is GameRuleOpponentSelector || ruleActionSelector is GameRuleBallShooterOpponentSelector)
					restrictions.Add(GameRuleRestriction.NoOpponentPlayerTargetSelectors);
			}

			GameRuleSelector sourceToTrigger = randomSelectorForSource(ballCondition);

			//restrict which kinds of events can be picked
			if (restrictFreezeUntilConditions) {
				restrictions.Add(GameRuleRestriction.OnlyPlayerBallInteractionEvents);
				restrictions.Add(GameRuleRestriction.OnlyBallFieldObjectInteractionEvents);
			}

			return new GameRuleActionUntilConditionDuration(
				randomEventHappenedConditionForTarget(
					sourceToTrigger));
		} else
			throw new System.Exception("Bug: Invalid duration type!");
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
		return GameRuleChances.pickFrom(acceptableSelectors);
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
		return GameRuleChances.pickFrom(acceptableSelectors);
	}
}

////////////////Serialization and Deserialization of rules////////////////
public class GameRuleSerializationBase {
	public const byte GAME_RULE_FORMAT_CURRENT_VERSION = 1;
	public const byte GAME_RULE_FORMAT_VERSION_BASE = 32;
	public const byte GAME_RULE_FORMAT_BITS_PER_CHAR = 5;
	public const byte GAME_RULE_FORMAT_CHAR_BIT_MASK = ~(-1 << GAME_RULE_FORMAT_BITS_PER_CHAR);
	public const byte O_CHARACTER_BYTE_VALUE = 10 + 'O' - 'A';
	//stores bits taken from the string/to be put into the string
	protected int bits = 0;
	//the count of how many bits are stored in the bits variable
	protected int bitCount = 0;
	//determine how many bits we need to store an index for this list
	public static byte bitsForIndex<T>(List<T> valueList) {
		byte bitSize = 0;
		for (int j = valueList.Count; j > 0; j >>= 1)
			bitSize++;
		return bitSize;
	}
}

////////////////Saving the rule to a string////////////////
public class GameRuleSerializer : GameRuleSerializationBase {
	private StringBuilder ruleString = new StringBuilder(":");

	public GameRuleSerializer() {
		//begin a new stringbuilder that we can add to
		//put the version number in, the colon is already there
		for (int i = GAME_RULE_FORMAT_CURRENT_VERSION; i > 0; i /= GAME_RULE_FORMAT_VERSION_BASE)
			ruleString.Insert(0, byteToChar((byte)(i)));
	}
	public static string packRuleToString(GameRule rule) {
		GameRuleSerializer serializer = new GameRuleSerializer();
		rule.packToString(serializer);
		return serializer.getStringResult();
	}
	public void packByte(byte bitSize, byte byteVal) {
		bits |= byteVal << bitCount;
		bitCount += bitSize;
		while (bitCount >= GAME_RULE_FORMAT_BITS_PER_CHAR) {
			ruleString.Append(byteToChar((byte)(bits & GAME_RULE_FORMAT_CHAR_BIT_MASK)));
			bits >>= GAME_RULE_FORMAT_BITS_PER_CHAR;
			bitCount -= GAME_RULE_FORMAT_BITS_PER_CHAR;
		}
	}
	public static char byteToChar(byte b) {
		if (b <= 9)
			return (char)(b + '0');
		//skip the O character since it looks too much like a 0
		else if (b < O_CHARACTER_BYTE_VALUE)
			return (char)(b - 10 + 'A');
		else
			return (char)(b - O_CHARACTER_BYTE_VALUE + 'P');
	}
	public void packToString<T>(T valueToPack, List<T> valueList) {
		//pack the value's index in the list
		for (byte i = (byte)(valueList.Count - 1); i >= 0; i--) {
			if (valueList[i].Equals(valueToPack)) {
				packByte(bitsForIndex(valueList), (byte)(i));
				return;
			}
		}
		throw new System.Exception("Bug: could not find the value to serialize!");
	}
	public string getStringResult() {
		if (bitCount > 0) {
			ruleString.Append(byteToChar((byte)(bits)));
			bitCount = 0;
		}
		return ruleString.ToString();
	}
}

////////////////Loading the rule from a string////////////////
public class GameRuleDeserializer : GameRuleSerializationBase {
	private List<char> ruleBits = new List<char>();
	private int version = 0;

	public GameRuleDeserializer(string ruleString) {
		//take the existing string and store it
		//find the version number
		int start = 0;
		while (true) {
			char c = ruleString[start];
			if (c == ':')
				break;
			else
				version = version * GameRuleSerializer.GAME_RULE_FORMAT_VERSION_BASE + charToByte(c);
			start++;
		}
		//add the string to our list in reverse so we can take chars off the end of it
		for (int i = ruleString.Length - 1; i > start; i--)
			ruleBits.Add(ruleString[i]);
	}
	public static GameRule unpackStringToRule(string ruleString) {
		return GameRule.unpackFromString(new GameRuleDeserializer(ruleString.ToUpper()));
	}
	public byte unpackByte(byte bitSize) {
		while (bitCount < bitSize) {
			int lastIndex = ruleBits.Count - 1;
			bits |= charToByte(ruleBits[lastIndex]) << bitCount;
			bitCount += GAME_RULE_FORMAT_BITS_PER_CHAR;
			ruleBits.RemoveAt(lastIndex);
		}
		byte b = (byte)(bits & ~(-1 << bitSize));
		bits >>= bitSize;
		bitCount -= bitSize;
		return b;
	}
	public static byte charToByte(char c) {
		if (c <= '9')
			return (byte)(c - '0');
		//there are no O characters so we have to handle that here
		else if (c < 'O')
			return (byte)(c + 10 - 'A');
		else
			return (byte)(c + O_CHARACTER_BYTE_VALUE - 'P');
	}
	public T unpackFromString<T>(List<T> valueList) {
		return valueList[unpackByte(bitsForIndex(valueList))];
	}
}
