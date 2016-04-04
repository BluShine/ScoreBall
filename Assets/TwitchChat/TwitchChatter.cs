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
    struct SpawnRef
    {
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
                List<GameRuleRestriction> restrictions = new List<GameRuleRestriction>();
                //result restrictions
                if(message.Contains("scoring") || message.Contains("point") || 
                    message.Contains("score") || message.Contains("positive"))
                {
                    restrictions.Add(GameRuleRestriction.OnlyPositivePointAmounts);
                } else if (message.Contains("punishment") || message.Contains("foul") ||
                    message.Contains("penalty") || message.Contains("negative"))
                {
                    restrictions.Add(GameRuleRestriction.OnlyNegativePointAmounts);
                } else if (message.Contains("fun") || message.Contains("silly") ||
                    message.Contains("weird") || message.Contains("good"))
                {
                    restrictions.Add(GameRuleRestriction.OnlyFunActions);
                }
                
                gameRules.GenerateNewRule(restrictions);
            }
            else
            {
                string key = message.Remove(0, 4);
                int spawns = 1;
                if (message.Contains("apocalypse") || message.Contains("armageddon"))
                {
                    spawns = 20;
                    key = key.Replace("apocalypse", "");
                    key = key.Replace("armageddon", "");
                    key = key.Trim();
                }
                if (spawnDict.ContainsKey(key))
                {
                    for (int i = 0; i < spawns; i++)
                    {
                        SpawnRef spr = spawnDict[key];
                        spawnedObjects[spr.index].Add(GameObject.Instantiate(spr.gameObj));
                    }
                }
            }
        }
        if(message.Contains("delete"))
        {
            if (message.Contains("rule") && message.Length > 12)
            {
                int ruleIndex;
                if(int.TryParse(message.Substring(12), out ruleIndex))
                {
                    gameRules.deleteRuleByIndex(ruleIndex - 1);
                } else
                {
                    gameRules.deleteRuleByIndex(0);
                }
            } else
            {
                string key = message.Remove(0, 7);
                int numToDel = 1;
                if (message.Contains("all") )
                {
                    key = key.Replace("all", "");
                    key = key.Trim();
                    numToDel = MAXOBJECTSOFTYPE;
                }
                if (spawnDict.ContainsKey(key))
                {
                    SpawnRef spr = spawnDict[key];
                    for (int i = 0; i < numToDel; i++)
                    {
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
}
