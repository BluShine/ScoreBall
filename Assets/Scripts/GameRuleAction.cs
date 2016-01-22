using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

////////////////Rule consequence base classes////////////////
public class GameRuleAction {
	public GameRuleSelector selector;
	public GameRuleActionAction innerAction;
	public GameRuleAction(GameRuleSelector sos, GameRuleActionAction ia) {
		selector = sos;
		innerAction = ia;
	}
	public override string ToString() {
		return selector.ToString() + " " + innerAction.ToString();
	}
	public void takeAction(SportsObject source) {
		SportsObject target = selector.target(source);
		if (target != null)
			innerAction.takeAction(source, target);
	}
	public void packToString(GameRuleSerializer serializer) {
		selector.packToString(serializer);
		innerAction.packToString(serializer);
	}
	public static GameRuleAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleSelector s = GameRuleSelector.unpackFromString(deserializer);
		GameRuleActionAction ia = GameRuleActionAction.unpackFromString(deserializer);
		return new GameRuleAction(s, ia);
	}
}

public abstract class GameRuleActionAction {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract void takeAction(SportsObject source, SportsObject target);
	//000=GameRulePointsPlayerActionAction
	//001=GameRuleDuplicateActionAction
	//010=GameRuleFreezeActionAction
	//011=GameRuleDizzyActionAction
	//100=GameRuleBounceActionAction
	public const int GAME_RULE_ACTION_ACTION_BIT_SIZE = 3;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleActionAction unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_ACTION_ACTION_BIT_SIZE);
		if (subclassByte == 0)
			return GameRulePointsPlayerActionAction.unpackFromString(deserializer);
		else if (subclassByte == 1)
			return GameRuleDuplicateActionAction.unpackFromString(deserializer);
		else if (subclassByte == 2)
			return GameRuleFreezeActionAction.unpackFromString(deserializer);
		else if (subclassByte == 3)
			return GameRuleDizzyActionAction.unpackFromString(deserializer);
		else if (subclassByte == 4)
			return GameRuleBounceActionAction.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleActionAction unpacked byte " + subclassByte);
	}
}

public abstract class GameRuleDurationActionAction : GameRuleActionAction {
	public GameRuleActionDuration duration;
	public GameRuleDurationActionAction(GameRuleActionDuration d) {
		duration = d;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		duration.addRequiredObjects(requiredObjectsList);
	}
	public override string ToString() {
		return getVerb() + " " + duration.ToString();
	}
	public abstract void cancelAction(SportsObject so);
	//this class handles the full ToString, subclasses just need to return the verb
	public abstract string getVerb();
	public override void packToString(GameRuleSerializer serializer) {
		duration.packToString(serializer);
	}
}

////////////////The actual functionality to affect players and sports objects (one-off actions)////////////////
public class GameRulePointsPlayerActionAction : GameRuleActionAction {
	public const int POINTS_SERIALIZATION_BIT_COUNT = 5;
	public const int POINTS_SERIALIZATION_MASK = ~(-1 << POINTS_SERIALIZATION_BIT_COUNT);
	public const int POINTS_SERIALIZATION_MAX_VALUE = 20;
	public int pointsGiven;
	public GameRulePointsPlayerActionAction(int pg) {
		pointsGiven = pg;
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		((TeamPlayer)(target)).ScorePoints(pointsGiven);
		GameRules.instance.spawnPointsText(pointsGiven, (TeamPlayer)(target));
	}
	public override string ToString() {
		string pluralPointString = Math.Abs(pointsGiven) == 1 ? " point" : " points";
		return pointsGiven >= 0 ?
			"gains " + pointsGiven.ToString() + pluralPointString :
			"loses " + (-pointsGiven).ToString() + pluralPointString;
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_ACTION_BIT_SIZE, 0);
		//we'll just save N bits of the points, so one of the (2^N) consecutive values <= POINTS_SERIALIZATION_MAX_VALUE
		serializer.packByte(POINTS_SERIALIZATION_BIT_COUNT, (byte)(pointsGiven & POINTS_SERIALIZATION_MASK));
	}
	public static new GameRulePointsPlayerActionAction unpackFromString(GameRuleDeserializer deserializer) {
		//if it's over the max value, it's actually the low bits of a negative number
		int pg = deserializer.unpackByte(POINTS_SERIALIZATION_BIT_COUNT);
		if (pg > POINTS_SERIALIZATION_MAX_VALUE)
			pg |= ~POINTS_SERIALIZATION_MASK;
		return new GameRulePointsPlayerActionAction(pg);
	}
}

public class GameRuleDuplicateActionAction : GameRuleActionAction {
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Duplicate(1);
	}
	public override string ToString() {
		return "gets duplicated";
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_ACTION_BIT_SIZE, 1);
	}
	public static new GameRuleDuplicateActionAction unpackFromString(GameRuleDeserializer deserializer) {
		return new GameRuleDuplicateActionAction();
	}
}

////////////////The actual functionality to affect players and sports objects (duration actions)////////////////
public class GameRuleFreezeActionAction : GameRuleDurationActionAction {
	public GameRuleFreezeActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Freeze(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "freezes";
	}
	public override void cancelAction(SportsObject so) {
		so.Unfreeze();
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_ACTION_BIT_SIZE, 2);
		base.packToString(serializer);
	}
	public static new GameRuleFreezeActionAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleFreezeActionAction(d);
	}
}

public class GameRuleDizzyActionAction : GameRuleDurationActionAction {
	public GameRuleDizzyActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.BeDizzy(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "becomes dizzy";
	}
	public override void cancelAction(SportsObject so) {
		so.StopBeingDizzy();
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_ACTION_BIT_SIZE, 3);
		base.packToString(serializer);
	}
	public static new GameRuleDizzyActionAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleDizzyActionAction(d);
	}
}

public class GameRuleBounceActionAction : GameRuleDurationActionAction {
	public GameRuleBounceActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.StartBouncing(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "bounces";
	}
	public override void cancelAction(SportsObject so) {
		so.StopBouncing();
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_ACTION_BIT_SIZE, 4);
		base.packToString(serializer);
	}
	public static new GameRuleBounceActionAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleBounceActionAction(d);
	}
}

////////////////Functionality for rules that happen for a duration////////////////
public abstract class GameRuleActionDuration {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract float startDuration(SportsObject source, SportsObject target, GameRuleDurationActionAction action);
	//0=GameRuleActionFixedDuration
	//1=GameRuleActionUntilConditionDuration
	public const int GAME_RULE_ACTION_DURATION_BIT_SIZE = 1;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleActionDuration unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_ACTION_DURATION_BIT_SIZE);
		if (subclassByte == 0)
			return GameRuleActionFixedDuration.unpackFromString(deserializer);
		else if (subclassByte == 1)
			return GameRuleActionUntilConditionDuration.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleActionDuration unpacked byte " + subclassByte);
	}
}

public class GameRuleActionFixedDuration : GameRuleActionDuration {
	public const int DURATION_SERIALIZATION_BIT_COUNT = 4;
	public const int DURATION_SERIALIZATION_MASK = ~(-1 << DURATION_SERIALIZATION_BIT_COUNT);
	public int duration;
	public GameRuleActionFixedDuration(int d) {
		duration = d;
	}
	public override float startDuration(SportsObject source, SportsObject target, GameRuleDurationActionAction action) {
		return duration;
	}
	public override string ToString() {
		return "for " + duration + " seconds";
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_DURATION_BIT_SIZE, 0);
		//we'll just save N bits, so from 0 to (2^N-1)
		serializer.packByte(DURATION_SERIALIZATION_BIT_COUNT, (byte)(duration & DURATION_SERIALIZATION_MASK));
	}
	public static new GameRuleActionFixedDuration unpackFromString(GameRuleDeserializer deserializer) {
		//we only store non-negative values
		int d = deserializer.unpackByte(DURATION_SERIALIZATION_BIT_COUNT);
		return new GameRuleActionFixedDuration(d);
	}
}

public class GameRuleActionUntilConditionDuration : GameRuleActionDuration {
	public GameRuleEventHappenedCondition untilCondition;
	public GameRuleActionUntilConditionDuration(GameRuleEventHappenedCondition grehc) {
		untilCondition = grehc;
	}
	public override float startDuration(SportsObject source, SportsObject target, GameRuleDurationActionAction action) {
		GameRules.instance.waitTimers.Add(new GameRuleActionWaitTimer(untilCondition, source, target, action));
		return 1000000000.0f;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		untilCondition.addRequiredObjects(requiredObjectsList);
	}
	public override string ToString() {
		return "until " + untilCondition.ToString();
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_DURATION_BIT_SIZE, 1);
		untilCondition.packToString(serializer);
	}
	public static new GameRuleActionUntilConditionDuration unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleEventHappenedCondition uc = GameRuleEventHappenedCondition.unpackFromString(deserializer);
		return new GameRuleActionUntilConditionDuration(uc);
	}
}

////////////////Wait timers for actions that don't happen until an event////////////////
public class GameRuleActionWaitTimer {
	public GameRuleEventHappenedCondition condition;
	public SportsObject source; //this caused the original condition
	public SportsObject target; //this is the object that the action happened to
	public GameRuleDurationActionAction action;
	public GameRuleActionWaitTimer(GameRuleEventHappenedCondition grehc, SportsObject sos, SportsObject sot,
		GameRuleDurationActionAction grdaa) {
		condition = grehc;
		source = sos;
		target = sot;
		action = grdaa;
	}
	public bool conditionHappened(GameRuleEvent gre) {
		if (condition.conditionHappened(gre, source)) {
			cancelAction();
			return true;
		}
		return false;
	}
	public void cancelAction() {
		action.cancelAction(target);
	}
}
