using UnityEngine;
using System.Collections.Generic;

public enum GameRuleRequiredObject: int {
	Ball,
	GrabbableBall,
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

		instance = this;
	}
}

[System.Serializable]
public class GameRuleSpawnableObject {
	public GameObject spawnedObject;
	public GameObject icon;
}
