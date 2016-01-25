using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class RuleNetworking : NetworkBehaviour {

    public GameRules gameRules;

	// Use this for initialization
	void Start () {
	
	}

    public void sendRule(string message)
    {
        CmdRecieveRule(message);
    }

    [Command]
    void CmdRecieveRule(string rule)
    {
        gameRules.GenerateNewRule(null, rule);
    }
}
