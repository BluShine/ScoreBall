using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

////////////////Game events////////////////
public enum GameRuleEventType : int {
	PlayerEventTypeStart = 0,
	PlayerShootBall,
	PlayerGrabBall,
	PlayerTacklePlayer,
	PlayerHitPlayer,
    PlayerHitSportsObject,
    PlayerHitFieldObject,
	PlayerStealBall,
	PlayerHitInTheFaceByBall,
	PlayerTouchBall,

	BallEventTypeStart = 1000,
	BallHitSportsObject,
	BallHitFieldObject,
	BallHitBall
}

public class GameRuleEvent {
	public TeamPlayer instigator;
	public TeamPlayer victim;
	public GameRuleEventType eventType;
	public Ball ball;
	public Ball secondaryBall;
    public SportsObject sportsObj;
    public FieldObject fieldObj;
	public string param;
	public GameRuleEvent(GameRuleEventType gret, TeamPlayer tp = null, TeamPlayer vct = null,
		Ball bl = null, Ball bl2 = null, SportsObject so = null, FieldObject fo = null) {
		eventType = gret;
		instigator = tp;
		victim = vct;
		ball = bl;
		secondaryBall = bl2;
        sportsObj = so;
        fieldObj = fo;
		if (gret == GameRuleEventType.PlayerHitFieldObject || gret == GameRuleEventType.BallHitFieldObject)
			param = fo.sportName;
		else
			param = null;
    }
	public SportsObject getEventSource() {
		if (eventType >= GameRuleEventType.BallEventTypeStart)
			return ball;
		else
			return instigator;
	}
}

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	List<GameRule> rulesList = new List<GameRule>();
	public GameObject ruleDisplayPrefab;
	public GameObject uiCanvas;

	public List<List<TeamPlayer>> allPlayers = new List<List<TeamPlayer>>();
	public List<Text> teamTexts;
	public List<int> teamScores;
	//so that we have access to scores and stuff when evaluating rules
	public static GameRules currentGameRules;

	public void RegisterPlayer(TeamPlayer tp) {
		//fill unused teams in the allPlayers list with nulls as needed
		for (int i = tp.team - allPlayers.Count; i >= 1; i--)
			allPlayers.Add(null);

		//add a new list for the team if one doesn't already exist
		if (allPlayers.Count == tp.team)
			allPlayers.Add(new List<TeamPlayer>());

		allPlayers[tp.team].Add(tp);
	}
	public void UpdateScore() {
		foreach(List<TeamPlayer> teamPlayerList in allPlayers)
		{
			//in case the team numbers are not consecutive
			if (teamPlayerList == null)
				continue;

			int totalscore = 0;
			foreach(TeamPlayer p in teamPlayerList)
				totalscore += p.score;
			int team = teamPlayerList[0].team;
			teamTexts[team].text = "Score: " + totalscore;
			teamScores[team] = totalscore;
		}
	}
	public void GenerateNewRule() {
		//only 3 rules for now
		if (rulesList.Count >= 3)
			return;

		//shift the current rules left
		float widthoffset = ((RectTransform)(ruleDisplayPrefab.transform)).rect.width * -0.5f;
		foreach (GameRule gameRule in rulesList) {
			offsetRuleDisplayX(gameRule, widthoffset);
		}

		GameObject display = (GameObject)Instantiate(ruleDisplayPrefab);
		display.transform.SetParent(uiCanvas.transform);
		display.transform.localPosition = ruleDisplayPrefab.transform.localPosition;
        display.transform.localScale = ruleDisplayPrefab.transform.localScale;

		//unity has no good way of giving us the button we clicked, so we have to remember it here
		Button deleteButton = display.transform.FindChild("Delete Button").gameObject.GetComponent<Button>();
		deleteButton.onClick.AddListener(() => {this.DeleteRule(deleteButton);});

		GameRuleCondition condition = randomCondition();
		bool isComparison = condition is GameRuleComparisonCondition;
		bool playerAction = true;
		if (!isComparison && ((GameRuleEventHappenedCondition)(condition)).eventType >= GameRuleEventType.BallEventTypeStart)
			playerAction = false;
        GameRule rule = new GameRule(condition, randomAction(isComparison, playerAction), display);
		rulesList.Add(rule);
		offsetRuleDisplayX(rule, -widthoffset * (rulesList.Count - 1));
		Transform t = display.transform;
		t.GetChild(0).gameObject.GetComponent<Text>().text = "If " + rule.condition.ToString();
		t.GetChild(1).gameObject.GetComponent<Text>().text = "Then " + rule.action.ToString();
	}
	public void DeleteRule(Button button) {
		RectTransform ruleTransform = (RectTransform)(button.transform.parent);
		float widthoffset = ruleTransform.rect.width * 0.5f;
		GameObject ruleDisplay = ruleTransform.gameObject;
		for (int i = 0; i < rulesList.Count;) {
			GameRule gameRule = rulesList[i];
			if (gameRule.ruleDisplay == ruleDisplay) {
				widthoffset = -widthoffset;
				rulesList.RemoveAt(i);
			} else {
				offsetRuleDisplayX(gameRule, widthoffset);
				i++;
			}
		}
		Destroy(ruleDisplay);
	}

	public static GameRuleCondition randomCondition() {
		return /*Random.Range(0, 2) == 0 ? randomComparisonCondition() :*/ randomEventHappenedCondition();
	}
	public static GameRuleComparisonCondition randomComparisonCondition() {
		//only player-value comparison conditions for now
		return new GameRulePlayerValueComparisonCondition(randomPlayerValue(), randomConditionOperator(), Random.Range(0, 2) == 0 ? randomValue() : randomPlayerValue());
	}
	public static GameRulePlayerValue randomPlayerValue() {
		return new GameRulePlayerScoreValue();
	}
	public static GameRuleConditionOperator randomConditionOperator() {
		int rand = Random.Range(0, 6);
		if (rand == 0)
			return GameRuleConditionOperator.lessThanOperator;
		else if (rand == 1)
			return GameRuleConditionOperator.greaterThanOperator;
		else if (rand == 2)
			return GameRuleConditionOperator.lessOrEqualOperator;
		else if (rand == 3)
			return GameRuleConditionOperator.greaterOrEqualOperator;
		else if (rand == 4)
			return GameRuleConditionOperator.intEqualOperator;
		else
			return GameRuleConditionOperator.intNotEqualOperator;
	}
	public static GameRuleValue randomValue() {
		return new GameRuleIntConstantValue(Random.Range(-100, 101));
	}
	public static GameRuleCondition randomEventHappenedCondition() {
		//2/3 player, 1/3 ball
		if (Random.Range(0, 3) < 2) {
			int rand = Random.Range(0, 9);
			if (rand == 0)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerShootBall, "you shoot the ball");
			else if (rand == 1)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerGrabBall, "you grab the ball");
			else if (rand == 2)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTacklePlayer, "you tackle your opponent");
			else if (rand == 3)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitPlayer, "you bump into your opponent");
//			else if (rand == 4)
//				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitSportsObject, "you bump into ????");
			else if (rand == 4)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, "you hit a ", "wall");
			else if (rand == 5)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, "you hit a ", "goal");
			else if (rand == 6)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerStealBall, "you steal the ball");
			else if (rand == 7)
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitInTheFaceByBall, "your opponent hits you with the ball");
			else
				return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTouchBall, "you touch the ball");
		} else {
			int rand = Random.Range(0, 3);
			if (rand == 0)
//				return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitSportsObject, "the ball bumps into ????");
//			else if (rand == 1)
				return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, "the ball hits a ", "wall");
			else if (rand == 1)
				return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, "the ball hits a ", "goal");
			else
				return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitBall, "the ball bumps into another ball");
		}
	}
	public static GameRuleAction randomAction(bool isComparison, bool playerAction) {
		GameRuleSportsObjectSelector selector = playerAction ?
			randomPlayerSourceSportsObjectSelector() :
			randomBallSourceSportsObjectSelector();
		return new GameRuleAction(selector, randomActionActionForTarget(selector));
	}
	public static GameRuleSportsObjectSelector randomPlayerSourceSportsObjectSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRulePlayerSportsObjectSelector.instance;
		else
			return GameRuleOpponentSportsObjectSelector.instance;
	}
	public static GameRuleSportsObjectSelector randomBallSourceSportsObjectSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRuleBallSportsObjectSelector.instance;
		else
			return GameRuleBallShooterSportsObjectSelector.instance;
	}
	public static GameRuleSportsObjectActionAction randomActionActionForTarget(GameRuleSportsObjectSelector selector) {
		if (selector.targetType() == typeof(Ball))
			return randomBallActionAction();
		else
			return randomPlayerActionAction(false);
	}
	public static GameRuleSportsObjectActionAction randomPlayerActionAction(bool isComparison) {
isComparison = false;
		int rand = Random.Range(0, 3);
		if (rand == 0) {
			int points = Random.Range(-5, 5);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (rand == 1)
			return new GameRuleFreezeSportsObjectActionAction(Random.Range(0.25f, 4.0f));
		else
			return new GameRuleDuplicateSportsObjectActionAction();
	}
	public static GameRuleSportsObjectActionAction randomBallActionAction() {
		int rand = Random.Range(0, 2);
		if (rand == 0)
			return new GameRuleFreezeSportsObjectActionAction(Random.Range(0.25f, 4.0f));
		else
			return new GameRuleDuplicateSportsObjectActionAction();
	}

	// FixedUpdate is called at a fixed rate
	public void FixedUpdate() {
		currentGameRules = this;
		foreach (GameRule rule in rulesList) {
			rule.CheckCondition();
		}
	}
	public void SendEvent(GameRuleEvent gre) {
		currentGameRules = this;
		foreach (GameRule rule in rulesList) {
			rule.SendEvent(gre);
		}
	}

	//General helpers
	public static void offsetRuleDisplayX(GameRule gameRule, float widthoffset) {
		Transform t = gameRule.ruleDisplay.transform;
		Vector3 position = t.localPosition;
		position.x += widthoffset;
		t.localPosition = position;
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	public GameRuleCondition condition = null;
	public GameRuleAction action = null;
	public GameObject ruleDisplay;
	public GameRule(GameRuleCondition c, GameRuleAction a, GameObject rd) {
		condition = c;
		action = a;
		ruleDisplay = rd;
	}
	public void CheckCondition() {
		//receive a list of all players that triggered the condition
		List<TeamPlayer> triggeringPlayers = new List<TeamPlayer>();
		condition.checkCondition(triggeringPlayers);
		foreach (TeamPlayer tp in triggeringPlayers) {
			action.takeAction(tp);
		}
	}
	public void SendEvent(GameRuleEvent gre) {
		if (condition.conditionHappened(gre)) {
			action.takeAction(gre.getEventSource());
		}
	}
}

////////////////Rule conditions////////////////
public abstract class GameRuleCondition {
	public virtual void checkCondition(List<TeamPlayer> triggeringPlayers) {}
	public virtual bool conditionHappened(GameRuleEvent gre) {return false;}
}

////////////////Conditions that trigger actions when checked////////////////
public class GameRuleComparisonCondition : GameRuleCondition {
	public GameRuleConditionOperator conditionOperator;
	public GameRuleComparisonCondition(GameRuleConditionOperator grco) {
		conditionOperator = grco;
	}
}

//comparison between a value on a player and a value that may or may not be on a player
public class GameRulePlayerValueComparisonCondition : GameRuleComparisonCondition {
	public GameRulePlayerValue leftGRPV;
	public GameRuleValue rightGRV;
	public GameRulePlayerValueComparisonCondition(GameRulePlayerValue grpvl, GameRuleConditionOperator grco, GameRuleValue grvr) :
		base(grco) {
		leftGRPV = grpvl;
		rightGRV = grvr;
	}
	public override void checkCondition(List<TeamPlayer> triggeringPlayers) {
		foreach (List<TeamPlayer> teamPlayerList in GameRules.currentGameRules.allPlayers) {
			TeamPlayer player = teamPlayerList[0];
			leftGRPV.player = player;
			if (rightGRV is GameRulePlayerValue)
				((GameRulePlayerValue)(rightGRV)).player = player.opponent;
			if (conditionOperator.compare(leftGRPV, rightGRV))
				triggeringPlayers.Add(player);
		}
	}
	public override string ToString() {
		return GameRulePlayerSportsObjectSelector.possessivePrefix +
			leftGRPV.ToString() +
			conditionOperator.ToString() +
			((rightGRV is GameRulePlayerValue) ? GameRuleOpponentSportsObjectSelector.possessivePrefix : "") +
			rightGRV.ToString();
	}
}

////////////////Operators to compare game rule values////////////////
public delegate bool GameRuleValueComparison(GameRuleValue left, GameRuleValue right);
public class GameRuleConditionOperator {
	public GameRuleValueComparison compare;
	public string compareString;
	public GameRuleConditionOperator(GameRuleValueComparison grvc, string s) {
		compare = grvc;
		compareString = s;
	}
	public override string ToString() {
		return compareString;
	}

	////////////////Boolean comparisons between two values////////////////
	public static GameRuleConditionOperator lessThanOperator = new GameRuleConditionOperator(lessThan, " < ");
	public static bool lessThan(GameRuleValue left, GameRuleValue right) {
		return left.intValue() < right.intValue();
	}
	public static GameRuleConditionOperator greaterThanOperator = new GameRuleConditionOperator(greaterThan, " > ");
	public static bool greaterThan(GameRuleValue left, GameRuleValue right) {
		return left.intValue() > right.intValue();
	}
	public static GameRuleConditionOperator lessOrEqualOperator = new GameRuleConditionOperator(lessOrEqual, " <= ");
	public static bool lessOrEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() <= right.intValue();
	}
	public static GameRuleConditionOperator greaterOrEqualOperator = new GameRuleConditionOperator(greaterOrEqual, " >= ");
	public static bool greaterOrEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() >= right.intValue();
	}
	public static GameRuleConditionOperator intEqualOperator = new GameRuleConditionOperator(intEqual, " = ");
	public static bool intEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() == right.intValue();
	}
	public static GameRuleConditionOperator intNotEqualOperator = new GameRuleConditionOperator(intNotEqual, " != ");
	public static bool intNotEqual(GameRuleValue left, GameRuleValue right) {
		return left.intValue() != right.intValue();
	}
}

////////////////Values for use of comparing////////////////
public abstract class GameRuleValue {
	public virtual int intValue() {return 0;}
}

public class GameRuleIntConstantValue : GameRuleValue {
	public int val;
	public GameRuleIntConstantValue(int v) {
		val = v;
	}
	public override int intValue() {return val;}
	public override string ToString() {return val.ToString();}
}

////////////////Values on players for use of comparing////////////////
public abstract class GameRulePlayerValue : GameRuleValue {
	//this gets set before the values are computed
	public TeamPlayer player;
}

public class GameRulePlayerScoreValue : GameRulePlayerValue {
	public override int intValue() {
		return GameRules.currentGameRules.teamScores[player.team];
	}
	public override string ToString() {
		return "score";
	}
}


////////////////Conditions that trigger actions when an event happen////////////////
public class GameRuleEventHappenedCondition : GameRuleCondition {
	public GameRuleEventType eventType;
	public string conditionString;
	public string param;
	public GameRuleEventHappenedCondition(GameRuleEventType et, string cs, string p = null) {
		eventType = et;
		conditionString = cs;
		param = p;
	}
	public override bool conditionHappened(GameRuleEvent gre) {
		return gre.eventType == eventType && gre.param == param;
	}
	public override string ToString() {
		return conditionString + param;
	}
}

////////////////Rule consequences////////////////
public class GameRuleAction {
	public GameRuleSportsObjectSelector sportsObjectSelector;
	public GameRuleSportsObjectActionAction innerAction;
	public GameRuleAction(GameRuleSportsObjectSelector sos, GameRuleSportsObjectActionAction ia) {
		sportsObjectSelector = sos;
		innerAction = ia;
	}
	public override string ToString() {
		return sportsObjectSelector.ToString() + " " + innerAction.ToString(sportsObjectSelector.conjugate);
	}
	public void takeAction(SportsObject source) {
		innerAction.takeAction(sportsObjectSelector.target(source));
	}
}

////////////////Sports object selectors////////////////
public abstract class GameRuleSportsObjectSelector {
	public int conjugate; //for verbs
	public abstract SportsObject target(SportsObject source);
	public abstract System.Type targetType();
}

public abstract class GameRuleSourceSportsObjectSelector : GameRuleSportsObjectSelector {
	public override SportsObject target(SportsObject source) {
		return source;
	}
}

public class GameRulePlayerSportsObjectSelector : GameRuleSourceSportsObjectSelector {
	public static string possessivePrefix = "your ";
	public static GameRulePlayerSportsObjectSelector instance = new GameRulePlayerSportsObjectSelector();
	public GameRulePlayerSportsObjectSelector() {
		conjugate = 0;
	}
	public override string ToString() {
		return "you";
	}
	public override System.Type targetType() {
		return typeof(TeamPlayer);
	}
}

public class GameRuleOpponentSportsObjectSelector : GameRuleSportsObjectSelector {
	public static string possessivePrefix = "your opponent's ";
	public static GameRuleOpponentSportsObjectSelector instance = new GameRuleOpponentSportsObjectSelector();
	public GameRuleOpponentSportsObjectSelector() {
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

public class GameRuleBallShooterSportsObjectSelector : GameRuleSportsObjectSelector {
	public static GameRuleBallShooterSportsObjectSelector instance = new GameRuleBallShooterSportsObjectSelector();
	public GameRuleBallShooterSportsObjectSelector() {
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

public class GameRuleBallSportsObjectSelector : GameRuleSourceSportsObjectSelector {
	public static GameRuleBallSportsObjectSelector instance = new GameRuleBallSportsObjectSelector();
	public GameRuleBallSportsObjectSelector() {
		conjugate = 1;
	}
	public override string ToString() {
		return "the ball";
	}
	public override System.Type targetType() {
		return typeof(Ball);
	}
}

////////////////The actual functionality to affect players and sports objects////////////////
public abstract class GameRuleSportsObjectActionAction {
	public abstract void takeAction(SportsObject so);
	public abstract string ToString(int conjugate);
}

public abstract class GameRulePlayerActionAction : GameRuleSportsObjectActionAction {
	public override void takeAction(SportsObject so) {
		takeAction((TeamPlayer)(so));
	}
	public abstract void takeAction(TeamPlayer tp);
}

public class GameRulePointsPlayerActionAction : GameRulePlayerActionAction {
	public static string[] gainConjugates = new string[] {"gain ", "gains "};
	public static string[] loseConjugates = new string[] {"lose ", "loses "};
	public int pointsGiven;
	public GameRulePointsPlayerActionAction(int pg) {
		pointsGiven = pg;
	}
	public override void takeAction(TeamPlayer tp) {
		tp.ScorePoints(pointsGiven);
	}
	public override string ToString(int conjugate) {
		string pluralPointString = Mathf.Abs(pointsGiven) == 1 ? " point" : " points";
		return pointsGiven >= 0 ?
			gainConjugates[conjugate] + pointsGiven.ToString() + pluralPointString :
			loseConjugates[conjugate] + (-pointsGiven).ToString() + pluralPointString;
	}
}

public class GameRuleFreezeSportsObjectActionAction : GameRuleSportsObjectActionAction {
	public static string[] freezeConjugates = new string[] {"freeze ", "freezes "};
	public float timeFrozen;
	public GameRuleFreezeSportsObjectActionAction(float tf) {
		timeFrozen = tf;
	}
	public override void takeAction(SportsObject so) {
		so.Freeze(timeFrozen);
	}
	public override string ToString(int conjugate) {
		return freezeConjugates[conjugate] + "for " + timeFrozen.ToString("F1") + " seconds";
	}
}

public class GameRuleDuplicateSportsObjectActionAction : GameRuleSportsObjectActionAction {
	public static string[] duplicateConjugates = new string[] { "get ", "gets " };
	public override void takeAction(SportsObject so) {
		so.Duplicate(1);
	}
	public override string ToString(int conjugate) {
		return duplicateConjugates[conjugate] + "duplicated";
	}
}
