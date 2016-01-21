using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameRuleRequiredObject {
	Ball,
	SecondBall,

	GoalRequiredObjectStart,
	FootGoal,
	GoalPosts,
	BackboardHoop,
	SmallWall,
	FullGoalWall,
	GoalRequiredObjectEnd
};

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	//so that we have access to scores and stuff when evaluating rules
	public static GameRules instance;

	//prefabs for spawning objects
	public GameObject ballPrefab;
	public GameObject bigBallPrefab;
	public GameObject goalPrefab;
	public GameObject goalPrefab2;
	public GameObject goalPrefab3;
	public GameObject goalPrefab4;
	public GameObject goalPrefab5;

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

    //colors
    public Color[] teamColors;

    //sounds
    public AudioClip addRuleSound;
    public AudioClip removeRuleSound;

    AudioSource soundSource;

    public MusicPlayer musicPlayer;

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
	const float NEW_RULE_WAIT_TIME = 3.0f; //keep this at least as big as the complete new rule animation
	public float lastRuleChange = -NEW_RULE_WAIT_TIME; //so that we can immediately generate a new rule

	public void Start() {
        soundSource = GetComponent<AudioSource>();
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
	public void GenerateNewRule(List<GameRuleRestriction> optionalRestrictions = null) {
		//don't generate a rule if the rules were recently changed
		//only 3 rules for now
		if (ruleChangeIsOnCooldown() || rulesList.Count >= 3)
			return;

		lastRuleChange = Time.realtimeSinceStartup;

        //play sound
        soundSource.clip = addRuleSound;
        soundSource.Play();

		GameObject display = (GameObject)Instantiate(ruleDisplayPrefab);
		display.transform.SetParent(uiCanvas.transform);

		GameRule rule = GameRuleGenerator.GenerateNewRule(display, optionalRestrictions);
		rulesList.Add(rule);

		//unity has no good way of giving us what we clicked on, so we have to remember it here
		getButtonFromRuleDisplay(display).onClick.AddListener(() => {this.DeleteRule(rule);});

		Transform t = display.transform;
		t.GetChild(0).gameObject.GetComponent<Text>().text = "If " + rule.condition.ToString();
		t.GetChild(1).gameObject.GetComponent<Text>().text = "Then " + rule.action.ToString();
		t.GetChild(2).gameObject.GetComponent<Text>().text = GameRuleSerializer.packRuleToString(rule);

		addRequiredObjects();

        //update music
        musicPlayer.setTrackCount(rulesList.Count);
	}
	public void DeleteRule(GameRule ruleToDelete) {
		//don't delete a rule if the rules were recently changed
		if (ruleChangeIsOnCooldown())
			return;

        //play sound
        soundSource.clip = removeRuleSound;
        soundSource.Play();

		float widthoffset = ((RectTransform)(ruleDisplayPrefab.transform)).rect.width * 0.5f;
		for (int i = 0; i < rulesList.Count;) {
			GameRule gameRule = rulesList[i];
			if (gameRule == ruleToDelete) {
				widthoffset = -widthoffset;
				rulesList.RemoveAt(i);
			} else {
				gameRule.animationStartTime = Time.realtimeSinceStartup;
				gameRule.animationState = 4;
				gameRule.targetPosition = (gameRule.startPosition = gameRule.ruleDisplay.transform.localPosition);
				gameRule.targetPosition.x += widthoffset;
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
		Destroy(ruleToDelete.ruleDisplay);
		deleteRequiredObjects();
		lastRuleChange = Time.realtimeSinceStartup;

        //update music
        musicPlayer.setTrackCount(rulesList.Count);
	}

    public void deleteRuleByIndex(int index)
    {
        if(index >= 0 && index < rulesList.Count && rulesList.Count > 0)
        {
            DeleteRule(rulesList[index]);
        }
    }
	public void deleteAllRules() {
		//don't delete the rules if the rules were recently changed
		if (ruleChangeIsOnCooldown())
			return;

        //play sound
        soundSource.clip = removeRuleSound;
        soundSource.Play();

		for (int i = rulesList.Count - 1; i >= 0; i--) {
			DeleteRule(rulesList[i]);

			//reset the time so that we can delete all the rules
			//after this, a new rule will get generated and set it to normal
			lastRuleChange = -NEW_RULE_WAIT_TIME;
		}
		rulesList.Clear();

        //update music
        musicPlayer.setTrackCount(rulesList.Count);
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
				if (requiredObject > GameRuleRequiredObject.GoalRequiredObjectStart && requiredObject < GameRuleRequiredObject.GoalRequiredObjectEnd) {
                    spawnedObject.GetComponent<FieldObject>().setColor(teamColors[2]);
					FieldObject fo = spawnedObject.GetComponent<FieldObject>();
					fo.team = 2;

					//make another one
					spawnedObject = (GameObject)Instantiate(prefab);
					spawnedObjectPrefabMap.Add(spawnedObject, prefab);
                    spawnedObject.GetComponent<FieldObject>().setColor(teamColors[1]);
					fo = spawnedObject.GetComponent<FieldObject>();
					fo.team = 1;

					//put it on the other side of the field facing the other way
					Transform t = spawnedObject.transform;
					Vector3 v = t.position;
					v.x = -v.x;
					t.position = v;
					Quaternion q = t.rotation;
					q *= Quaternion.Euler(Vector3.up * 180);
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
		else if (requiredObject == GameRuleRequiredObject.SecondBall)
			return bigBallPrefab;
		else if (requiredObject == GameRuleRequiredObject.FootGoal)
			return goalPrefab;
		else if (requiredObject == GameRuleRequiredObject.GoalPosts)
			return goalPrefab2;
		else if (requiredObject == GameRuleRequiredObject.BackboardHoop)
			return goalPrefab3;
		else if (requiredObject == GameRuleRequiredObject.SmallWall)
			return goalPrefab4;
		else if (requiredObject == GameRuleRequiredObject.FullGoalWall)
			return goalPrefab5;
		else
			throw new System.Exception("Bug: Invalid required object");
	}
	public GameRuleRequiredObject getRequiredObjectForPrefab(GameObject prefab) {
		if (prefab == ballPrefab)
			return GameRuleRequiredObject.Ball;
		else if (prefab == bigBallPrefab)
			return GameRuleRequiredObject.SecondBall;
		else if (prefab == goalPrefab)
			return GameRuleRequiredObject.FootGoal;
		else if (prefab == goalPrefab2)
			return GameRuleRequiredObject.GoalPosts;
		else if (prefab == goalPrefab3)
			return GameRuleRequiredObject.BackboardHoop;
		else if (prefab == goalPrefab4)
			return GameRuleRequiredObject.SmallWall;
		else if (prefab == goalPrefab5)
			return GameRuleRequiredObject.FullGoalWall;
		else
			throw new System.Exception("Bug: Invalid prefab");
	}
	public bool ruleChangeIsOnCooldown() {
		return Time.realtimeSinceStartup - lastRuleChange < NEW_RULE_WAIT_TIME;
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
		bool animateRules = false;
		foreach (GameRule rule in rulesList) {
			rule.update();
			if (rule.animationState == 2)
				animateRules = true;
		}
		//we have a new rule, we need to tell all panels to slide left and tell the new rule to slide into place
		if (animateRules) {
			RectTransform prefabTransform = (RectTransform)(ruleDisplayPrefab.transform);
			float halfRuleDisplayWidth = prefabTransform.rect.width * 0.5f;
			foreach (GameRule rule in rulesList) {
				rule.animationStartTime = Time.realtimeSinceStartup;
				if (rule.animationState == 2) {
					rule.animationState = 3;
					rule.targetPosition = new Vector3(halfRuleDisplayWidth * (rulesList.Count - 1), prefabTransform.position.y);
				} else {
					rule.animationState = 4;
					rule.targetPosition = (rule.startPosition = rule.ruleDisplay.transform.localPosition);
					rule.targetPosition.x -= halfRuleDisplayWidth;
				}
			}
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
		if (rulesList.Count == 0) {
			//reset the cooldown
			lastRuleChange = -NEW_RULE_WAIT_TIME;

			GenerateNewRule();
		}
	}
	public void SendEvent(GameRuleEvent gre) {
		foreach (GameRule rule in rulesList) {
			rule.sendEvent(gre);
		}

		//check wait timers and remove any if they happen
		//also if both players are frozen, unfreeze players on the team that was frozen longer
		int frozenTeam = 0;
		for (int i = waitTimers.Count - 1; i >= 0; i--) {
			GameRuleActionWaitTimer waitTimer = waitTimers[i];
			if (waitTimer.action is GameRuleFreezeActionAction) {
				//this is the most recent team frozen, it will stay frozen
				if (frozenTeam == 0)
					frozenTeam = waitTimer.target.team;
				//unfreeze all players from the other team(s)
				else if (frozenTeam != waitTimer.target.team) {
					waitTimer.cancelAction();
					waitTimers.RemoveAt(i);
					continue;
				}
			}
			if (waitTimer.conditionHappened(gre))
				waitTimers.RemoveAt(i);
		}
	}

	//General helpers
	public static Button getButtonFromRuleDisplay(GameObject ruleDisplay) {
		return ruleDisplay.transform.FindChild("Delete Button").gameObject.GetComponent<Button>();
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	const float RULE_FLASH_FADE_SECONDS = 1.5f;

	public GameRuleCondition condition;
	public GameRuleAction action;
	public GameObject ruleDisplay;
	public Image flashImage;

	//for rule animations
	const float NEW_RULE_BIG_DISPLAY_SECONDS = 1.5f;
	const float NEW_RULE_TWEEN_SECONDS = 1.5f;
	//0: rule is stationary in its spot on the bottom
	//1: rule is big and new in the middle
	//2: signal for GameRules to advance this rule to 3 and all others to 4
	//3: rule is moving from the middle to its target spot
	//4: rule is sliding over to accomodate a new or deleted rule
	public int animationState = 1;
	public float animationStartTime;
	public Vector3 targetPosition;
	public Vector3 startPosition = new Vector3(0.0f, 0.0f);
	public static Vector3 targetScale = new Vector3(1.0f, 1.0f);
	public static Vector3 startScale = new Vector3(2.0f, 2.0f);

	public GameRule(GameRuleCondition c, GameRuleAction a, GameObject rd) {
		condition = c;
		action = a;
		ruleDisplay = rd;
		flashImage = rd.transform.FindChild("Flash").gameObject.GetComponent<Image>();
		RectTransform flashSize = ((RectTransform)(flashImage.transform));
		RectTransform ruleDisplaySize = ((RectTransform)(ruleDisplay.transform));
		flashSize.sizeDelta = ruleDisplaySize.sizeDelta;
		animationStartTime = Time.realtimeSinceStartup;
		rd.transform.localPosition = startPosition;
		rd.transform.localScale = startScale;
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

		//update the animation
		if (animationState != 0) {
			float timeAnimated = Time.realtimeSinceStartup - animationStartTime;
			switch (animationState) {
				case 1:
					if (timeAnimated >= NEW_RULE_BIG_DISPLAY_SECONDS)
						animationState = 2;
					break;
				case 3:
					ruleDisplay.transform.localScale = Vector3.Lerp(startScale, targetScale, timeAnimated / NEW_RULE_TWEEN_SECONDS);
					goto case 4;
				case 4:
					ruleDisplay.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, timeAnimated / NEW_RULE_TWEEN_SECONDS);
					if (timeAnimated >= NEW_RULE_TWEEN_SECONDS)
						animationState = 0;
					break;
				default:
					throw new System.Exception("Bug: rule should never be in state " + animationState);
			}
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
			startFlash(triggeringPlayers[0]);
		}
	}
	public void sendEvent(GameRuleEvent gre) {
		if (condition.conditionHappened(gre)) {
			SportsObject source = gre.getEventSource();
			action.takeAction(source);
			SportsObject target = action.selector.target(source);
			if (target != null)
				startFlash(target);
		}
	}
	public void startFlash(SportsObject so) {
		flashImage.gameObject.SetActive(true);
		Color targetColor = GameRules.instance.teamColors[so.team];
		flashImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1.0f);
	}
	public void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		//only conditions generate required objects but an action may have an inner until-condition action
		condition.addRequiredObjects(requiredObjectsList);
		action.innerAction.addRequiredObjects(requiredObjectsList);
	}
	public void packToString(GameRuleSerializer serializer) {
		condition.packToString(serializer);
		action.packToString(serializer);
	}
}
