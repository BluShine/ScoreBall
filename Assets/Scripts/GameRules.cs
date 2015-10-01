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
		rule.condition = new GameRuleCondition();
		t.GetChild(0).gameObject.GetComponent<Text>().text = rule.condition.ToString();
		rule.action = new GameRuleAction();
		t.GetChild(1).gameObject.GetComponent<Text>().text = rule.action.ToString();
//		outputText.text = rule.ToString();
	}
}

public class GameRule {
	public GameRuleCondition condition;
	public GameRuleAction action;
	public GameRule() {
	}
}

public class GameRuleCondition {
	public GameRuleCondition() {
	}
	public string ToString() {
		return "If hello";
	}
}

public class GameRuleAction {
	public GameRuleAction() {
	}
	public string ToString() {
		return "Then hello";
	}
}
