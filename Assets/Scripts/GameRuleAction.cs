using System;
using UnityEngine;
using UnityEngine.UI;

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
		SportsObject target = selector.target(source);
		if (target != null)
			innerAction.takeAction(source, target);
	}
}

public abstract class GameRuleActionAction {
	public abstract void takeAction(SportsObject source, SportsObject target);
	public abstract string ToString(int conjugate);
}

public abstract class GameRuleUntilConditionActionAction : GameRuleActionAction {
	public GameRuleEventHappenedCondition untilCondition;
	public GameRuleUntilConditionActionAction(GameRuleEventHappenedCondition grehc) {
		untilCondition = grehc;
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		GameRules.instance.waitTimers.Add(new GameRuleActionWaitTimer(untilCondition, source, target, this));
	}
	public override string ToString(int conjugate) {
		return getConjugate(conjugate) + "until " + untilCondition.ToString();
	}
	public abstract void cancelAction(SportsObject so);
	public abstract string getConjugate(int conjugate);
}

////////////////The actual functionality to affect players and sports objects////////////////
public class GameRulePointsPlayerActionAction : GameRuleActionAction {
	public static string[] gainConjugates = new string[] {"gain ", "gains "};
	public static string[] loseConjugates = new string[] {"lose ", "loses "};
	public int pointsGiven;
	public GameRulePointsPlayerActionAction(int pg) {
		pointsGiven = pg;
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		((TeamPlayer)(target)).ScorePoints(pointsGiven);
		GameRules.instance.spawnPointsText(pointsGiven, (TeamPlayer)(target));
	}
	public override string ToString(int conjugate) {
		string pluralPointString = Math.Abs(pointsGiven) == 1 ? " point" : " points";
		return pointsGiven >= 0 ?
			gainConjugates[conjugate] + pointsGiven.ToString() + pluralPointString :
			loseConjugates[conjugate] + (-pointsGiven).ToString() + pluralPointString;
	}
}

public class GameRuleFreezeActionAction : GameRuleActionAction {
	public static string[] freezeConjugates = new string[] {"freeze ", "freezes "};
	public float timeFrozen;
	public GameRuleFreezeActionAction(float tf) {
		timeFrozen = tf;
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Freeze(timeFrozen);
	}
	public override string ToString(int conjugate) {
		return freezeConjugates[conjugate] + "for " + timeFrozen.ToString("F1") + " seconds";
	}
}

public class GameRuleDuplicateActionAction : GameRuleActionAction {
	public static string[] duplicateConjugates = new string[] {"get ", "gets "};
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Duplicate(1);
	}
	public override string ToString(int conjugate) {
		return duplicateConjugates[conjugate] + "duplicated";
	}
}

public class GameRuleFreezeUntilConditionActionAction : GameRuleUntilConditionActionAction {
	public static string[] freezeConjugates = new string[] {"freeze ", "freezes "};
	public GameRuleFreezeUntilConditionActionAction(GameRuleEventHappenedCondition grehc) :
		base(grehc) {
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Freeze(1000000000.0f);
		base.takeAction(source, target);
	}
	public override void cancelAction(SportsObject so) {
		so.Unfreeze();
	}
	//not ToString because that's handled in the base class, we just need to return the verb
	public override string getConjugate(int conjugate) {
		return freezeConjugates[conjugate];
	}
}

public class GameRuleDizzyActionAction : GameRuleActionAction {
	public static string[] dizzyConjugates = new string[] {"become ", "becomes "};
	public float timeDizzy;
	public GameRuleDizzyActionAction(float td) {
		timeDizzy = td;
	}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.BeDizzy(timeDizzy);
	}
	public override string ToString(int conjugate) {
		return dizzyConjugates[conjugate] + "dizzy for " + timeDizzy.ToString("F1") + " seconds";
	}
}

////////////////Wait timers for actions that don't happen until an event////////////////
public class GameRuleActionWaitTimer {
	public GameRuleEventHappenedCondition condition;
	public SportsObject source; //this caused the original condition
	public SportsObject target; //this is the object that the action happened to
	public GameRuleUntilConditionActionAction action;
	public GameRuleActionWaitTimer(GameRuleEventHappenedCondition grehc, SportsObject sos, SportsObject sot,
		GameRuleUntilConditionActionAction grucaa) {
		condition = grehc;
		source = sos;
		target = sot;
		action = grucaa;
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
