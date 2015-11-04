using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	List<GameRule> rulesList = new List<GameRule>();
	public List<GameRuleActionWaitTimer> waitTimers = new List<GameRuleActionWaitTimer>();
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

		GameRule rule = GameRuleGenerator.GenerateNewRule(display);
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

	// FixedUpdate is called at a fixed rate
	public void FixedUpdate() {
		currentGameRules = this;
		foreach (GameRule rule in rulesList) {
			rule.checkCondition();
		}
	}
	public void SendEvent(GameRuleEvent gre) {
		currentGameRules = this;
		foreach (GameRule rule in rulesList) {
			rule.sendEvent(gre);
		}

		//check wait timers and remove any if they happen
		for (int i = waitTimers.Count - 1; i >= 0; i--) {
			if (waitTimers[i].conditionHappened(gre))
				waitTimers.RemoveAt(i);
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
	public void checkCondition() {
		//receive a list of all players that triggered the condition
		List<TeamPlayer> triggeringPlayers = new List<TeamPlayer>();
		condition.checkCondition(triggeringPlayers);
		foreach (TeamPlayer tp in triggeringPlayers) {
			action.takeAction(tp);
		}
	}
	public void sendEvent(GameRuleEvent gre) {
		if (condition.conditionHappened(gre)) {
			action.takeAction(gre.getEventSource());
		}
	}
}
