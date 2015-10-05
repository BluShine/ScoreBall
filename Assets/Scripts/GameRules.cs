using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameRuleTypes;

namespace GameRuleTypes {
	public enum GameRuleEvent {
		BallShot,
		BallGrabbed
	}
}

public class GameRules : MonoBehaviour {
	List<GameRule> rulesList = new List<GameRule>();
	public GameObject ruleDisplayPrefab;
	public GameObject uiCanvas;

	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
//	void Update () {
//	
//	}

	public void GenerateNewRule() {
//temporary, for now there is only one rule at a time
while (rulesList.Count > 0) {
Destroy(rulesList[0].ruleDisplay);
rulesList.RemoveAt(0);
}
		GameObject display = (GameObject)Instantiate(ruleDisplayPrefab);
		display.transform.SetParent(uiCanvas.transform);
		display.transform.localPosition = ruleDisplayPrefab.transform.localPosition;
		GameRule rule = new GameRule(new GameRuleComparisonCondition(), new GameRuleAction(), display);
		rulesList.Add(rule);
		Transform t = display.transform;
		GameRuleCondition condition = rule.condition;
		t.GetChild(0).gameObject.GetComponent<Text>().text = condition.ToString();
		t.GetChild(1).gameObject.GetComponent<Text>().text = rule.action.ToString();
		t.GetChild(2).gameObject.GetComponent<Text>().text = condition.conditionHappened().ToString();
	}

	public void SendEvent(GameRuleEvent gre) {

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
}

////////////////Conditions that trigger rules////////////////
public abstract class GameRuleCondition {
	abstract public bool conditionHappened();
}

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
	public GameRuleValue() {
	}
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

////////////////Rule consequences////////////////
public class GameRuleAction {
	public GameRuleAction() {
	}
	public override string ToString() {
		return "Then hello";
	}
}
