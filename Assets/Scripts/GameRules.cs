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
	public static GameRules instance;

	public void Start() {
		instance = this;
	}
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
		Button deleteButton = getButtonFromRuleDisplay(display);
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

			//cancel any wait timers associated with this rule
			GameRuleActionAction innerAction = gameRule.action.innerAction;
			if (innerAction is GameRuleUntilConditionActionAction) {
				GameRuleEventHappenedCondition untilCondition =
					((GameRuleUntilConditionActionAction)(innerAction)).untilCondition;
				//loop through all the wait timers and cancel their actions if they have this rule
				for (int j = waitTimers.Count - 1; j >= 0; j--) {
					if (waitTimers[i].condition == untilCondition) {
						waitTimers[i].cancelAction();
						waitTimers.RemoveAt(i);
					}
				}
			}
		}
		Destroy(ruleDisplay);
	}

    public void deleteAllRules()
    {
        for(int i = rulesList.Count - 1; i >= 0; i--)
        {
			DeleteRule(getButtonFromRuleDisplay(rulesList[i].ruleDisplay));
        }
        rulesList.Clear();
    }

	// FixedUpdate is called at a fixed rate
	public void FixedUpdate() {
		foreach (GameRule rule in rulesList) {
			rule.update();
		}

        if(Input.GetButtonDown("Submit"))
        {
            GenerateNewRule();
        }
        if(Input.GetButtonDown("Cancel"))
        {
            deleteAllRules();
        }
	}
	public void SendEvent(GameRuleEvent gre) {
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
	public static Button getButtonFromRuleDisplay(GameObject ruleDisplay) {
		return ruleDisplay.transform.FindChild("Delete Button").gameObject.GetComponent<Button>();
	}
	public static void offsetRuleDisplayX(GameRule gameRule, float widthoffset) {
		Transform t = gameRule.ruleDisplay.transform;
		Vector3 position = t.localPosition;
		position.x += widthoffset;
		t.localPosition = position;
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	public GameRuleCondition condition;
	public GameRuleAction action;
	public GameObject ruleDisplay;
	public GameObject flashDisplay;
	public GameRule(GameRuleCondition c, GameRuleAction a, GameObject rd) {
		condition = c;
		action = a;
		ruleDisplay = rd;
		flashDisplay = rd.transform.FindChild("Flash").gameObject;
	}
	public void update() {
		checkCondition();
	}
	public void checkCondition() {
		//receive a list of all players that triggered the condition
		List<TeamPlayer> triggeringPlayers = new List<TeamPlayer>();
		condition.checkCondition(triggeringPlayers);
		if (triggeringPlayers.Count > 0) {
			foreach (TeamPlayer tp in triggeringPlayers) {
				action.takeAction(tp);
			}
			startFlash();
		}
	}
	public void sendEvent(GameRuleEvent gre) {
		if (condition.conditionHappened(gre)) {
			action.takeAction(gre.getEventSource());
			startFlash();
		}
	}
	public void startFlash() {
		Image flashImage = flashDisplay.GetComponent<Image>();
		//this is apparently the way to reset a CrossFadeAlpha
		flashImage.CrossFadeAlpha(0.75f, 0.0f, false);
		flashImage.CrossFadeAlpha(0.0f, 1.0f, false);
		flashImage.enabled = true;
		RectTransform flashSize = ((RectTransform)(flashImage.transform));
		RectTransform ruleDisplaySize = ((RectTransform)(ruleDisplay.transform));
		flashSize.sizeDelta = ruleDisplaySize.sizeDelta;
	}
}
