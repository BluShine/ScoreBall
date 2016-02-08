﻿using UnityEngine;
using System.Collections.Generic;

////////////////Base classes for actions which affect other rules instead of affecting objects directly////////////////
public class GameRuleMetaRuleAction : GameRuleAction {
	public GameRuleMetaRule innerMetaRule;
	public GameRuleMetaRuleAction(GameRuleMetaRule imr) {
		innerMetaRule = imr;
	}
	public override string ToString() {
		return innerMetaRule.ToString();
	}
	public override void addIcons(List<GameObject> iconList) {
		innerMetaRule.addIcons(iconList);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_ACTION_BIT_SIZE, 1);
		innerMetaRule.packToString(serializer);
	}
	public static new GameRuleMetaRuleAction unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleMetaRule imr = GameRuleMetaRule.unpackFromString(deserializer);
		return new GameRuleMetaRuleAction(imr);
	}
}

public abstract class GameRuleMetaRule {
	public SportsObject lastInterceptionSource = null;
	public abstract SportsObject interceptSelection(SportsObject so);
	public abstract void addIcons(List<GameObject> iconList);
	//0=GameRulePlayerSwapMetaRule
	public const int GAME_RULE_META_RULE_BIT_SIZE = 1;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleMetaRule unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_META_RULE_BIT_SIZE);
		if (subclassByte == 0)
			return GameRulePlayerSwapMetaRule.unpackFromString(deserializer);
		else
			throw new System.Exception("Invalid GameRuleMetaRule unpacked byte " + subclassByte);
	}
}

////////////////The actual metarules////////////////
public class GameRulePlayerSwapMetaRule : GameRuleMetaRule {
	public override SportsObject interceptSelection(SportsObject so) {
		lastInterceptionSource = so;
		return ((TeamPlayer)(so)).opponent;
	}
	public override string ToString() {
		return "effects that happen to player happen to opponent instead";
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.gainsEffectIcon);
		iconList.Add(GameRuleIconStorage.instance.playerIcon);
		iconList.Add(GameRuleIconStorage.instance.resultsInIcon);
		iconList.Add(GameRuleIconStorage.instance.gainsEffectIcon);
		iconList.Add(GameRuleIconStorage.instance.opponentIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_META_RULE_BIT_SIZE, 0);
	}
	public static new GameRulePlayerSwapMetaRule unpackFromString(GameRuleDeserializer deserializer) {
		return new GameRulePlayerSwapMetaRule();
	}
}