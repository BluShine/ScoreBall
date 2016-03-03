using UnityEngine;
using System.Collections.Generic;

////////////////Sports object selectors based on the source of an event////////////////
public abstract class GameRuleSelector {
	public abstract SportsObject target(SportsObject source);
	public abstract System.Type targetType();
	public abstract void addIcons(List<GameObject> iconList);
	//serialization
	public const int GAME_RULE_PLAYER_SELECTOR_BYTE_VAL = 0;
	public const int GAME_RULE_OPPONENT_SELECTOR_BYTE_VAL = 1;
	public const int GAME_RULE_BALL_SHOOTER_SELECTOR_BYTE_VAL = 2;
	public const int GAME_RULE_BALL_SHOOTER_OPPONENT_SELECTOR_BYTE_VAL = 3;
	public const int GAME_RULE_BALL_SELECTOR_BYTE_VAL = 4;
	public const int GAME_RULE_SELECTOR_BIT_SIZE = 3;
	public abstract void packToString(GameRuleSerializer serializer);
	public static GameRuleSelector unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_SELECTOR_BIT_SIZE);
		if (subclassByte == GAME_RULE_PLAYER_SELECTOR_BYTE_VAL)
			return GameRulePlayerSelector.instance;
		else if (subclassByte == GAME_RULE_OPPONENT_SELECTOR_BYTE_VAL)
			return GameRuleOpponentSelector.instance;
		else if (subclassByte == GAME_RULE_BALL_SHOOTER_SELECTOR_BYTE_VAL)
			return GameRuleBallShooterSelector.instance;
		else if (subclassByte == GAME_RULE_BALL_SHOOTER_OPPONENT_SELECTOR_BYTE_VAL)
			return GameRuleBallShooterOpponentSelector.instance;
		else if (subclassByte == GAME_RULE_BALL_SELECTOR_BYTE_VAL)
			return GameRuleBallSelector.instance;
		else
			throw new System.Exception("Invalid GameRuleSelector unpacked byte " + subclassByte);
	}
}

public abstract class GameRuleSourceSelector : GameRuleSelector {
	public override SportsObject target(SportsObject source) {
		return source;
	}
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(iconIdentifier(targetType()));
	}
	public static GameObject iconIdentifier(System.Type type) {
		if (type == typeof(TeamPlayer))
			return GameRuleIconStorage.instance.playerIcon;
		else if (type == typeof(Ball))
			return GameRuleIconStorage.instance.genericBallIcon;
		else
			throw new System.Exception("Bug: could not get identifying icon for System.Type " + type);
	}
	public override string ToString() {
		return stringIdentifier(targetType());
	}
	public static string stringIdentifier(System.Type type) {
		if (type == typeof(TeamPlayer))
			return "player";
		else if (type == typeof(Ball))
			return "ball";
		else
			throw new System.Exception("Bug: could not get identifying string for System.Type " + type);
	}
	//serialization
	public const int GAME_RULE_PLAYER_SOURCE_SELECTOR_BYTE_VAL = 0;
	public const int GAME_RULE_BALL_SOURCE_SELECTOR_BYTE_VAL = 1;
	public const int GAME_RULE_SOURCE_SELECTOR_BIT_SIZE = 1;
	public abstract void packToStringAsSourceSelector(GameRuleSerializer serializer);
	public static new GameRuleSourceSelector unpackFromString(GameRuleDeserializer deserializer) {
		byte subclassByte = deserializer.unpackByte(GAME_RULE_SOURCE_SELECTOR_BIT_SIZE);
		if (subclassByte == GAME_RULE_PLAYER_SOURCE_SELECTOR_BYTE_VAL)
			return GameRulePlayerSelector.instance;
		else if (subclassByte == GAME_RULE_BALL_SOURCE_SELECTOR_BYTE_VAL)
			return GameRuleBallSelector.instance;
		else
			throw new System.Exception("Invalid GameRuleSelector unpacked byte " + subclassByte);
	}
}

////////////////The actual selectors////////////////
public class GameRulePlayerSelector: GameRuleSourceSelector {
	public static GameRulePlayerSelector instance = new GameRulePlayerSelector();
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, GAME_RULE_PLAYER_SELECTOR_BYTE_VAL);
	}
	public override void packToStringAsSourceSelector(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SOURCE_SELECTOR_BIT_SIZE, GAME_RULE_PLAYER_SOURCE_SELECTOR_BYTE_VAL);
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
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, GAME_RULE_OPPONENT_SELECTOR_BYTE_VAL);
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
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, GAME_RULE_BALL_SHOOTER_SELECTOR_BYTE_VAL);
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
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, GAME_RULE_BALL_SHOOTER_OPPONENT_SELECTOR_BYTE_VAL);
	}
}

public class GameRuleBallSelector : GameRuleSourceSelector {
	public static GameRuleBallSelector instance = new GameRuleBallSelector();
	public override System.Type targetType() {
		return typeof(Ball);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, GAME_RULE_BALL_SELECTOR_BYTE_VAL);
	}
	public override void packToStringAsSourceSelector(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SOURCE_SELECTOR_BIT_SIZE, GAME_RULE_BALL_SOURCE_SELECTOR_BYTE_VAL);
	}
}
