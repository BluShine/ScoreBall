using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

////////////////Game events////////////////
public enum GameRuleEventType {
	PlayerShootBall,
	PlayerGrabBall,
    PlayerTacklePlayer,
    PlayerHitPlayer,
    PlayerHitObject,
    PlayerHitTrigger,
    PlayerStealBall,
    PlayerHitInTheFaceByBall,
    BallHitObject,
    BallHitTrigger,
    BallHitBall
}

public class GameRuleEvent {
	public TeamPlayer instigator;
    public TeamPlayer victim;
	public GameRuleEventType eventType;
    public Ball ball;
    public Ball secondaryBall;
    public Collider collider; //this can be triggers or objects

	public GameRuleEvent(GameRuleEventType gret, TeamPlayer tp = null, TeamPlayer vct = null,
        Ball bl = null, Ball bl2 = null, Collider col = null) {
		eventType = gret;
		instigator = tp;
        victim = vct;
        ball = bl;
        secondaryBall = bl2;
        collider = col;
	}
}

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	List<GameRule> rulesList = new List<GameRule>();
	public GameObject ruleDisplayPrefab;
	public GameObject uiCanvas;

    public List<Text> teamTexts;

    // Use this for initialization
    void Start () {

	}

	// Update is called once per frame
//	void Update () {
//	
//	}

    public void updateScore()
    {
        TeamPlayer[] players = FindObjectsOfType<TeamPlayer>();
        int[] totalscores = new int[byte.MaxValue];
        foreach(TeamPlayer p in players)
        {
            totalscores[p.team] += p.score;
        }
        for(int i = 0; i < teamTexts.Count; i++)
        {
            teamTexts[i].text = "score: " + totalscores[i];
        }
    }

	public void GenerateNewRule() {
//temporary, for now there is only one rule at a time
while (rulesList.Count > 0) {
Destroy(rulesList[0].ruleDisplay);
rulesList.RemoveAt(0);
}
		GameObject display = (GameObject)Instantiate(ruleDisplayPrefab);
		display.transform.SetParent(uiCanvas.transform);
		display.transform.localPosition = ruleDisplayPrefab.transform.localPosition;
		GameRule rule = new GameRule(
			new GameRuleEventHappenedCondition(GameRuleEventType.PlayerShootBall, "player shoots the ball"),
			new GameRuleAction(delegate(TeamPlayer tp) {tp.score += 1;}, "player gains a point"),
			display);
		rulesList.Add(rule);
		Transform t = display.transform;
		GameRuleCondition condition = rule.condition;
		t.GetChild(0).gameObject.GetComponent<Text>().text = condition.ToString();
		t.GetChild(1).gameObject.GetComponent<Text>().text = rule.action.ToString();
		t.GetChild(2).gameObject.GetComponent<Text>().text = condition.conditionHappened().ToString();
	}

	public void SendEvent(GameRuleEvent gre) {
		foreach (GameRule rule in rulesList) {
			rule.SendEvent(gre);
		}
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	public GameRuleCondition condition = null;
	public GameRuleAction action = null;
	public GameObject ruleDisplay;
	public GameRule(GameRuleCondition c, GameRuleAction a, GameObject r) {
		condition = c;
		action = a;
		ruleDisplay = r;
	}
	public void SendEvent(GameRuleEvent gre) {
		if (condition.conditionHappened(gre))
			action.takeAction(gre.instigator);
	}
}

public abstract class GameRuleCondition {
	public virtual bool conditionHappened() {return false;}
	public virtual bool conditionHappened(GameRuleEvent gre) {return false;}
}

////////////////Conditions that trigger actions when checked////////////////
public delegate bool GRVComparison(GameRuleValue left, GameRuleValue right);
public class GameRuleComparisonCondition : GameRuleCondition {
	public GRVComparison compare;
	public string compareString = "";
	public GameRuleValue leftGRV;
	public GameRuleValue rightGRV;
	public GameRuleComparisonCondition() {
		leftGRV = new GameRuleIntConstantValue(3);
		rightGRV = new GameRuleIntConstantValue(4);
		compare = lessThan;
		compareString = " < ";
	}
	public override bool conditionHappened() {
		return compare(leftGRV, rightGRV);
	}
	public override string ToString() {
		return "If " + leftGRV.ToString() + compareString + rightGRV.ToString();
	}

	////////////////Boolean comparisons between two values////////////////
	public static bool lessThan(GameRuleValue left, GameRuleValue right) {
		return left.intValue() < right.intValue();
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

////////////////Events that trigger actions when the events happen////////////////
public class GameRuleEventHappenedCondition : GameRuleCondition {
	public GameRuleEventType eventType;
	public string conditionString;
	public GameRuleEventHappenedCondition(GameRuleEventType gret, string s) {
		eventType = gret;
		conditionString = s;
	}
	public override bool conditionHappened(GameRuleEvent gre) {
		return gre.eventType == eventType;
	}
	public override string ToString() {
		return "If " + conditionString;
	}
}

////////////////Rule consequences////////////////
public delegate void GameRuleActionAction(TeamPlayer tp);
public class GameRuleAction {
	public GameRuleActionAction takeAction;
	public string actionString;
	public GameRuleAction(GameRuleActionAction graa, string s) {
		takeAction = graa;
		actionString = s;
	}
	public override string ToString() {
		return "Then " + actionString;
	}
}
