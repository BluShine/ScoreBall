using UnityEngine;
using System.Collections.Generic;

////////////////Sports object selectors based on the source of an event////////////////
public abstract class GameRuleSelector {
	public abstract SportsObject target(SportsObject source);
	public abstract System.Type targetType();
	public abstract void addIcons(List<GameObject> iconList);
	//000=GameRulePlayerSelector
	//001=GameRuleOpponentSelector
	//010=GameRuleBallShooterSelector
	//011=GameRuleBallShooterOpponentSelector
	//100=GameRuleBallSelector
	public const int GAME_RULE_SELECTOR_BIT_SIZE = 3;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleSelector unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_SELECTOR_BIT_SIZE);
		if (subclassByte == 0)
			return GameRulePlayerSelector.instance;
		else if (subclassByte == 1)
			return GameRuleOpponentSelector.instance;
		else if (subclassByte == 2)
			return GameRuleBallShooterSelector.instance;
		else if (subclassByte == 3)
			return GameRuleBallShooterOpponentSelector.instance;
		else if (subclassByte == 4)
			return GameRuleBallSelector.instance;
		else
			throw new System.Exception("Invalid GameRuleSelector unpacked byte " + subclassByte);
	}
}

public abstract class GameRuleSourceSelector : GameRuleSelector {
	public override SportsObject target(SportsObject source) {
		return source;
	}
}

public class GameRulePlayerSelector : GameRuleSourceSelector {
	public static GameRulePlayerSelector instance = new GameRulePlayerSelector();
	public override string ToString() {
		return "player";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.playerIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 0);
	}
}

public class GameRuleOpponentSelector : GameRuleSelector {
	public static GameRuleOpponentSelector instance = new GameRuleOpponentSelector();
	public override SportsObject target(SportsObject source) {
		return ((TeamPlayer)(source)).opponent;
	}
	public override string ToString() {
		return "opponent";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.opponentIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 1);
	}
}

public class GameRuleBallShooterSelector : GameRuleSelector {
	public static GameRuleBallShooterSelector instance = new GameRuleBallShooterSelector();
	public override SportsObject target(SportsObject source) {
		return ((Ball)(source)).currentPlayer;
	}
	public override string ToString() {
		return "kicker";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.playerIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 2);
	}
}

public class GameRuleBallShooterOpponentSelector : GameRuleSelector {
	public static GameRuleBallShooterOpponentSelector instance = new GameRuleBallShooterOpponentSelector();
	public override SportsObject target(SportsObject source) {
		TeamPlayer shooter = ((Ball)(source)).currentPlayer;
		return shooter == null ? null : shooter.opponent;
	}
	public override string ToString() {
		return "kicker's opponent";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.opponentIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 3);
	}
}

public class GameRuleBallSelector : GameRuleSourceSelector {
	public static GameRuleBallSelector instance = new GameRuleBallSelector();
	public override string ToString() {
		return "ball";
	}
	public override System.Type targetType() {
		return typeof(Ball);
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(GameRuleIconStorage.instance.genericBallIcon);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 4);
	}
}
