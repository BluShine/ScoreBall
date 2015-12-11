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
		return selector.ToString() + " " + innerAction.ToString(selector.conjugate);
	}
	public void takeAction(SportsObject source) {
		SportsObject target = selector.target(source);
		if (target != null)
			innerAction.takeAction(source, target);
	}
}

public abstract class GameRuleActionAction {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract void takeAction(SportsObject source, SportsObject target);
	public abstract string ToString(int conjugate);
}

public abstract class GameRuleDurationActionAction : GameRuleActionAction {
	public GameRuleActionDuration duration;
	public GameRuleDurationActionAction(GameRuleActionDuration d) {
		duration = d;
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		duration.addRequiredObjects(requiredObjectsList);
	}
	public override string ToString(int conjugate) {
		return getConjugate(conjugate) + duration.ToString();
	}
	public virtual void cancelAction(SportsObject so) {}
	//this class handles the full ToString, subclasses just need to return the verb
	public abstract string getConjugate(int conjugate);
}

////////////////The actual functionality to affect players and sports objects (one-off actions)////////////////
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

public class GameRuleDuplicateActionAction : GameRuleActionAction {
	public static string[] duplicateConjugates = new string[] {"get ", "gets "};
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Duplicate(1);
	}
	public override string ToString(int conjugate) {
		return duplicateConjugates[conjugate] + "duplicated";
	}
}

////////////////The actual functionality for duration actions ////////////////
public class GameRuleFreezeActionAction : GameRuleDurationActionAction {
	public static string[] freezeConjugates = new string[] {"freeze ", "freezes "};
	public GameRuleFreezeActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.Freeze(duration.startDuration(source, target, this));
	}
	public override string getConjugate(int conjugate) {
		return freezeConjugates[conjugate];
	}
	public override void cancelAction(SportsObject so) {
		so.Unfreeze();
	}
}

public class GameRuleDizzyActionAction : GameRuleDurationActionAction {
	public static string[] dizzyConjugates = new string[] {"become ", "becomes "};
	public GameRuleDizzyActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.BeDizzy(duration.startDuration(source, target, this));
	}
	public override string getConjugate(int conjugate) {
		return dizzyConjugates[conjugate] + "dizzy ";
	}
	public override void cancelAction(SportsObject so) {
		so.StopBeingDizzy();
	}
}

public class GameRuleBounceActionAction : GameRuleDurationActionAction {
	public static string[] bounceConjugates = new string[] {"bounce ", "bounces "};
	public GameRuleBounceActionAction(GameRuleActionDuration d) : base(d) {}
	public override void takeAction(SportsObject source, SportsObject target) {
		target.StartBouncing(duration.startDuration(source, target, this));
	}
	public override string getConjugate(int conjugate) {
		return bounceConjugates[conjugate];
	}
	public override void cancelAction(SportsObject so) {
		so.StopBouncing();
	}
}

////////////////Functionality for rules that happen for a duration////////////////
public abstract class GameRuleActionDuration {
	public virtual void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {}
	public abstract float startDuration(SportsObject source, SportsObject target, GameRuleDurationActionAction action);
}

public class GameRuleActionFixedDuration : GameRuleActionDuration {
	int duration;
	public GameRuleActionFixedDuration(int d) {
		duration = d;
	}
	public override float startDuration(SportsObject source, SportsObject target, GameRuleDurationActionAction action) {
		return duration;
	}
	public override string ToString() {
		return "for " + duration + " seconds";
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
