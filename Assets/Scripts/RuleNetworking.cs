using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Match;

public class RuleNetworking : NetworkBehaviour {

    public GameRules gameRules;
    public NetworkManager manager;
    public NetworkManagerHUD networkHUD;
    public bool isServer = false;
    bool isRegistered = false;

    public GameObject connectedDisplay;

    static short RULEMESSSAGEID = 1324;

	// Use this for initialization
	void Start () {

	}

    void Update()
    {
        if(isServer && !isRegistered)
        {
            if(manager.isNetworkActive && manager.IsClientConnected())
            {
                NetworkServer.RegisterHandler(RULEMESSSAGEID, recieveRule);
                isRegistered = true;
                Debug.Log("ready to recieve messages");
            }
        }
    }

    public void lanHost()
    {
        manager.StartServer();
    }

    public void lanClient()
    {
        manager.StartClient();
    }

    public void matchMakingHost()
    {
        manager.StartMatchMaker();
        manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "", manager.OnMatchCreate);
    }

    public void matchMakingClient()
    {
        manager.StartMatchMaker();
        MatchDesc firstMatch = manager.matches[0];
        manager.matchMaker.JoinMatch(firstMatch.networkId, "", manager.OnMatchJoined);
    }

    public void toggleHUD() {
        networkHUD.enabled = !networkHUD.enabled;
    }
    public void sendRule(string message)
    {
        manager.client.Send(RULEMESSSAGEID, new StringMessage(message));
    }

    void recieveRule(NetworkMessage message)
    {
        string rule = message.ReadMessage<StringMessage>().value;
        Debug.Log("rule recieved: " + rule);
        gameRules.GenerateNewRule(null, rule);
    }
}
