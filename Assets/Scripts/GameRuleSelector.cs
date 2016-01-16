////////////////Sports object selectors based on the source of an event////////////////
public abstract class GameRuleSelector {
	public int conjugate; //for verbs
	public abstract SportsObject target(SportsObject source);
	public abstract System.Type targetType();
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
	public static string possessivePrefix = "your ";
	public static GameRulePlayerSelector instance = new GameRulePlayerSelector();
	public GameRulePlayerSelector() {
		conjugate = 0;
	}
	public override string ToString() {
		return "you";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 0);
	}
}

public class GameRuleOpponentSelector : GameRuleSelector {
	public static string possessivePrefix = "your opponent's ";
	public static GameRuleOpponentSelector instance = new GameRuleOpponentSelector();
	public GameRuleOpponentSelector() {
		conjugate = 1;
	}
	public override SportsObject target(SportsObject source) {
		return ((TeamPlayer)(source)).opponent;
	}
	public override string ToString() {
		return "your opponent";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 1);
	}
}

public class GameRuleBallShooterSelector : GameRuleSelector {
	public static GameRuleBallShooterSelector instance = new GameRuleBallShooterSelector();
	public GameRuleBallShooterSelector() {
		conjugate = 1;
	}
	public override SportsObject target(SportsObject source) {
		return ((Ball)(source)).currentPlayer;
	}
	public override string ToString() {
		return "the player who shot the ball";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 2);
	}
}

public class GameRuleBallShooterOpponentSelector : GameRuleSelector {
	public static GameRuleBallShooterOpponentSelector instance = new GameRuleBallShooterOpponentSelector();
	public GameRuleBallShooterOpponentSelector() {
		conjugate = 1;
	}
	public override SportsObject target(SportsObject source) {
		TeamPlayer shooter = ((Ball)(source)).currentPlayer;
		return shooter == null ? null : shooter.opponent;
	}
	public override string ToString() {
		return "the opponent of the player who shot the ball";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 3);
	}
}

public class GameRuleBallSelector : GameRuleSourceSelector {
	public static GameRuleBallSelector instance = new GameRuleBallSelector();
	public GameRuleBallSelector() {
		conjugate = 1;
	}
	public override string ToString() {
		return "the ball";
	}
	public override System.Type targetType() {
		return typeof(Ball);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_SELECTOR_BIT_SIZE, 4);
	}
}
