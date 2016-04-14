using UnityEngine;
using System.Collections.Generic;

//these next two classes are sort of unrelated to each other but their structure is practically identical
public class GameRuleGoalAreaOpenSpace {
	public float xLo;
	public float xHi;
	public float y;
	public GameRuleGoalAreaOpenSpace(float xl, float xh, float y0) {
		xLo = xl;
		xHi = xh;
		y = y0;
	}
}
public class GameRuleGoalAreaEdge : GameRuleGoalAreaOpenSpace, System.IComparable {
	public bool topEdge;
	public GameRuleGoalAreaEdge(float xl, float xh, float y0, bool t) :
		base(xl, xh, y0) {
		topEdge = t;
	}
	public int CompareTo(object o) {
		GameRuleGoalAreaEdge g = (GameRuleGoalAreaEdge)o;
		if (y != g.y)
			//higher y values come first as up is +y in unity
			return (y > g.y) ? -1 : 1;
		else
			//if they lie on the same y value, handle bottom edges first
			return topEdge ? 1 : -1;
	}
}

//made this its own class so as to not clog up GameRules
public class GameRuleGoalPlacer {
	public GameRules gameRules;
	public Rect goalAreaBounds;
	//the rects represent the bounds that the top-left corner of the goal can go in
	public Dictionary<string, List<Rect>> goalOpenSpacesMap = new Dictionary<string, List<Rect>>();
	public bool goalSpacesAreOutdated = true;
	public GameRuleGoalPlacer(GameRules gr) {
		gameRules = gr;
		//pull out the goal area information
		Transform goalAreaTransform = gr.goalArea.transform;
		Vector3 goalAreaLocalScale = goalAreaTransform.localScale;
		Vector3 goalAreaLocalPosition = goalAreaTransform.localPosition;
		goalAreaBounds = new Rect(
			goalAreaLocalPosition.x - (goalAreaLocalScale.x * 0.5f),
			goalAreaLocalPosition.z - (goalAreaLocalScale.z * 0.5f),
			goalAreaLocalScale.x,
			goalAreaLocalScale.z);
		GameObject.Destroy(gr.goalArea);
	}
	//check the goal area and return any goals that we can spawn that fit
	public List<string> goalsThatFit() {
		recalculateGoalOpenSpaces();

		List<string> goals = new List<string>();
		foreach (KeyValuePair<string, List<Rect>> goalOpenSpaces in goalOpenSpacesMap) {
			if (goalOpenSpaces.Value.Count > 0)
				goals.Add(goalOpenSpaces.Key);
		}

		return goals;
	}
	//when we pick or spawn a goal we make sure the list of spaces that goals can fit is up-to-date
	//when the generator spawns the goal it will spawn it in one of these pre-calculated spaces for the goal
	public void recalculateGoalOpenSpaces() {
		if (!goalSpacesAreOutdated)
			return;

		goalOpenSpacesMap.Clear();

		//first things first, build the list of edges from our current goals
		List<GameRuleGoalAreaEdge> goalEdges = new List<GameRuleGoalAreaEdge>();
		foreach (KeyValuePair<GameRuleRequiredObject, List<GameObject>> spawnedObjects in gameRules.spawnedObjectsMap) {
			//skip it if it's not a goal
			if (spawnedObjects.Key.requiredObjectType != GameRuleRequiredObjectType.SpecificGoal)
				continue;

			//the list has two goals, one of them is the one we want
			foreach (GameObject goal in spawnedObjects.Value) {
				//we only care about the goal on the +x side
				if (goal.transform.position.x < 0.0f)
					continue;

				//find the non-trigger collider
				Collider c = getNonTriggerCollider(goal);

				Bounds b = c.bounds;
				Vector3 min = b.min;
				Vector3 max = b.max;
				//don't bother adding it if it's outside the goal area
				if (min.x < goalAreaBounds.xMax && max.x > goalAreaBounds.xMin) {
					min.x = Mathf.Max(min.x, goalAreaBounds.xMin);
					max.x = Mathf.Min(max.x, goalAreaBounds.xMax);
					goalEdges.Add(new GameRuleGoalAreaEdge(min.x, max.x, max.z, true));
					goalEdges.Add(new GameRuleGoalAreaEdge(min.x, max.x, min.z, false));
				}
			}
		}

		//sort the list of edges
		goalEdges.Sort();

		//add an end edge so that the algorithm automatically ends all the open spaces
		//the start space gets added specially so we don't need a special start edge
		goalEdges.Add(new GameRuleGoalAreaEdge(goalAreaBounds.xMin, goalAreaBounds.xMax, goalAreaBounds.yMin, true));

		//now go through all goal prefabs and determine if they fit
		foreach (GameRuleSpawnableObject spawnableObject in GameRuleSpawnableObjectRegistry.instance.goalSpawnableObjects) {
			GameObject goal = spawnableObject.spawnedObject;
			//find the collider
			Collider c = getNonTriggerCollider(goal);

			//get the collider's size
			//we can't use c.bounds.size because the prefab isn't initialized
			//also get the vector from the top-left corner to the center of the prefab
			Vector3 size;
			if (c is BoxCollider) {
				size = Vector3.Scale(((BoxCollider)c).size, c.gameObject.transform.lossyScale);
			} else if (c is MeshCollider) {
				size = Vector3.Scale(((MeshCollider)c).sharedMesh.bounds.size, c.gameObject.transform.lossyScale);
			} else
				throw new System.Exception("Goal has unexpected collider type " + c.GetType());

			//time for the real algorithm
			//go through all the edges and grow a list of open spaces
			//when the open space hits a top edge, that closes the space, possibly splitting it
			//if the space can fit the goal, add it to our open spaces list and remove it from the open spaces
			List<GameRuleGoalAreaOpenSpace> currentSpaces = new List<GameRuleGoalAreaOpenSpace>();
			currentSpaces.Add(new GameRuleGoalAreaOpenSpace(goalAreaBounds.xMin, goalAreaBounds.xMax, goalAreaBounds.yMax));
			List<Rect> validSpaces = (goalOpenSpacesMap[goal.GetComponent<FieldObject>().sportName] = new List<Rect>());
			foreach (GameRuleGoalAreaEdge nextEdge in goalEdges) {
				//find the edges that this top edge cuts through
				if (nextEdge.topEdge) {
					//overlaps are possible so it may cut multiple open spaces
					//there are six cases of intersection for the new edge vs the saved spaces-
					//-no overlap (too far right or left)
					//-partial overlap (on the left or right, some of the edge is in and some is out)
					//-bisecting overlap (it lies completely in our open space with space on either side)
					//-full overlap (it spans at least the full width of this space)
					for (int i = currentSpaces.Count - 1; i >= 0; i--) {
						GameRuleGoalAreaOpenSpace currentSpace = currentSpaces[i];
						//no overlap (too far right)
						//because we're iterating spaces right to left, we can stop our search
						if (nextEdge.xLo >= currentSpace.xHi)
							break;
						//no overlap (too far left)
						if (nextEdge.xHi <= currentSpace.xLo)
							continue;

						//our space as we know it is coming to an end in some way
						//figure out how big the space is
						float spaceWidth = currentSpace.xHi - currentSpace.xLo;
						float spaceHeight = currentSpace.y - nextEdge.y;
						//add it if the goal fits
						//we may be reusing this space for a new space
						//decrease the space's y so that we don't have any overlap in valid space
						if (size.x < spaceWidth && size.z < spaceHeight) {
							float validSpaceHeight = spaceHeight - size.z;
							validSpaces.Add(new Rect(
								currentSpace.xLo,
								currentSpace.y - validSpaceHeight,
								spaceWidth - size.x,
								validSpaceHeight));
							currentSpace.y -= validSpaceHeight;
						}

						//full overlap, we're just getting rid of the space
						if (nextEdge.xLo <= currentSpace.xLo && nextEdge.xHi >= currentSpace.xHi)
							currentSpaces.RemoveAt(i);
						//partial left overlap, modify the space to be to the right of the edge
						else if (nextEdge.xLo <= currentSpace.xLo)
							currentSpace.xLo = nextEdge.xHi;
						//partial right overlap or bisecting overlap
						else {
							//if it's bisecting, add the right side space first
							if (nextEdge.xHi < currentSpace.xHi)
								currentSpaces.Insert(i + 1, new GameRuleGoalAreaOpenSpace(
									nextEdge.xHi, currentSpace.xHi, currentSpace.y));

							//modify the space to be to the left of the edge
							currentSpace.xHi = nextEdge.xLo;
						}
					}
				//join together the two spaces next to this (if present)
				} else {
					//iterate through the list of spaces that could have a space to the left of it
					for (int i = currentSpaces.Count - 1; i >= 1; i--) {
						GameRuleGoalAreaOpenSpace rightSpace = currentSpaces[i];
						//we've reached the left bound of what we're searching for
						if (rightSpace.xLo <= nextEdge.xHi) {
							//the left edge of the space matches the right side of the edge
							if (rightSpace.xLo == nextEdge.xHi) {
								GameRuleGoalAreaOpenSpace leftSpace = currentSpaces[i - 1];

								//it's possible we have 1 or 2 other right spaces that have the same xLo edge
								//remove any such spaces, the algorithm ensures that they are too skinny to fit the goal
								int oldI = i;
								while (leftSpace.xLo == rightSpace.xLo) {
									i--;

									//there actually aren't any left spaces, just break out
									if (i == 0)
										break;
									leftSpace = currentSpaces[i - 1];
								}

								//we have a left space and it matches the edge too, we can join the two spaces
								//but like the right space, there might be other spaces with the same xHi edge
								//remove them, and like the right edges they will be too skinny to fit the goal
								while (i > 1) {
									GameRuleGoalAreaOpenSpace farLeftSpace = currentSpaces[i - 2];
									//yep, same right edge, so that's the one we actually want, discard the current left edge
									if (farLeftSpace.xHi == leftSpace.xHi) {
										leftSpace = farLeftSpace;
										i--;
									//we have the one we want, we're done
									} else
										break;
								}

								//clear out any spaces that we're not using
								currentSpaces.RemoveRange(i, oldI - i);

								//we're now ready to join the two spaces
								//however, we may need to maintain one or both of those spaces if they're wide enough
								//keep track of which spaces we keep so we know how to make the new space
								if (i > 0 && leftSpace.xHi == nextEdge.xLo) {
									float leftWidth = leftSpace.xHi - leftSpace.xLo;
									float rightWidth = rightSpace.xHi - rightSpace.xLo;

									//we'll be keeping the left space
									if (size.x < leftWidth) {
										//we're keeping both spaces, add a new one between left and right
										if (size.x < rightWidth)
											currentSpaces.Insert(i, new GameRuleGoalAreaOpenSpace(
												leftSpace.xHi - size.x, rightSpace.xLo + size.x, nextEdge.y));
										//we're only keeping the left space, replace the right space
										else {
											rightSpace.xLo = leftSpace.xHi - size.x;
											rightSpace.y = nextEdge.y;
										}
									//we won't be keeping the left space
									} else {
										//we're only keeping the right space, replace the left space
										if (size.x < rightWidth) {
											leftSpace.xHi = rightSpace.xLo + size.x;
											leftSpace.y = nextEdge.y;
										//we won't be keeping either space, replace the left and remove the right
										} else {
											leftSpace.xHi = rightSpace.xHi;
											leftSpace.y = nextEdge.y;
											currentSpaces.RemoveAt(i);
										}
									}
								}
							}
							break;
						}
					}
				}
			}
		}

		goalSpacesAreOutdated = false;
	}
	public static Collider getNonTriggerCollider(GameObject go) {
		foreach (Collider c in go.GetComponentsInChildren<Collider>()) {
			if (!c.isTrigger)
				return c;
		}
		throw new System.Exception("Could not get non-trigger collider for " + go);
	}
	public void positionGoals(FieldObject fo1, FieldObject fo2) {
		//for the first goal we need to place it randomly in one of the valid spaces it can go in
		//make sure our list of valid spaces is up-to-date
		recalculateGoalOpenSpaces();
		List<Rect> validSpaces = goalOpenSpacesMap[fo1.sportName];

		//we can't move it if there are no valid spaces for it
		if (validSpaces.Count > 0) {
			//add up the chances each one has of being picked
			//each space has a chance proportional to its area
			float[] chances = new float[validSpaces.Count];
			float totalChances = 0.0f;
			for (int i = validSpaces.Count - 1; i >= 0; i--) {
				Rect validSpace = validSpaces[i];
				totalChances += (chances[i] = validSpace.width * validSpace.height);
			}
			float chosenSpaceValue = Random.Range(0.0f, totalChances);
			for (int i = chances.Length - 1; i >= 0; i--) {
				if ((chosenSpaceValue -= chances[i]) < 0.0f) {
					//the chosen space is where the top-left corner can go
					//offset it by the collider's offset to convert it to center-center
					Rect chosenSpace = validSpaces[i];
					Bounds b = getNonTriggerCollider(fo1.gameObject).bounds;
					Vector3 p = fo1.transform.position;
					fo1.transform.position = new Vector3(
						Random.Range(chosenSpace.xMin, chosenSpace.xMax) + (p.x - b.min.x),
						p.y,
						Random.Range(chosenSpace.yMin, chosenSpace.yMax) + (p.z - b.max.z)
					);
					break;
				}
			}
		}

		//for the second goal we just put it on the other side of the field facing the other way
		Transform t = fo2.transform;
		t.position = Vector3.Scale(fo1.transform.position, new Vector3(-1.0f, 1.0f, -1.0f));
		t.rotation = t.rotation * Quaternion.Euler(Vector3.up * 180);

		//mark that the goal spaces need to be recalculated
		goalSpacesAreOutdated = true;
	}
}
