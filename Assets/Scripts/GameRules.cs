using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameRuleRequiredObject {
	Ball,
	Goal,
	SecondBall
};

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	//so that we have access to scores and stuff when evaluating rules
	public static GameRules instance;

	//prefabs for spawning objects
	public GameObject ballPrefab;
	public GameObject bigBallPrefab;
	public GameObject goalPrefab;
	public Material[] teamMaterials;
	public Dictionary<GameObject, GameObject> spawnedObjectPrefabMap = new Dictionary<GameObject, GameObject>();

	//access to interacting with the game world
	public GameObject ruleDisplayPrefab;
	public GameObject pointsTextPrefab;
	public GameObject uiCanvas;
	public GameObject mainCamera;
	public GameObject floor;
	public List<List<TeamPlayer>> allPlayers = new List<List<TeamPlayer>>();
	public Text[] teamTexts;
	public int[] teamScores;

	//constants for positioning the points text above the player and fading out
	//const int POINTS_TEXT_POOL_AMOUNT = 8;
	const float POINTS_TEXT_CAMERA_UP_SPAWN_MULTIPLIER = 3.0f;
	const float POINTS_TEXT_CAMERA_UP_DRIFT_MULTIPLIER = 0.03f;
	const float POINTS_TEXT_FADE_SECONDS = 1.5f;
	Stack<TextMesh> pointsTextPool = new Stack<TextMesh>();
	Queue<TextMesh> activePointsTexts = new Queue<TextMesh>();

	//active rules
	public List<GameRule> rulesList = new List<GameRule>();
	public List<GameRuleActionWaitTimer> waitTimers = new List<GameRuleActionWaitTimer>();

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
		foreach (List<TeamPlayer> teamPlayerList in allPlayers) {
			//in case the team numbers are not consecutive
			if (teamPlayerList == null)
				continue;

			int totalscore = 0;
			foreach (TeamPlayer p in teamPlayerList)
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

		addRequiredObjects();
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
			if (innerAction is GameRuleDurationActionAction) {
				GameRuleActionDuration duration = ((GameRuleDurationActionAction)(innerAction)).duration;
				if (duration is GameRuleActionUntilConditionDuration) {
					GameRuleEventHappenedCondition untilCondition =
						((GameRuleActionUntilConditionDuration)(duration)).untilCondition;
					//loop through all the wait timers and cancel their actions if they have this rule
					for (int j = waitTimers.Count - 1; j >= 0; j--) {
						if (waitTimers[j].condition == untilCondition) {
							waitTimers[j].cancelAction();
							waitTimers.RemoveAt(j);
						}
					}
				}
			}
		}
		Destroy(ruleDisplay);
		deleteRequiredObjects();
	}
    public void deleteAllRules()
    {
        for (int i = rulesList.Count - 1; i >= 0; i--)
        {
			DeleteRule(getButtonFromRuleDisplay(rulesList[i].ruleDisplay));
        }
        rulesList.Clear();
    }
	public void addRequiredObjects() {
		List<GameRuleRequiredObject> requiredObjectsList = buildRequiredObjectsList();
		for (int i = requiredObjectsList.Count - 1; i >= 0; i--) {
			GameRuleRequiredObject requiredObject = requiredObjectsList[i];
			GameObject prefab = getPrefabForRequiredObject(requiredObject);

			//check to see if the object is present
			bool missingObject = true;
			foreach (KeyValuePair<GameObject, GameObject> spawnedObjectPrefab in spawnedObjectPrefabMap) {
				if (spawnedObjectPrefab.Value == prefab) {
					missingObject = false;
					break;
				}
			}

			//we don't have the object, spawn it
			if (missingObject) {
				GameObject spawnedObject = (GameObject)Instantiate(prefab);
				spawnedObjectPrefabMap.Add(spawnedObject, prefab);

				//these need multiple objects that get assigned to teams
				if (requiredObject == GameRuleRequiredObject.Goal) {
					spawnedObject.GetComponent<MeshRenderer>().material = teamMaterials[1];
					FieldObject fo = spawnedObject.GetComponent<FieldObject>();
					fo.team = 1;

					//make another one
					spawnedObject = (GameObject)Instantiate(prefab);
					spawnedObjectPrefabMap.Add(spawnedObject, prefab);
					spawnedObject.GetComponent<MeshRenderer>().material = teamMaterials[2];
					fo = spawnedObject.GetComponent<FieldObject>();
					fo.team = 2;

					//put it on the other side of the field facing the other way
					Transform t = spawnedObject.transform;
					Vector3 v = t.position;
					v.x = -v.x;
					t.position = v;
					Quaternion q = t.rotation;
					q.y = 180.0f;
					t.rotation = q;
				}
			}
		}
	}
	public void deleteRequiredObjects() {
		List<GameRuleRequiredObject> requiredObjectsList = buildRequiredObjectsList();
		List<GameObject> gameObjectsToRemove = new List<GameObject>();
		foreach (KeyValuePair<GameObject, GameObject> spawnedObjectPrefab in spawnedObjectPrefabMap) {
			GameRuleRequiredObject requiredObject = getRequiredObjectForPrefab(spawnedObjectPrefab.Value);

			//go through the required objects list and see if we need this one
			bool objectNotRequired = true;
			foreach (GameRuleRequiredObject otherRequiredObject in requiredObjectsList) {
				if (otherRequiredObject == requiredObject) {
					objectNotRequired = false;
					break;
				}
			}

			//none of the rules require this object, we'll delete it
			if (objectNotRequired)
				gameObjectsToRemove.Add(spawnedObjectPrefab.Key);
		}

		//go through the list of objects to remove and remove them
		foreach (GameObject objectToRemove in gameObjectsToRemove) {
			spawnedObjectPrefabMap.Remove(objectToRemove);
			Destroy(objectToRemove);
		}
	}
	public List<GameRuleRequiredObject> buildRequiredObjectsList() {
		List<GameRuleRequiredObject> requiredObjectsList = new List<GameRuleRequiredObject>();
		foreach (GameRule gameRule in rulesList)
			gameRule.addRequiredObjects(requiredObjectsList);
		return requiredObjectsList;
	}
	public GameObject getPrefabForRequiredObject(GameRuleRequiredObject requiredObject) {
		if (requiredObject == GameRuleRequiredObject.Ball)
			return ballPrefab;
		else if (requiredObject == GameRuleRequiredObject.Goal)
			return goalPrefab;
		else if (requiredObject == GameRuleRequiredObject.SecondBall)
			return bigBallPrefab;
		else
			throw new System.Exception("Invalid required object");
	}
	public GameRuleRequiredObject getRequiredObjectForPrefab(GameObject prefab) {
		if (prefab == ballPrefab)
			return GameRuleRequiredObject.Ball;
		else if (prefab == goalPrefab)
			return GameRuleRequiredObject.Goal;
		else if (prefab == bigBallPrefab)
			return GameRuleRequiredObject.SecondBall;
		else
			throw new System.Exception("Invalid prefab");
	}

	public void spawnPointsText(int pointsGiven, TeamPlayer target) {
		TextMesh pointsText;
		GameObject pointsTextObject;
		//add to the pool if there isn't enough
		if (pointsTextPool.Count == 0) {
			//for (int i = POINTS_TEXT_POOL_AMOUNT; i > 0; i--) {
				pointsTextObject = GameObject.Instantiate(GameRules.instance.pointsTextPrefab);
				pointsTextObject.SetActive(false);
				pointsText = pointsTextObject.GetComponent<TextMesh>();
				pointsTextPool.Push(pointsText);
			//}
		}

		//get one from the pool
		pointsText = pointsTextPool.Pop();
		pointsTextObject = pointsText.gameObject;
		pointsTextObject.SetActive(true);
		activePointsTexts.Enqueue(pointsText);

		//reposition
		Vector3 newPosition = target.transform.position;
		Transform cameraTransform = mainCamera.transform;
		newPosition += cameraTransform.up * POINTS_TEXT_CAMERA_UP_SPAWN_MULTIPLIER;
		pointsText.transform.position = newPosition;
		pointsText.transform.localRotation = cameraTransform.rotation;

		//adjust display
		pointsText.text = pointsGiven >= 0 ? "+" + pointsGiven.ToString() : pointsGiven.ToString();
		Color textColor = GameRules.instance.teamTexts[target.team].color;
		textColor.r *= 0.8f;
		textColor.g *= 0.8f;
		textColor.b *= 0.8f;
		textColor.a = 1.0f;
		pointsText.color = textColor;
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

		//move and fade the points display
		foreach (TextMesh pointsText in activePointsTexts) {
			Vector3 newPosition = pointsText.transform.position;
			newPosition += mainCamera.transform.up * POINTS_TEXT_CAMERA_UP_DRIFT_MULTIPLIER;
			pointsText.transform.position = newPosition;
			Color textColor = pointsText.color;
			textColor.a -= Time.deltaTime / POINTS_TEXT_FADE_SECONDS;
			pointsText.color = textColor;
		}

		//hide all the texts that are invisible
		while (activePointsTexts.Count > 0 && activePointsTexts.Peek().color.a <= 0.0f) {
			TextMesh pointsText = activePointsTexts.Dequeue();
			pointsText.gameObject.SetActive(false);
			pointsTextPool.Push(pointsText);
		}

		//ensure we always have at least 1 rule
		if (rulesList.Count == 0)
			GenerateNewRule();
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
	const float RULE_FLASH_FADE_SECONDS = 1.5f;
	const float RULE_FLASH_MAX_ALPHA = 15.0f / 16.0f;

	public GameRuleCondition condition;
	public GameRuleAction action;
	public GameObject ruleDisplay;
	public Image flashImage;
	public GameRule(GameRuleCondition c, GameRuleAction a, GameObject rd) {
		condition = c;
		action = a;
		ruleDisplay = rd;
		flashImage = rd.transform.FindChild("Flash").gameObject.GetComponent<Image>();
		RectTransform flashSize = ((RectTransform)(flashImage.transform));
		RectTransform ruleDisplaySize = ((RectTransform)(ruleDisplay.transform));
		flashSize.sizeDelta = ruleDisplaySize.sizeDelta;
	}
	public void update() {
		checkCondition();

		//update the flash
		if (flashImage.gameObject.activeSelf) {
			Color flashColor = flashImage.color;
			flashColor.a -= Time.deltaTime / RULE_FLASH_FADE_SECONDS;
			flashImage.color = flashColor;
			if (flashColor.a <= 0.0f)
				flashImage.gameObject.SetActive(false);
		}
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
		flashImage.gameObject.SetActive(true);
        flashImage.color = new Color(flashImage.color.r, flashImage.color.g, flashImage.color.b, 1f);
    }
	public void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		//only conditions generate required objects but an action may have an inner until-condition action
		condition.addRequiredObjects(requiredObjectsList);
		action.innerAction.addRequiredObjects(requiredObjectsList);
	}
}
