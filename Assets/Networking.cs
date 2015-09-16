using UnityEngine;
using System.Collections;
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

	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
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
        if (connected && Input.GetButtonDown("Submit"))
        {
            SendPacket("pressed submit!");
        }

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
                break;
            case NetworkEventType.DisconnectEvent:
                outputText.text = "disconnection! socket: " + rSocketID +
                    " ID: " + rConnectionID + " channel: " + rChannelID;
                Debug.Log(outputText.text);
                break;
            case NetworkEventType.DataEvent:
                Stream rStream = new MemoryStream(rBuffer);
                BinaryFormatter formatter = new BinaryFormatter();
                string rMessage = formatter.Deserialize(rStream) as string;
                outputText.text = rSocketID + ": " + rMessage;
                Debug.Log(outputText.text);
                break;
        }
	}

    public void SendPacket(string message)
    {
        byte error;
        int bufferSize = 1024;
        //format the message into a byte array
        byte[] bBuffer = new byte[bufferSize];
        Stream bStream = new MemoryStream(bBuffer);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(bStream, message);

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
