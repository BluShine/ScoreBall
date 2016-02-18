using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameRuleRequiredObject : int {
	Ball,
	SecondBall,

	GoalRequiredObjectStart,
	FootGoal,
	GoalPosts,
	BackboardHoop,
	SmallWall,
	FullGoalWall,
	GoalRequiredObjectEnd,

	ZoneTypeStart,
	BoomerangZone,
	ZoneTypeEnd
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
	public GameObject zonePrefab;

	public Dictionary<GameRuleRequiredObject, List<GameObject>> spawnedObjectsMap =
		new Dictionary<GameRuleRequiredObject, List<GameObject>>();

	//access to interacting with the game world
	public GameObject ruleDisplayPrefab;
	public GameObject pointsTextPrefab;
	public GameObject iconStoragePrefab; //this is shared across scenes, so keep it as just a prefab
    public GameRuleEffectStorage effectStoragePrefab; //same as icon storage, but for effects
	public GameObject uiCanvas;
	public GameObject mainCamera;
	public GameObject floor;
	public InputField ruleEntryField;
	public List<List<TeamPlayer>> allPlayers = new List<List<TeamPlayer>>();
	public Text[] teamTexts;
	public int[] teamScores;
	public bool useRuleIcons = false;

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
	public List<GameRule> effectRulesList = new List<GameRule>();
	public List<GameRule> metaRulesList = new List<GameRule>();
	public Dictionary<System.Type, List<GameRule>> rulesDict = new Dictionary<System.Type, List<GameRule>>();
	public List<GameRuleActionWaitTimer> waitTimers = new List<GameRuleActionWaitTimer>();
	const float NEW_RULE_WAIT_TIME = 3.0f; //keep this at least as big as the complete new rule animation
	[HideInInspector]
	public float lastRuleChange = -NEW_RULE_WAIT_TIME; //so that we can immediately generate a new rule

	public void Start() {
        soundSource = GetComponent<AudioSource>();
		instance = this;
		rulesDict[typeof(GameRuleEffectAction)] = effectRulesList;
		rulesDict[typeof(GameRuleMetaRuleAction)] = metaRulesList;
		Instantiate(iconStoragePrefab);
        Instantiate(effectStoragePrefab);
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
	public void GenerateNewRuleFromButton() {
		GenerateNewRule();
	}
	public void GenerateNewRule(List<GameRuleRestriction> optionalRestrictions = null, string ruleString = null) {
		//don't generate a rule if the rules were recently changed
		//only 3 rules for now
		if (ruleChangeIsOnCooldown() || rulesList.Count >= 3)
			return;
		//if we're using rule icons, make sure that the icon storage has loaded
		if (useRuleIcons && GameRuleIconStorage.instance == null)
			return;

		lastRuleChange = Time.realtimeSinceStartup;

        //play sound
        soundSource.clip = addRuleSound;
        soundSource.Play();

		GameRule rule;
		if(ruleString != null)
			rule = GameRuleDeserializer.unpackStringToRule(ruleString);
		//build a rule from the inputted string
		else if (ruleEntryField.text.Length > 0) {
			rule = GameRuleDeserializer.unpackStringToRule(ruleEntryField.text);
			ruleEntryField.text = "";
		} else
			rule = GameRuleGenerator.GenerateNewRule(optionalRestrictions);
		rulesList.Add(rule);
		rulesDict[rule.action.GetType()].Add(rule);

		//unity has no good way of giving us what we clicked on, so we have to remember it here
		getButtonFromRuleDisplay(rule.ruleDisplay).onClick.AddListener(() => {this.DeleteRule(rule);});

		addRequiredObjects();

		//if the rule condition tracks a zone, give it its zone to track
		if (rule.condition is GameRuleZoneCondition) {
			GameRuleZoneCondition zoneCondition = ((GameRuleZoneCondition)(rule.condition));
			zoneCondition.conditionZone = spawnedObjectsMap[zoneCondition.zoneType][0].GetComponent<Zone>();
		}

        //update music
        musicPlayer.setTrackCount(rulesList.Count);
	}
	public GameObject generateNewRuleDisplay() {
		GameObject display = (GameObject)Instantiate(ruleDisplayPrefab);
		display.transform.SetParent(uiCanvas.transform);
		return display;
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
				rulesDict[ruleToDelete.action.GetType()].Remove(ruleToDelete);
			} else {
				gameRule.animationStartTime = Time.realtimeSinceStartup;
				gameRule.animationState = 4;
				gameRule.targetPosition = (gameRule.startPosition = gameRule.ruleDisplay.transform.localPosition);
				gameRule.targetPosition.x += widthoffset;
				i++;
			}

			//cancel any wait timers associated with this rule
			if (gameRule.action is GameRuleEffectAction) {
				GameRuleEffect innerEffect = ((GameRuleEffectAction)(gameRule.action)).innerEffect;
				if (innerEffect is GameRuleDurationEffect) {
					GameRuleActionDuration duration = ((GameRuleDurationEffect)(innerEffect)).duration;
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
		}
		Destroy(ruleToDelete.ruleDisplay);
		deleteRequiredObjects();
		lastRuleChange = Time.realtimeSinceStartup;

        //update music
        musicPlayer.setTrackCount(rulesList.Count);
	}

    public void deleteRuleByIndex(int index)
    {
        if(index >= 0 && index < rulesList.Count)
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

			//if we don't have the object, we need to spawn one (or many)
			if (!spawnedObjectsMap.ContainsKey(requiredObject)) {
				List<GameObject> spawnedObjects = new List<GameObject>();
				spawnedObjectsMap[requiredObject] = spawnedObjects;

				GameObject prefab = getPrefabForRequiredObject(requiredObject);
				GameObject spawnedObject = (GameObject)Instantiate(prefab);
				spawnedObjects.Add(spawnedObject);

				//these need multiple objects that get assigned to teams
				if (requiredObject > GameRuleRequiredObject.GoalRequiredObjectStart && requiredObject < GameRuleRequiredObject.GoalRequiredObjectEnd) {
					spawnedObject.GetComponent<FieldObject>().setColor(teamColors[2]);
					FieldObject fo = spawnedObject.GetComponent<FieldObject>();
					fo.team = 2;

					//make another one
					spawnedObject = (GameObject)Instantiate(prefab);
					spawnedObjects.Add(spawnedObject);
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
				//zones all use the same prefab but get a different zone type
				} else if (requiredObject > GameRuleRequiredObject.ZoneTypeStart && requiredObject < GameRuleRequiredObject.ZoneTypeEnd) {
					spawnedObject.GetComponent<Zone>().buildZone(requiredObject);
				}
			}
		}
	}
	public void deleteRequiredObjects() {
		List<GameRuleRequiredObject> requiredObjectsList = buildRequiredObjectsList();
		List<GameRuleRequiredObject> requiredObjectsToRemove = new List<GameRuleRequiredObject>();
		foreach (KeyValuePair<GameRuleRequiredObject, List<GameObject>> spawnedObjects in spawnedObjectsMap) {
			GameRuleRequiredObject requiredObject = spawnedObjects.Key;

			//none of the rules require these objects, add it for deletion
			if (!requiredObjectsList.Contains(requiredObject))
				requiredObjectsToRemove.Add(requiredObject);
		}

		//go through the list of objects to remove and remove them
		foreach (GameRuleRequiredObject requiredObjectToRemove in requiredObjectsToRemove) {
			foreach (GameObject objectToRemove in spawnedObjectsMap[requiredObjectToRemove])
				Destroy(objectToRemove);

			spawnedObjectsMap.Remove(requiredObjectToRemove);
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
		else if (requiredObject > GameRuleRequiredObject.ZoneTypeStart && requiredObject < GameRuleRequiredObject.ZoneTypeEnd)
			return zonePrefab;
		else
			throw new System.Exception("Bug: Invalid required object " + requiredObject);
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
		foreach (GameRule rule in effectRulesList) {
			rule.sendEvent(gre);
		}
		//if any of the metarules intercepted, give them a flash
		foreach (GameRule rule in metaRulesList) {
			GameRuleMetaRule metaRule = ((GameRuleMetaRuleAction)(rule.action)).innerMetaRule;
			if (metaRule.lastInterceptionSource != null) {
				rule.startFlash(metaRule.lastInterceptionSource);
				metaRule.lastInterceptionSource = null;
			}
		}

		//check wait timers and remove any if they happen
		//also if both players are frozen, unfreeze players on the team that was frozen longer
		int frozenTeam = 0;
		for (int i = waitTimers.Count - 1; i >= 0; i--) {
			GameRuleActionWaitTimer waitTimer = waitTimers[i];
			if (waitTimer.effect is GameRuleFreezeEffect) {
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
			if (waitTimer.eventHappened(gre))
				waitTimers.RemoveAt(i);
		}
	}
	public SportsObject interceptSelection(SportsObject so) {
		foreach (GameRule metaRule in metaRulesList) {
			if (metaRule.condition.checkCondition(so))
				return ((GameRuleMetaRuleAction)(metaRule.action)).innerMetaRule.interceptSelection(so);
		}
		return so;
	}

	//General helpers
	public static Button getButtonFromRuleDisplay(GameObject ruleDisplay) {
		return ruleDisplay.transform.FindChild("Delete Button").gameObject.GetComponent<Button>();
	}
}

////////////////Represents a single game rule////////////////
public class GameRule {
	const float RULE_FLASH_FADE_SECONDS = 1.5f;
	const float RULE_ICON_SPACING_AMOUNT = 1.0f / 8.0f;

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="c">condition</param>
    /// <param name="a">action</param>
    /// <param name="proxyRule">if true, rule will be created without display, etc.</param>
	public GameRule(GameRuleCondition c, GameRuleAction a, bool proxyRule = false) {
		condition = c;
		action = a;
        if (proxyRule)
            return;

		ruleDisplay = GameRules.instance.generateNewRuleDisplay();
		RectTransform t = (RectTransform)ruleDisplay.transform;

		flashImage = t.FindChild("Flash").gameObject.GetComponent<Image>();
		((RectTransform)(flashImage.transform)).sizeDelta = t.sizeDelta;
		animationStartTime = Time.realtimeSinceStartup;
		t.localPosition = startPosition;
		t.localScale = startScale;

		t.FindChild("Save Name").gameObject.GetComponent<Text>().text = GameRuleSerializer.packRuleToString(this);

		Transform tText = t.FindChild("Rule Text");
		Transform tImage = t.FindChild("Rule Icons");

		if (GameRules.instance.useRuleIcons) {
			tText.gameObject.SetActive(false);

			//build out the list of icons to display
			List<GameObject> iconList = new List<GameObject>();
			condition.addIcons(iconList);
			iconList.Add(GameRuleIconStorage.instance.resultsInIcon);
			action.addIcons(iconList);

			//clone our base image object so that we have one per icon (including the base image)
			GameObject parentObject = tImage.FindChild("Image").gameObject;
			List<GameObject> imageObjects = new List<GameObject>();
            float x = 0;
			for (int i = 0; i < iconList.Count; i++) {
				GameObject imageObject = GameObject.Instantiate(iconList[i]);
                //set parent to the parentObject, making sure to use the prefab's local position (not world)
				imageObject.transform.SetParent(parentObject.transform, false);
				imageObjects.Add(imageObject);
                //space out the icons
                RectTransform r = imageObject.GetComponent<RectTransform>();
                r.localPosition = r.localPosition + new Vector3(x, 0, 0);
                x += r.rect.width;
			}
			Debug.Log("If " + condition.ToString() + " => Then " + action.ToString());
		} else {
			tImage.gameObject.SetActive(false);
			tText.GetChild(0).gameObject.GetComponent<Text>().text = "If " + condition.ToString();
			tText.GetChild(1).gameObject.GetComponent<Text>().text = "Then " + action.ToString();
		}
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
		/*
		//receive a list of all players that triggered the condition
		List<SportsObject> triggeringObjects = new List<SportsObject>();
		condition.checkCondition(triggeringObjects);
		if (triggeringObjects.Count > 0) {
			foreach (SportsObject so in triggeringObjects) {
				action.takeAction(so);
			}
			startFlash(triggeringObjects[0]);
		}
		*/
	}
	public void sendEvent(GameRuleEvent gre) {
		if (condition.eventHappened(gre)) {
			if (action is GameRuleEffectAction) {
				GameRuleEffectAction effectAction = (GameRuleEffectAction)action;
				SportsObject target = effectAction.takeAction(gre.getEventSource());
				if (target != null)
					startFlash(target);
			}
		}
	}
	public void startFlash(SportsObject so) {
		flashImage.gameObject.SetActive(true);
		Color targetColor = GameRules.instance.teamColors[so.team];
		flashImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1.0f);
	}
	public void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		//only conditions generate required objects but an effect action may have an inner until-condition effect
		condition.addRequiredObjects(requiredObjectsList);
		if (action is GameRuleEffectAction)
			((GameRuleEffectAction)(action)).innerEffect.addRequiredObjects(requiredObjectsList);
	}
	public void packToString(GameRuleSerializer serializer) {
		condition.packToString(serializer);
		action.packToString(serializer);
	}
	public static GameRule unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleCondition condition = GameRuleCondition.unpackFromString(deserializer);
		GameRuleAction action = GameRuleAction.unpackFromString(deserializer);
		return new GameRule(condition, action);
	}
}
