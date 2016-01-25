using UnityEngine;
using System.Collections;
using System.Collections.Generic;

////////////////Rule conditions////////////////
public abstract class GameRuleCondition {
	public virtual void checkCondition(List<TeamPlayer> triggeringPlayers) {}
	public virtual bool conditionHappened(GameRuleEvent gre) {return false;}
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract void addIcons(List<Sprite> iconList);
	//0=GameRuleComparisonCondition
	//1=GameRuleEventHappenedCondition
	public const int GAME_RULE_CONDITION_BIT_SIZE = 1;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleCondition unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_CONDITION_BIT_SIZE);
		if (subclassByte == 0)
			return GameRuleComparisonCondition.unpackFromString(deserializer);
		else if (subclassByte == 1)
			return GameRuleEventHappenedCondition.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleCondition unpacked byte " + subclassByte);
	}
}

////////////////Conditions that trigger actions when checked////////////////
public class GameRuleComparisonCondition : GameRuleCondition {
	public GameRuleConditionOperator conditionOperator;
	public GameRuleComparisonCondition(GameRuleConditionOperator grco) {
		conditionOperator = grco;
	}
	public override void addIcons(List<Sprite> iconList) {
		throw new System.Exception("Icon displays not yet supported for comparison conditions!");
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_CONDITION_BIT_SIZE, 0);
		throw new System.Exception("Rule serialization not yet supported for comparison conditions!");
	}
	public static new GameRuleComparisonCondition unpackFromString(GameRuleDeserializer deserializer) {
		throw new System.Exception("Rule deserialization not yet supported for comparison conditions!");
	}
}

//comparison between a value on a player and a value that may or may not be on a player
public class GameRulePlayerValueComparisonCondition : GameRuleComparisonCondition {
	public GameRulePlayerValue leftGRPV;
	public GameRuleValue rightGRV;
	public GameRulePlayerValueComparisonCondition(GameRulePlayerValue grpvl, GameRuleConditionOperator grco, GameRuleValue grvr) :
		base(grco) {
		leftGRPV = grpvl;
		rightGRV = grvr;
	}
	public override void checkCondition(List<TeamPlayer> triggeringPlayers) {
		foreach (List<TeamPlayer> teamPlayerList in GameRules.instance.allPlayers) {
			TeamPlayer player = teamPlayerList[0];
			leftGRPV.player = player;
			if (rightGRV is GameRulePlayerValue)
				((GameRulePlayerValue)(rightGRV)).player = player.opponent;
			if (conditionOperator.compare(leftGRPV, rightGRV))
				triggeringPlayers.Add(player);
		}
	}
	public override string ToString() {
		return GameRulePlayerSelector.instance.ToString() + "'s" +
			leftGRPV.ToString() +
			conditionOperator.ToString() +
			((rightGRV is GameRulePlayerValue) ? GameRuleOpponentSelector.instance.ToString() + "'s" : "") +
			rightGRV.ToString();
	}
}

////////////////Operators to compare game rule values////////////////
public delegate bool GameRuleValueComparison(GameRuleValue left, GameRuleValue right);
public class GameRuleConditionOperator {
	public GameRuleValueComparison compare;
	public string compareString;
	public GameRuleConditionOperator(GameRuleValueComparison grvc, string s) {
		compare = grvc;
		compareString = s;
	}
	public override string ToString() {
		return compareString;
	}

	////////////////Boolean comparisons between two values////////////////
	public static GameRuleConditionOperator lessThanOperator = new GameRuleConditionOperator(lessThan, " < ");
	public static bool lessThan(GameRuleValue left, GameRuleValue right) {
		return left.intValue() < right.intValue();
	}
	public static GameRuleConditionOperator greaterThanOperator = new GameRuleConditionOperator(greaterThan, " > ");
	public static bool greaterThan(GameRuleValue left, GameRuleValue right) {
		return left.intValue() > right.intValue();
	}
	public static GameRuleConditionOperator lessOrEqualOperator = new GameRuleConditionOperator(lessOrEqual, " <= ");
	public static bool lessOrEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() <= right.intValue();
	}
	public static GameRuleConditionOperator greaterOrEqualOperator = new GameRuleConditionOperator(greaterOrEqual, " >= ");
	public static bool greaterOrEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() >= right.intValue();
	}
	public static GameRuleConditionOperator intEqualOperator = new GameRuleConditionOperator(intEqual, " = ");
	public static bool intEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() == right.intValue();
	}
	public static GameRuleConditionOperator intNotEqualOperator = new GameRuleConditionOperator(intNotEqual, " != ");
	public static bool intNotEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() != right.intValue();
	}
}

////////////////Values for use of comparing////////////////
public abstract class GameRuleValue {
	public virtual int intValue() { return 0; }
}

public class GameRuleIntConstantValue : GameRuleValue {
	public int val;
	public GameRuleIntConstantValue(int v) {
		val = v;
	}
	public override int intValue() { return val; }
	public override string ToString() { return val.ToString(); }
}

////////////////Values on players for use of comparing////////////////
public abstract class GameRulePlayerValue : GameRuleValue {
	//this gets set before the values are computed
	public TeamPlayer player;
}

public class GameRulePlayerScoreValue : GameRulePlayerValue {
	public override int intValue() {
		return GameRules.instance.teamScores[player.team];
	}
	public override string ToString() {
		return "score";
	}
}
