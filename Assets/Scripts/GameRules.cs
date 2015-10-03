using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameRules : MonoBehaviour {
	GameRule rule = new GameRule();
	public GameObject condition;

	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
//	void Update () {
//	
//	}

	public void GenerateNewRule() {
		Transform t = condition.transform;
		rule.condition = new GameRuleComparisonCondition();
		t.GetChild(0).gameObject.GetComponent<Text>().text = rule.condition.ToString();
		rule.action = new GameRuleAction();
		t.GetChild(1).gameObject.GetComponent<Text>().text = rule.action.ToString();
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	public GameRuleCondition condition = null;
	public GameRuleAction action = null;
	public GameRule() {
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
		return "If " + leftGRV.ToString() + compareString + rightGRV.ToString()
			+ " (" + conditionHappened() + ")";
	}

	////////////////Boolean comparisons between two values////////////////
	public static bool lessThan(GameRuleValue left, GameRuleValue right) {
		Debug.Log(left.intValue() + ", " + right.intValue());
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
