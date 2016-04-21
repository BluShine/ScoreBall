using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

////////////////Master rules handler object////////////////
public class GameRules : MonoBehaviour {
	//so that we have access to scores and stuff when evaluating rules
	public static GameRules instance;

	//don't do any rules stuff until all classes we use are ready
	private static bool allClassesAreReady = false;

	//prefabs for spawning objects
	public GameObject zonePrefab;

	public Dictionary<GameRuleRequiredObject, List<GameObject>> spawnedObjectsMap =
		new Dictionary<GameRuleRequiredObject, List<GameObject>>();

	//access to interacting with the game world
	public GameObject ruleDisplayPrefab;
	public GameObject pointsTextPrefab;
	public GameObject dataStoragePrefab; //this is shared across scenes, so keep it as just a prefab
	public RectTransform uiCanvas;
	public GameObject mainCamera;
	public GameObject floor;
	public InputField ruleEntryField;
	public GameObject goalArea;
	public Image countdownClock;
	public Transform quitButton;
	public GameRuleGoalPlacer goalPlacer;
	public List<List<TeamPlayer>> allPlayers = new List<List<TeamPlayer>>();
	public Text[] teamTexts;
	public int[] teamScores;
	public GameObject[] teamStats;
	public bool useRuleIcons = false;

    //colors
    public Color[] teamColors;

    //sounds
    public AudioClip addRuleSound;
    public AudioClip removeRuleSound;

    AudioSource soundSource;

    public MusicPlayer musicPlayer;

	//constants for positioning the points text above the player and fading out
	const float POINTS_TEXT_CAMERA_UP_SPAWN_MULTIPLIER = 3.0f;
	const float POINTS_TEXT_CAMERA_UP_DRIFT_MULTIPLIER = 0.03f;
	const float POINTS_TEXT_FADE_SECONDS = 1.5f;
	const int MAX_ACTIVE_POINTS_TEXTS = 16;
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

	//end-of-game stuff
	public float gameLengthSeconds;
	public GameObject gameStats;
	[HideInInspector]
	public int[] gameStatKicks;
	[HideInInspector]
	public int[] gameStatGrabs;
	[HideInInspector]
	public int[] gameStatBumps;
	[HideInInspector]
	public int[] gameStatSmacks;
	[HideInInspector]
	public float[] gameStatTimeFrozen;
	[HideInInspector]
	public float[] gameStatTimeDizzy;
	[HideInInspector]
	public float[] gameStatTimeBouncy;
	[HideInInspector]
	public int[] gameStatDuplications;
	//[HideInInspector]
	//public float gameStatsFadeAlpha; //store gameStats' alpha so we can lerp to it
	//[HideInInspector]
	//public float gameStatsFadeIndex = 0; //which section of fade-in we're on
	//[HideInInspector]
	//public float gameStatsFadeTimeLastStart; //to guide the fade
	//const float GAME_STATS_OVERLAY_FADE_IN_TIME = 1.0f; //keep this at least as big as the complete new rule animation
	private bool gameOver = false;

	public void Start() {
        soundSource = GetComponent<AudioSource>();
		rulesDict[typeof(GameRuleEffectAction)] = effectRulesList;
		rulesDict[typeof(GameRuleMetaRuleAction)] = metaRulesList;
		Instantiate(dataStoragePrefab);

		gameStatKicks = new int[teamScores.Length];
		gameStatGrabs = new int[teamScores.Length];
		gameStatBumps = new int[teamScores.Length];
		gameStatSmacks = new int[teamScores.Length];
		gameStatTimeFrozen = new float[teamScores.Length];
		gameStatTimeDizzy = new float[teamScores.Length];
		gameStatTimeBouncy = new float[teamScores.Length];
		gameStatDuplications = new int[teamScores.Length];
		//Image gameStatsImage = gameStats.GetComponent<Image>();
		//Color gameStatsImageColor = gameStatsImage.color;
		//gameStatsFadeAlpha = gameStatsImageColor.a;
		//gameStatsImageColor.a = 0.0f;
		//gameStatsImage.color = gameStatsImageColor;

		//set up the goal placer
		goalPlacer = new GameRuleGoalPlacer(this);

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
	public void DeregisterPlayer(TeamPlayer tp) {
		allPlayers[tp.team].Remove(tp);
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
	//unity buttons don't like optional parameters
	public void GenerateNewRuleFromButton() {
		GenerateNewRule();
	}
	public void GenerateNewRule(List<GameRuleRestriction> optionalRestrictions = null, string ruleString = null) {
		//bail out if one of the classes we use isn't ready
		if (!allClassesAreReady) {
			if (GameRuleIconStorage.instance == null ||
				GameRuleEffectStorage.instance == null ||
				GameRuleSpawnableObjectRegistry.instance == null)
				return;
			//actually, they're all ready, we can proceed
			else {
				allClassesAreReady = true;
				//runRuleStringSanityChecks();
			}
		}
		//don't generate a rule if the rules were recently changed
		//only 3 rules for now
		if (ruleChangeIsOnCooldown() || rulesList.Count >= 3)
			return;

		lastRuleChange = Time.realtimeSinceStartup;

        //play sound
        soundSource.clip = addRuleSound;
        soundSource.Play();

		GameRule rule;
		if(ruleString != null)
			rule = GameRuleDeserializer.unpackRuleFromString(ruleString);
		//build a rule from the inputted string
		else if (ruleEntryField.text.Length > 0) {
			rule = GameRuleDeserializer.unpackRuleFromString(ruleEntryField.text);
			ruleEntryField.text = "";
		} else
			rule = GameRuleGenerator.GenerateNewRule(optionalRestrictions);
		rule.buildRuleDisplay();
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
	public void DeleteRule(GameRule ruleToDelete, bool shouldDeleteRequiredObjects = true) {
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
		if (shouldDeleteRequiredObjects)
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
			DeleteRule(rulesList[i], false);

			//reset the time so that we can delete all the rules
			//after this, a new rule will get generated and set it to normal
			lastRuleChange = -NEW_RULE_WAIT_TIME;
		}
		rulesList.Clear();
		deleteRequiredObjects();

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

				GameObject prefab = GameRuleSpawnableObjectRegistry.instance.getPrefabForRequiredObject(requiredObject);
				GameObject spawnedObject = Instantiate(prefab);
				spawnedObjects.Add(spawnedObject);

				GameRuleRequiredObjectType requiredObjectType = requiredObject.requiredObjectType;
				//goals spawn in random locations and need multiple objects that get assigned to teams
				if (requiredObjectType == GameRuleRequiredObjectType.SpecificGoal) {
					FieldObject fo = spawnedObject.GetComponent<FieldObject>();
					fo.setColor(teamColors[2]);
					fo.team = 2;

					//make another one
					spawnedObject = Instantiate(prefab);
					spawnedObjects.Add(spawnedObject);
					FieldObject fo2 = spawnedObject.GetComponent<FieldObject>();
					fo2.setColor(teamColors[1]);
					fo2.team = 1;

					//position them in a valid space
					goalPlacer.positionGoals(fo, fo2);
				//zones all use the same prefab but get a different zone type
				} else if (requiredObjectType == GameRuleRequiredObjectType.BoomerangZone) {
					spawnedObject.GetComponent<Zone>().buildZone(requiredObjectType);
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

			//if we deleted a goal, mark that the goal spaces need to be recalculated
			if (requiredObjectToRemove.requiredObjectType == GameRuleRequiredObjectType.SpecificGoal)
				goalPlacer.goalSpacesAreOutdated = true;
		}
	}
	public List<GameRuleRequiredObject> buildRequiredObjectsList() {
		List<GameRuleRequiredObject> requiredObjectsList = new List<GameRuleRequiredObject>();
		foreach (GameRule gameRule in rulesList)
			gameRule.addRequiredObjects(requiredObjectsList);
		return requiredObjectsList;
	}
	public bool ruleChangeIsOnCooldown() {
		return Time.realtimeSinceStartup - lastRuleChange < NEW_RULE_WAIT_TIME;
	}

	public void spawnPointsText(int pointsGiven, TeamPlayer target) {
		TextMesh pointsText;
		//if we have too many points texts, yank the oldest one from the queue
		if (activePointsTexts.Count >= MAX_ACTIVE_POINTS_TEXTS)
			pointsText = activePointsTexts.Dequeue();
		//we don't have too many points texts, get one from the pool if there's one available
		else if (pointsTextPool.Count > 0) {
			pointsText = pointsTextPool.Pop();
			pointsText.gameObject.SetActive(true);
		//there are no spare points text objects, make a new one
		} else
			pointsText = Instantiate(GameRules.instance.pointsTextPrefab).GetComponent<TextMesh>();

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
		//check if the game is over
		if (gameOver)
			return;
		float gameAmountRemaining = Mathf.Max(0.0f, 1.0f - (Time.timeSinceLevelLoad / gameLengthSeconds));
		countdownClock.GetComponent<Image>().fillAmount = gameAmountRemaining;
		//if we get here, the game just ended
		if (gameAmountRemaining == 0.0f) {
			//fill out all the stats
			teamStats[1].GetComponent<Text>().text = "Score: " + teamScores[1];
			teamStats[2].GetComponent<Text>().text = "Score: " + teamScores[2];
			teamStats[0].GetComponent<Image>().color = teamScores[1] > teamScores[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(0).GetComponent<Text>().text = ": " + gameStatKicks[1];
			teamStats[2].transform.GetChild(0).GetComponent<Text>().text = ": " + gameStatKicks[2];
			teamStats[0].transform.GetChild(0).GetComponent<Image>().color =
				gameStatKicks[1] == gameStatKicks[2] ? teamColors[0] : gameStatKicks[1] > gameStatKicks[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(1).GetComponent<Text>().text = ": " + gameStatGrabs[1];
			teamStats[2].transform.GetChild(1).GetComponent<Text>().text = ": " + gameStatGrabs[2];
			teamStats[0].transform.GetChild(1).GetComponent<Image>().color =
				gameStatGrabs[1] == gameStatGrabs[2] ? teamColors[0] : gameStatGrabs[1] > gameStatGrabs[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(2).GetComponent<Text>().text = ": " + gameStatBumps[1];
			teamStats[2].transform.GetChild(2).GetComponent<Text>().text = ": " + gameStatBumps[2];
			teamStats[0].transform.GetChild(2).GetComponent<Image>().color =
				gameStatBumps[1] == gameStatBumps[2] ? teamColors[0] : gameStatBumps[1] > gameStatBumps[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(3).GetComponent<Text>().text = ": " + gameStatSmacks[1];
			teamStats[2].transform.GetChild(3).GetComponent<Text>().text = ": " + gameStatSmacks[2];
			teamStats[0].transform.GetChild(3).GetComponent<Image>().color =
				gameStatSmacks[1] == gameStatSmacks[2] ? teamColors[0] : gameStatSmacks[1] > gameStatSmacks[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(4).GetComponent<Text>().text = ": " + gameStatTimeFrozen[1].ToString("F2");
			teamStats[2].transform.GetChild(4).GetComponent<Text>().text = ": " + gameStatTimeFrozen[2].ToString("F2");
			teamStats[0].transform.GetChild(4).GetComponent<Image>().color =
				gameStatTimeFrozen[1] == gameStatTimeFrozen[2] ? teamColors[0] : gameStatTimeFrozen[1] > gameStatTimeFrozen[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(5).GetComponent<Text>().text = ": " + gameStatTimeDizzy[1].ToString("F2");
			teamStats[2].transform.GetChild(5).GetComponent<Text>().text = ": " + gameStatTimeDizzy[2].ToString("F2");
			teamStats[0].transform.GetChild(5).GetComponent<Image>().color =
				gameStatTimeDizzy[1] == gameStatTimeDizzy[2] ? teamColors[0] : gameStatTimeDizzy[1] > gameStatTimeDizzy[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(6).GetComponent<Text>().text = ": " + gameStatTimeBouncy[1].ToString("F2");
			teamStats[2].transform.GetChild(6).GetComponent<Text>().text = ": " + gameStatTimeBouncy[2].ToString("F2");
			teamStats[0].transform.GetChild(6).GetComponent<Image>().color =
				gameStatTimeBouncy[1] == gameStatTimeBouncy[2] ? teamColors[0] : gameStatTimeBouncy[1] > gameStatTimeBouncy[2] ? teamColors[1] : teamColors[2];
			teamStats[1].transform.GetChild(7).GetComponent<Text>().text = ": " + gameStatDuplications[1];
			teamStats[2].transform.GetChild(7).GetComponent<Text>().text = ": " + gameStatDuplications[2];
			teamStats[0].transform.GetChild(7).GetComponent<Image>().color =
				gameStatDuplications[1] == gameStatDuplications[2] ? teamColors[0] : gameStatDuplications[1] > gameStatDuplications[2] ? teamColors[1] : teamColors[2];

			//show the stats, put it above all the other rules
			gameStats.SetActive(true);
			gameStats.transform.SetAsLastSibling();
			gameStats.GetComponent<Animator>().Play("Game Over");

			//send the quit button to the front so that we can quit out
			quitButton.SetAsLastSibling();

			gameOver = true;
			return;
		}

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
		if (gameOver)
			return;

		//add to stats
		if (gre.eventType == GameRuleEventType.Kick)
			gameStatKicks[((TeamPlayer)gre.source).team] += 1;
		else if (gre.eventType == GameRuleEventType.Grab)
			gameStatGrabs[((TeamPlayer)gre.source).team] += 1;
		else if (gre.eventType == GameRuleEventType.Smack)
			gameStatSmacks[((TeamPlayer)gre.source).team] += 1;
		else if (gre.eventType == GameRuleEventType.Bump) {
			if (gre.source is TeamPlayer)
				gameStatBumps[((TeamPlayer)gre.source).team] += 1;
			if (gre.target is TeamPlayer)
				gameStatBumps[((TeamPlayer)gre.target).team] += 1;
		}

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
	public static bool derivesFrom(System.Type a, System.Type b) {
		return a == b || a.IsSubclassOf(b);
	}

	//for ensuring rules don't save the wrong string
	public static void runRuleStringSanityChecks() {
		for (byte i = 0; i <= GameRuleSerializationBase.GAME_RULE_FORMAT_CHAR_BIT_MASK; i++) {
			char c = GameRuleSerializer.byteToChar(i);
			byte b = GameRuleDeserializer.charToByte(c);
			if (i != b)
				throw new System.Exception("Bad rule byte " + i + " which becomes char " + c + " which becomes byte " + i);
		}

		const int sanityCheckCount = 256;
		List<GameRuleRestriction> noRestrictions = new List<GameRuleRestriction>();
		for (int i = 0; i < sanityCheckCount; i++) {
			string ruleString = GameRuleSerializer.packRuleToString(GameRuleGenerator.GenerateNewRule(noRestrictions));
			if (!sanityCheckRuleString(ruleString))
				throw new System.Exception("Bad rule string " + ruleString);
		}
		Debug.Log("Successfully ran all byte character checks and " + sanityCheckCount + " rule string sanity checks");
	}
	public static bool sanityCheckRuleString(string ruleString) {
		return GameRuleSerializer.packRuleToString(GameRuleDeserializer.unpackRuleFromString(ruleString)) == ruleString;
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

	public GameRule(GameRuleCondition c, GameRuleAction a) {
		condition = c;
		action = a;
	}
	public void buildRuleDisplay() {
		ruleDisplay = GameObject.Instantiate(GameRules.instance.ruleDisplayPrefab);
		RectTransform t = (RectTransform)ruleDisplay.transform;
		t.SetParent(GameRules.instance.uiCanvas.transform);

		flashImage = t.FindChild("Flash").gameObject.GetComponent<Image>();
		((RectTransform)(flashImage.transform)).sizeDelta = t.sizeDelta;
		animationStartTime = Time.realtimeSinceStartup;
		t.localPosition = startPosition;
		t.localScale = startScale;

		string ruleName = GameRuleSerializer.packRuleToString(this);
		t.FindChild("Save Name").gameObject.GetComponent<Text>().text = ruleName;

		Transform tText = t.FindChild("Rule Text");
		RectTransform tImage = (RectTransform)(t.FindChild("Rule Icons"));

		if (GameRules.instance.useRuleIcons) {
			tText.gameObject.SetActive(false);

			//build out the list of icons to display
			List<GameObject> iconList = new List<GameObject>();
			condition.addIcons(iconList);
			iconList.Add(GameRuleIconStorage.instance.resultsInIcon);
			action.addIcons(iconList);

			List<GameObject> imageObjects = new List<GameObject>();
			//calculate our width so we can scale down icons instead of overflowing.
			float totalWidth = 0;
			for (int i = 0; i < iconList.Count; i++) {
				totalWidth += ((RectTransform)(iconList[i].transform)).rect.width;
			}
			float iconScale = Mathf.Min(1.0f, tImage.rect.width / totalWidth);

			float x = 0.0f;
			for (int i = 0; i < iconList.Count; i++) {
				GameObject imageObject = GameObject.Instantiate(iconList[i]);
				//set parent to the icons container
				imageObject.transform.SetParent(tImage, false);
				imageObjects.Add(imageObject);
				//scale it 
				imageObject.transform.localScale = new Vector3(iconScale, iconScale);
				//space out the icons
				RectTransform r = imageObject.GetComponent<RectTransform>();
				r.localPosition = r.localPosition + new Vector3(x, 0, 0);
				x += r.rect.width * iconScale;
			}
			Debug.Log("If " + condition.ToString() + " => Then " + action.ToString() + " - " + ruleName);
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
					throw new System.Exception("Bug: rule should never be in animation state " + animationState);
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
				SportsObject target = effectAction.takeAction(gre.source);
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
