using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

public class Networking : MonoBehaviour {

    public Transform player;
    public string IP = "127.0.0.1";
    public int port = 7878;
    public int maxConnections = 16;

    public Text outputText;

    int reliableChannelID;
    int socketID;
    int connectionID;

    bool connected = false;

    public Vector3 playerPos;

    public GameObject opponentPrefab;
    Dictionary<int, GameObject> opponents;

	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
        player = FindObjectOfType<Player>().transform;
        opponents = new Dictionary<int, GameObject>();
	}

    public void setIP(string newIP)
    {
        if(newIP != "")
            IP = newIP;
    }

    public void setPort(string newPort)
    {
        int parsePort;
        if (int.TryParse(newPort, out parsePort))
        {
            port = parsePort;
        }
    }

    public void Host()
    {
        //initialize and configure stuff
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        //open up a socket
        socketID = NetworkTransport.AddHost(topology, port);
        outputText.text = "opened socket: " + socketID;
        Debug.Log(outputText.text);
    }
	
	// Update is called once per frame
	void Update () {
        //recieve network events;
        int rSocketID;
        int rConnectionID;
        int rChannelID;
        int rBufferSize = 1024;
        byte[] rBuffer = new byte[rBufferSize];
        int dataSize;
        byte error;
        NetworkEventType rEventType = NetworkTransport.Receive(out rSocketID,
            out rConnectionID, out rChannelID, rBuffer, rBufferSize,
            out dataSize, out error);
        switch (rEventType)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                outputText.text = "connection! socket: " + rSocketID + 
                    " ID: " + rConnectionID + " channel: " + rChannelID;
                Debug.Log(outputText.text);
                //insert a player
                GameObject opp = GameObject.Instantiate(opponentPrefab);
                opponents.Add(rConnectionID, opp);
                break;
            case NetworkEventType.DisconnectEvent:
                outputText.text = "disconnection! socket: " + rSocketID +
                    " ID: " + rConnectionID + " channel: " + rChannelID;
                Debug.Log(outputText.text);
                GameObject.Destroy(opponents[rConnectionID]);
                opponents.Remove(rConnectionID);
                break;
            case NetworkEventType.DataEvent:
                Stream rStream = new MemoryStream(rBuffer);
                BinaryFormatter formatter = new BinaryFormatter();
                float[] rMove = formatter.Deserialize(rStream) as float[];
                outputText.text = "moved: " + player.transform.position;
                Debug.Log(outputText.text);
                opponents[rConnectionID].transform.position = new Vector3(rMove[0], rMove[1], rMove[2]);
                break;
        }
	}

    public void SendMove(Vector3 move)
    {
        if (!connected)
        {
            return;
        }
        byte error;
        int bufferSize = 1024;
        //format the message into a byte array
        byte[] bBuffer = new byte[bufferSize];
        Stream bStream = new MemoryStream(bBuffer);
        BinaryFormatter formatter = new BinaryFormatter();
        float[] moveArr = new float[3];
        moveArr[0] = move.x;
        moveArr[1] = move.y;
        moveArr[2] = move.z;
        formatter.Serialize(bStream, moveArr);

        NetworkTransport.Send(socketID, connectionID, reliableChannelID,
            bBuffer, bufferSize, out error);
    }

    public void Connect()
    {
        byte error;
        connectionID = NetworkTransport.Connect(socketID, IP, port, 0, out error);
        if (error == 0)
        {
            outputText.text = "Connected! ID: " + connectionID;
            Debug.Log(outputText.text);
            connected = true;
        }
        else
        {
            outputText.text = "connection error!";
            Debug.Log(outputText.text);
        }
    }
}
