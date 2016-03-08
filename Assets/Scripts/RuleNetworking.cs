using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class RuleNetworking : NetworkBehaviour {

    public GameRules gameRules;
    public NetworkManager manager;
    public NetworkManagerHUD networkHUD;
    public bool isServer = false;
    bool isRegistered = false;

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
