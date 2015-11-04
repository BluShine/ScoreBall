////////////////Sports object selectors based on the source of an event////////////////
public abstract class GameRuleSelector {
	public int conjugate; //for verbs
	public abstract SportsObject target(SportsObject source);
	public abstract System.Type targetType();
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
}
