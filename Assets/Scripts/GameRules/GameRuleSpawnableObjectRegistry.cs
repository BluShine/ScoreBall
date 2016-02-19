using UnityEngine;
using System.Collections.Generic;

public enum GameRuleRequiredObjectType : int {
	AnyBall,
	HoldableBall,
	SecondBall,

	SpecificGoal,

	ZoneTypeStart,
	BoomerangZone,
	ZoneTypeEnd
}

public class GameRuleRequiredObject {
	public GameRuleRequiredObjectType requiredObjectType;
	public string sportName;
	private int hashCode;
	public GameRuleRequiredObject(GameRuleRequiredObjectType rot, string p) {
		requiredObjectType = rot;
		sportName = p;
		hashCode = (int)(requiredObjectType) * (sportName == null ? -1 : sportName.GetHashCode());
	}
	public override int GetHashCode() {
		return hashCode;
	}
	public override bool Equals(object other) {
		if (!(other is GameRuleRequiredObject))
			return false;
		GameRuleRequiredObject otherRequiredObject = (GameRuleRequiredObject)other;
		return requiredObjectType == otherRequiredObject.requiredObjectType &&
			sportName == otherRequiredObject.sportName;
	}
}

public class GameRuleSpawnableObjectRegistry : MonoBehaviour {
	public static GameRuleSpawnableObjectRegistry instance;

	public GameRuleSpawnableObject[] ballSpawnableObjects;
	[HideInInspector]
	public GameRuleSpawnableObject[] holdableBallSpawnableObjects;
	public GameRuleSpawnableObject[] goalSpawnableObjects;

	public void Start() {
		//put balls which can be held into their own list for easy access if we need a holdable ball
		List<GameRuleSpawnableObject> holdableBallSpawnableObjectsList = new List<GameRuleSpawnableObject>();
		for (int i = 0; i < ballSpawnableObjects.Length; i++) {
			GameRuleSpawnableObject ballSpawnableObject = ballSpawnableObjects[i];
			if (ballSpawnableObject.spawnedObject.GetComponent<Ball>().holdable)
				holdableBallSpawnableObjectsList.Add(ballSpawnableObject);
		}
		holdableBallSpawnableObjects = holdableBallSpawnableObjectsList.ToArray();

		//build the standard field objects list
		FieldObject.standardFieldObjects = new List<string>();
		foreach (GameRuleSpawnableObject fieldObject in goalSpawnableObjects)
			FieldObject.standardFieldObjects.Add(fieldObject.spawnedObject.GetComponent<FieldObject>().sportName);
		FieldObject.standardFieldObjects.Add("boundary");

		instance = this;
	}
	public GameObject getPrefabForRequiredObject(GameRuleRequiredObject requiredObject) {
		GameRuleRequiredObjectType requiredObjectType = requiredObject.requiredObjectType;
		if (requiredObjectType == GameRuleRequiredObjectType.AnyBall || requiredObjectType == GameRuleRequiredObjectType.SecondBall)
			return ballSpawnableObjects[Random.Range(0, ballSpawnableObjects.Length)].spawnedObject;
		else if (requiredObjectType == GameRuleRequiredObjectType.HoldableBall)
			return holdableBallSpawnableObjects[Random.Range(0, holdableBallSpawnableObjects.Length)].spawnedObject;
		else if (requiredObjectType == GameRuleRequiredObjectType.BoomerangZone)
			return GameRules.instance.zonePrefab;
		//it's a specific object, get it from the registry
		else
			return findGoalObject(requiredObject.sportName).spawnedObject;
	}
	public GameRuleSpawnableObject findGoalObject(string sportName) {
		for (int i = 0; i < goalSpawnableObjects.Length; i++) {
			GameRuleSpawnableObject spawnableObject = goalSpawnableObjects[i];
			if (spawnableObject.spawnedObject.GetComponent<FieldObject>().sportName == sportName)
				return spawnableObject;
		}
		throw new System.Exception("Bug: could not find spawnable goal " + sportName);
	}
}

[System.Serializable]
public class GameRuleSpawnableObject {
	public GameObject spawnedObject;
	public GameObject icon;
}
