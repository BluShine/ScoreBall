using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

////////////////Rule consequence base class////////////////
public abstract class GameRuleAction {
	public abstract void addIcons(List<GameObject> iconList);
	//serialization
	public const int GAME_RULE_EFFECT_ACTION_BYTE_VAL = 0;
	public const int GAME_RULE_META_RULE_ACTION_BYTE_VAL = 1;
	public const int GAME_RULE_ACTION_BIT_SIZE = 1;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleAction unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_ACTION_BIT_SIZE);
		if (subclassByte == GAME_RULE_EFFECT_ACTION_BYTE_VAL)
			return GameRuleEffectAction.unpackFromString(deserializer);
		else if (subclassByte == GAME_RULE_META_RULE_ACTION_BYTE_VAL)
			return GameRuleMetaRuleAction.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleAction unpacked byte " + subclassByte);
	}
}

////////////////Base classes for one-time actions////////////////
public class GameRuleEffectAction : GameRuleAction {
	public GameRuleSelector selector;
	public GameRuleEffect innerEffect;
	public GameRuleEffectAction(GameRuleSelector sos, GameRuleEffect ie) {
		selector = sos;
		innerEffect = ie;
	}
	public override string ToString() {
		return selector.ToString() + " " + innerEffect.ToString();
	}
	//returns the sportsobject that got affected, often the source
	public SportsObject takeAction(SportsObject source) {
		SportsObject target = GameRules.instance.interceptSelection(selector.target(source));
		if (target != null)
			innerEffect.takeAction(source, target);
		return target;
	}
	public override void addIcons(List<GameObject> iconList) {
		selector.addIcons(iconList);
		iconList.Add(GameRuleIconStorage.instance.gainsEffectIcon);
		innerEffect.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_BIT_SIZE, GAME_RULE_EFFECT_ACTION_BYTE_VAL);
		selector.packToString(serializer);
		innerEffect.packToString(serializer);
	}
	public static new GameRuleEffectAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleSelector s = GameRuleSelector.unpackFromString(deserializer);
		GameRuleEffect ia = GameRuleEffect.unpackFromString(deserializer);
		return new GameRuleEffectAction(s, ia);
	}
}

public abstract class GameRuleEffect {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract void takeAction(SportsObject source, SportsObject target);
	public abstract void addIcons(List<GameObject> iconList);
	//serialization
	public const int GAME_RULE_POINTS_PLAYER_EFFECT_BYTE_VAL = 0;
	public const int GAME_RULE_DUPLICATE_EFFECT_BYTE_VAL = 1;
	public const int GAME_RULE_FREEZE_EFFECT_BYTE_VAL = 2;
	public const int GAME_RULE_DIZZY_EFFECT_BYTE_VAL = 3;
	public const int GAME_RULE_BOUNCE_EFFECT_BYTE_VAL = 4;
	public const int GAME_RULE_EFFECT_BIT_SIZE = 3;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleEffect unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_EFFECT_BIT_SIZE);
		if (subclassByte == GAME_RULE_POINTS_PLAYER_EFFECT_BYTE_VAL)
			return GameRulePointsPlayerEffect.unpackFromString(deserializer);
		else if (subclassByte == GAME_RULE_DUPLICATE_EFFECT_BYTE_VAL)
			return GameRuleDuplicateEffect.unpackFromString(deserializer);
		else if (subclassByte == GAME_RULE_FREEZE_EFFECT_BYTE_VAL)
			return GameRuleFreezeEffect.unpackFromString(deserializer);
		else if (subclassByte == GAME_RULE_DIZZY_EFFECT_BYTE_VAL)
			return GameRuleDizzyEffect.unpackFromString(deserializer);
		else if (subclassByte == GAME_RULE_BOUNCE_EFFECT_BYTE_VAL)
			return GameRuleBounceEffect.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleEffect unpacked byte " + subclassByte);
	}
}

public abstract class GameRuleDurationEffect : GameRuleEffect {
	public GameRuleActionDuration duration;
	public GameRuleDurationEffect(GameRuleActionDuration d) {
		duration = d;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		duration.addRequiredObjects(requiredObjectsList);
	}
	public override string ToString() {
		return getVerb() + " " + duration.ToString();
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.clockIcon);
		duration.addIcons(iconList);
	}
	public abstract void cancelAction(SportsObject so);
	//this class handles the full ToString, subclasses just need to return the verb
	public abstract string getVerb();
	public override void packToString(GameRuleSerializer serializer) {
		duration.packToString(serializer);
	}
}

////////////////The actual functionality to affect players and sports objects (one-off actions)////////////////
public class GameRulePointsPlayerEffect : GameRuleEffect {
	public const int POINTS_SERIALIZATION_BIT_COUNT = 5;
	public const int POINTS_SERIALIZATION_MASK = ~(-1 << POINTS_SERIALIZATION_BIT_COUNT);
	public const int POINTS_SERIALIZATION_MAX_VALUE = 20;
	public int pointsGiven;
	public GameRulePointsPlayerEffect(int pg) {
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
	public override void addIcons(List<GameObject> iconList) {
		if (pointsGiven >= 0) {
			iconList.Add(GameRuleIconStorage.instance.charPlusIcon);
			GameRuleIconStorage.instance.addDigitIcons(pointsGiven, iconList);
		} else {
			iconList.Add(GameRuleIconStorage.instance.charMinusIcon);
			GameRuleIconStorage.instance.addDigitIcons(-pointsGiven, iconList);
		}
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_EFFECT_BIT_SIZE, GAME_RULE_POINTS_PLAYER_EFFECT_BYTE_VAL);
		//we'll just save N bits of the points, so one of the (2^N) consecutive values <= POINTS_SERIALIZATION_MAX_VALUE
		serializer.packByte(POINTS_SERIALIZATION_BIT_COUNT, (byte)(pointsGiven & POINTS_SERIALIZATION_MASK));
	}
	public static new GameRulePointsPlayerEffect unpackFromString(GameRuleDeserializer deserializer) {
		//if it's over the max value, it's actually the low bits of a negative number
		int pg = deserializer.unpackByte(POINTS_SERIALIZATION_BIT_COUNT);
		if (pg > POINTS_SERIALIZATION_MAX_VALUE)
			pg |= ~POINTS_SERIALIZATION_MASK;
		return new GameRulePointsPlayerEffect(pg);
	}
}

public class GameRuleDuplicateEffect : GameRuleEffect {
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Duplicate(1);
	}
	public override string ToString() {
		return "gets duplicated";
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.duplicatedIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_EFFECT_BIT_SIZE, GAME_RULE_DUPLICATE_EFFECT_BYTE_VAL);
	}
	public static new GameRuleDuplicateEffect unpackFromString(GameRuleDeserializer deserializer) {
		return new GameRuleDuplicateEffect();
	}
}

////////////////The actual functionality to affect players and sports objects (duration actions)////////////////
public class GameRuleFreezeEffect : GameRuleDurationEffect {
	public GameRuleFreezeEffect(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Freeze(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "freezes";
	}
	public override void cancelAction(SportsObject so) {
		so.Unfreeze();
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.frozenIcon);
		base.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_EFFECT_BIT_SIZE, GAME_RULE_FREEZE_EFFECT_BYTE_VAL);
		base.packToString(serializer);
	}
	public static new GameRuleFreezeEffect unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleFreezeEffect(d);
	}
}

public class GameRuleDizzyEffect : GameRuleDurationEffect {
	public GameRuleDizzyEffect(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.BeDizzy(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "becomes dizzy";
	}
	public override void cancelAction(SportsObject so) {
		so.StopBeingDizzy();
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.dizzyIcon);
		base.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_EFFECT_BIT_SIZE, GAME_RULE_DIZZY_EFFECT_BYTE_VAL);
		base.packToString(serializer);
	}
	public static new GameRuleDizzyEffect unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleDizzyEffect(d);
	}
}

public class GameRuleBounceEffect : GameRuleDurationEffect {
	public GameRuleBounceEffect(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.StartBouncing(duration.startDuration(source, target, this));
	}
	public override string getVerb() {
		return "bounces";
	}
	public override void cancelAction(SportsObject so) {
		so.StopBouncing();
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.bouncyIcon);
		base.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_EFFECT_BIT_SIZE, GAME_RULE_BOUNCE_EFFECT_BYTE_VAL);
		base.packToString(serializer);
	}
	public static new GameRuleBounceEffect unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleActionDuration d = GameRuleActionDuration.unpackFromString(deserializer);
		return new GameRuleBounceEffect(d);
	}
}

////////////////Functionality for rules that happen for a duration////////////////
public abstract class GameRuleActionDuration {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract float startDuration(SportsObject source, SportsObject target, GameRuleDurationEffect action);
	public abstract void addIcons(List<GameObject> iconList);
	//serialization
	public const int GAME_RULE_ACTION_FIXED_DURATION_BYTE_VAL = 0;
	public const int GAME_RULE_ACTION_UNTIL_CONDITION_DURATION_BYTE_VAL = 1;
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
	public override float startDuration(SportsObject source, SportsObject target, GameRuleDurationEffect action) {
		return duration;
	}
	public override string ToString() {
		return "for " + duration + " seconds";
	}
	public override void addIcons(List<GameObject> iconList) {
		GameRuleIconStorage.instance.addDigitIcons(duration, iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_DURATION_BIT_SIZE, GAME_RULE_ACTION_FIXED_DURATION_BYTE_VAL);
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
	public GameRuleSelector triggerSelector;
	public GameRuleActionUntilConditionDuration(GameRuleEventHappenedCondition uc) {
		untilCondition = uc;
		//steal the selector out from under the condition
		triggerSelector = uc.selector;
		uc.selector = (triggerSelector.targetType() == typeof(Ball)) ?
			(GameRuleSelector)GameRuleBallSelector.instance :
			(GameRuleSelector)GameRulePlayerSelector.instance;
	}
	public override float startDuration(SportsObject source, SportsObject target, GameRuleDurationEffect action) {
		GameRules.instance.waitTimers.Add(new GameRuleActionWaitTimer(untilCondition, triggerSelector.target(source), target, action));
		return 1000000000.0f;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		untilCondition.addRequiredObjects(requiredObjectsList);
	}
	public override string ToString() {
		//give the condition its trigger while producing string text
		return "until " + untilCondition.ToString(triggerSelector);
	}
	public override void addIcons(List<GameObject> iconList) {
		untilCondition.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_DURATION_BIT_SIZE, GAME_RULE_ACTION_UNTIL_CONDITION_DURATION_BYTE_VAL);
		//while packing, give the condition its selector to pack
		untilCondition.packToString(serializer, triggerSelector);
	}
	public static new GameRuleActionUntilConditionDuration unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleEventHappenedCondition uc = GameRuleEventHappenedCondition.unpackFromString(deserializer);
		return new GameRuleActionUntilConditionDuration(uc);
	}
}

////////////////Wait timers for actions that don't happen until an event////////////////
public class GameRuleActionWaitTimer {
	public GameRuleEventHappenedCondition condition;
	public SportsObject trigger; //this will cause the condition that ends the timer
	public SportsObject target; //this is the object that the action happened to
	public GameRuleDurationEffect effect;
	public GameRuleActionWaitTimer(GameRuleEventHappenedCondition c, SportsObject tr, SportsObject ta,
		GameRuleDurationEffect e) {
		condition = c;
		trigger = tr;
		target = ta;
		effect = e;
	}
	public bool eventHappened(GameRuleEvent gre) {
		if (condition.eventHappened(gre, trigger)) {
			cancelAction();
			return true;
		}
		return false;
	}
	public void cancelAction() {
		effect.cancelAction(target);
	}
}
