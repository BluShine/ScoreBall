using System;

////////////////Rule consequences////////////////
public class GameRuleAction {
	public GameRuleSelector selector;
	public GameRuleActionAction innerAction;
	public GameRuleAction(GameRuleSelector sos, GameRuleActionAction ia) {
		selector = sos;
		innerAction = ia;
	}
	public override string ToString() {
		return selector.ToString() + " " + innerAction.ToString(selector.conjugate);
	}
	public void takeAction(SportsObject source) {
		innerAction.takeAction(selector.target(source));
	}
}

////////////////The actual functionality to affect players and sports objects////////////////
public abstract class GameRuleActionAction {
	public abstract void takeAction(SportsObject so);
	public abstract string ToString(int conjugate);
}

public abstract class GameRuleUntilConditionActionAction : GameRuleActionAction {
	public GameRuleEventHappenedCondition untilCondition;
	public GameRuleUntilConditionActionAction(GameRuleEventHappenedCondition grehc) {
		untilCondition = grehc;
	}
	public override void takeAction(SportsObject so) {
		GameRules.currentGameRules.waitTimers.Add(new GameRuleActionWaitTimer(untilCondition, so, this));
	}
	public override string ToString(int conjugate) {
		return getConjugate(conjugate) + "until " + untilCondition.ToString();
	}
	public abstract void cancelAction(SportsObject so);
	public abstract string getConjugate(int conjugate);
}

public class GameRulePointsPlayerActionAction : GameRuleActionAction {
	public static string[] gainConjugates = new string[] { "gain ", "gains " };
	public static string[] loseConjugates = new string[] { "lose ", "loses " };
	public int pointsGiven;
	public GameRulePointsPlayerActionAction(int pg) {
		pointsGiven = pg;
	}
	public override void takeAction(SportsObject so) {
		((TeamPlayer)(so)).ScorePoints(pointsGiven);
	}
	public override string ToString(int conjugate) {
		string pluralPointString = Math.Abs(pointsGiven) == 1 ? " point" : " points";
		return pointsGiven >= 0 ?
			gainConjugates[conjugate] + pointsGiven.ToString() + pluralPointString :
			loseConjugates[conjugate] + (-pointsGiven).ToString() + pluralPointString;
	}
}

public class GameRuleFreezeActionAction : GameRuleActionAction {
	public static string[] freezeConjugates = new string[] { "freeze ", "freezes " };
	public float timeFrozen;
	public GameRuleFreezeActionAction(float tf) {
		timeFrozen = tf;
	}
	public override void takeAction(SportsObject so) {
		so.Freeze(timeFrozen);
	}
	public override string ToString(int conjugate) {
		return freezeConjugates[conjugate] + "for " + timeFrozen.ToString("F1") + " seconds";
	}
}

public class GameRuleDuplicateActionAction : GameRuleActionAction {
	public static string[] duplicateConjugates = new string[] { "get ", "gets " };
	public override void takeAction(SportsObject so) {
		so.Duplicate(1);
	}
	public override string ToString(int conjugate) {
		return duplicateConjugates[conjugate] + "duplicated";
	}
}

public class GameRuleFreezeUntilConditionActionAction : GameRuleUntilConditionActionAction {
	public static string[] freezeConjugates = new string[] { "freeze ", "freezes " };
	public GameRuleFreezeUntilConditionActionAction(GameRuleEventHappenedCondition grehc) :
		base(grehc) {
	}
	public override void takeAction(SportsObject so) {
		so.Freeze(1000000000.0f);
		base.takeAction(so);
	}
	public override void cancelAction(SportsObject so) {
		so.Unfreeze();
	}
	public override string getConjugate(int conjugate) {
		return freezeConjugates[conjugate];
	}
}

////////////////Wait timers for actions that don't happen until an event////////////////
public class GameRuleActionWaitTimer {
	public GameRuleEventHappenedCondition condition;
	public SportsObject target;
	public GameRuleUntilConditionActionAction action;
	public GameRuleActionWaitTimer(GameRuleEventHappenedCondition grehc, SportsObject so,
		GameRuleUntilConditionActionAction grucaa) {
		condition = grehc;
		target = so;
		action = grucaa;
	}
	public bool conditionHappened(GameRuleEvent gre) {
		if (condition.conditionHappened(gre, target)) {
			action.cancelAction(target);
			return true;
		}
		return false;
	}
}
