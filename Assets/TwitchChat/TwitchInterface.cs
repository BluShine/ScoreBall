using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using UnityEngine.UI;

public class TwitchInterface : MonoBehaviour {

    static string SERVER = "irc.twitch.tv";
    static int PORT = 6667;

    /// <summary>
    /// twitch username
    /// </summary>
    static string username = "";
    /// <summary>
    /// twitch oauth login token
    /// go to https://twitchapps.com/tmi/ to generate a token
    /// </summary>
    static string oauthToken = "";
    /// <summary>
    /// channel to connect to: #username (all lowercase)
    /// </summary>
    static string channel = "";

    public InputField usernameField;
    public InputField oauthField; 

    //recieved messages delegate
    public delegate void RecievedMessage(string message);
    public RecievedMessage messageReciever;

    private TcpClient tcpClient;
    private NetworkStream nStream;
    private StreamReader sReader;
    private StreamWriter sWriter;

    bool connected = false;
    public GameObject connectObject;
    public Text errorMessage;

    public struct TwitchChatMessage
    {
        public TwitchChatMessage(string msg, string send) : this()
        {
            message = msg;
            sender = send;
        }
        public string message;
        public string sender;
    }

    // Use this for initialization
    void Start () {
        connectObject.SetActive(false);
        if(oauthToken != "oauth:")
        {
            Connect();
        }
    }

    public void Connect()
    {
        if (connected)
            return;

        if(usernameField.text != "")
            username = usernameField.text.ToLower();
        if(oauthField.text != "")
            oauthToken = oauthField.text;
        channel = "#" + username;
        if (username == "")
        {
            errorMessage.text = "please enter a username";
            return;
        }
        if(oauthToken == "")
        {
            errorMessage.text = "please enter your oauth token";
            return;
        }

        try
        {
            tcpClient = new TcpClient(SERVER, PORT);
        }
        catch (SocketException e)
        {
            errorMessage.text = "network error: " + e.Message;
        }
        nStream = tcpClient.GetStream();
        sReader = new StreamReader(nStream);
        sWriter = new StreamWriter(nStream);

        Write("USER " + username + "tmi twitch :" + username);
        Write("PASS " + oauthToken);
        Write("NICK " + username);
    }

    public void Write(string message)
    {
        sWriter.WriteLine(message);
        sWriter.Flush();
    }
	
	// Update is called once per frame
	void Update () {
        //check for new data
        while (nStream != null && nStream.DataAvailable)
        {
            string data = sReader.ReadLine();
            if (data != null)
                ReadData(data);
        }
	}

    void ReadData(string data)
    {
        string[] splitData = data.Split(' ');
        //find the second colon
        int colonIndex = data.IndexOf(':');
        int secondColonIndex = data.IndexOf(':', colonIndex + 1);
        switch (splitData[1])
        {
            case "PING":
                //respond to pings
                SendMessage("PONG " + splitData[2]);
                break;
            case "NOTICE":
                errorMessage.text = "tmi.twitch.tv: " + data.Substring(secondColonIndex + 1);
                break;
            case "PRIVMSG":
                //get the string after the second colon
                messageReciever(data.Substring(secondColonIndex + 1));
                break;
            case "001":
                Write(("MODE " + username + " +B"));
                Write("JOIN " + channel);
                Write("PRIVMSG " + channel + " :Game connected!");
                connected = true;
                connectObject.SetActive(true);
                errorMessage.text = "connection successful";
                break;
            default:
                break;
        }
    }
}
