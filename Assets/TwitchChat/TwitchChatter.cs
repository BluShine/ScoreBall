using UnityEngine;
using System.Collections.Generic;

public class TwitchChatter : MonoBehaviour {

    public GameRules gameRules;
    public TwitchInterface twitchInterface;
    public GameObject[] spawnables;
    Dictionary<string, SpawnRef> spawnDict;
    List<GameObject>[] spawnedObjects;
    static int MAXOBJECTSOFTYPE = 20;

    //struct to store a gameobject ref and it's index in 
    struct SpawnRef {
        public SpawnRef(GameObject gObj, int i) : this()
        {
            gameObj = gObj;
            index = i;
        }
        public GameObject gameObj;
        public int index;
    }

    void Start()
    {
        //build spawn dictionary
        spawnDict = new Dictionary<string, SpawnRef>();
        spawnedObjects = new List<GameObject>[spawnables.Length];
        for(int i = 0; i < spawnables.Length; i++)
        {
            spawnDict.Add(spawnables[i].name, new SpawnRef(spawnables[i], i));
            spawnedObjects[i] = new List<GameObject>(MAXOBJECTSOFTYPE);
        }
        //make sure we have gamerules and twitchinterface
        if (gameRules == null)
        {
            gameRules = FindObjectOfType<GameRules>();
        }
        if(twitchInterface == null)
        {
            twitchInterface = FindObjectOfType<TwitchInterface>();
        }
        //set twitch interface event
        twitchInterface.messageReciever += OnMessage;
    } 

	void OnMessage(string message)
    {
        if (message.Contains("add"))
        {
            if (message.Contains("rule"))
            {
                gameRules.GenerateNewRule();
            }
            else
            {
                string key = message.Remove(0, 4);
                if (spawnDict.ContainsKey(key))
                {
                    SpawnRef spr = spawnDict[key];
                    spawnedObjects[spr.index].Add(GameObject.Instantiate(spr.gameObj));
                }
            }
        }
        if(message.Contains("delete"))
        {
            if (message.Contains("rule"))
            {
                gameRules.deleteAllRules();
            } else
            {
                string key = message.Remove(0, 7);
                if (spawnDict.ContainsKey(key))
                {
                    SpawnRef spr = spawnDict[key];
                    Debug.Log(spawnedObjects[spr.index].Count);
                    if (spawnedObjects[spr.index].Count > 0)
                    {
                        GameObject.Destroy(spawnedObjects[spr.index][0]);
                        spawnedObjects[spr.index].RemoveAt(0);
                    }
                }
            }
        }
    }
}
