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
	public static GameRuleEventHappenedCondition randomEventHappenedCondition() {
		//2/3 player, 1/3 ball
		if (Random.Range(0, 3) < 2)
			return randomPlayerEventHappenedCondition(GameRulePlayerSelector.instance);
		else
			return randomBallEventHappenedCondition(GameRuleBallSelector.instance);
	}
	public static GameRuleEventHappenedCondition randomPlayerEventHappenedCondition(GameRuleSelector selector) {
		int rand = Random.Range(0, 9);
		if (rand == 0)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerShootBall, selector, " shoot the ball");
		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerGrabBall, selector, " grab the ball");
		else if (rand == 2)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTacklePlayer, selector, " tackle your opponent");
		else if (rand == 3)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitPlayer, selector, " bump into your opponent");
//		else if (rand == 4)
//			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitSportsObject, selector, " bump into ????");
		else if (rand == 4)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, selector, " hit a ", "wall");
		else if (rand == 5)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitFieldObject, selector, " hit a ", "goal");
		else if (rand == 6)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerStealBall, selector, " steal the ball");
		else if (rand == 7)
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerHitInTheFaceByBall, selector, " get smacked by the ball");
		else
			return new GameRuleEventHappenedCondition(GameRuleEventType.PlayerTouchBall, selector, " touch the ball");
	}
	public static GameRuleEventHappenedCondition randomBallEventHappenedCondition(GameRuleSelector selector) {
		int rand = Random.Range(0, 3);
		if (rand == 0)
//			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitSportsObject, selector, " bumps into ????");
//		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, selector, " hits a ", "wall");
		else if (rand == 1)
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitFieldObject, selector, " hits a ", "goal");
		else
			return new GameRuleEventHappenedCondition(GameRuleEventType.BallHitBall, selector, " bumps into another ball");
	}
	public static GameRuleAction randomAction(bool isComparison, bool playerAction) {
		GameRuleSelector selector = playerAction ?
			randomPlayerSourceSelector() :
			randomBallSourceSelector();
		return new GameRuleAction(selector, randomActionActionForTarget(selector));
	}
	public static GameRuleSelector randomPlayerSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRulePlayerSelector.instance;
		else
			return GameRuleOpponentSelector.instance;
	}
	public static GameRuleSelector randomBallSourceSelector() {
		if (Random.Range(0, 2) == 0)
			return GameRuleBallSelector.instance;
		else
			return GameRuleBallShooterSelector.instance;
	}
	public static GameRuleActionAction randomActionActionForTarget(GameRuleSelector selector) {
		if (selector.targetType() == typeof(Ball))
			return randomBallActionAction();
		else
			return randomPlayerActionAction(false);
	}
	public static GameRuleActionAction randomPlayerActionAction(bool isComparison) {
isComparison = false;
		int rand = Random.Range(0, 4);
		if (rand == 0) {
			int points = Random.Range(-5, 5);
			if (points >= 0)
				points++;
			return new GameRulePointsPlayerActionAction(points);
		} else if (rand == 1)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else if (rand == 2)
			return new GameRuleDuplicateActionAction();
		else
			return new GameRuleFreezeUntilConditionActionAction(
				randomPlayerEventHappenedCondition(
					randomPlayerSourceSelector()));
	}
	public static GameRuleActionAction randomBallActionAction() {
		int rand = Random.Range(0, 2);
		if (rand == 0)
			return new GameRuleFreezeActionAction(Random.Range(0.25f, 4.0f));
		else
			return new GameRuleDuplicateActionAction();
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
