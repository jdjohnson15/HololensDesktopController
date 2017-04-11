using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCARDesktopManager : MonoBehaviour
{

    public int incomingPort;
    public int outgoingPort;
    public string targetIP;

    public GameObject[] plinths;

    GameObject oscHandler;

    Globals globals;
    Dictionary<string, ClientLog> clients;
    Dictionary<string, ServerLog> servers;

    int deviceID = 0;

    List<float> args;


    private string myIPAddress;

    // Use this for initialization
    void Start()
    {
        Debug.Log("OSC AR Desktop interface: started");
        // oscHandler = new GameObject("OSCHandler_Plinths");
        //oscHandler.AddComponent<OSCHandler>();

        OSCHandler.Instance.Init("Hololens_Desktop", targetIP, outgoingPort, incomingPort);

        clients = new Dictionary<string, ClientLog>();
        clients = OSCHandler.Instance.Clients;

        servers = new Dictionary<string, ServerLog>();
        servers = OSCHandler.Instance.Servers;

        args = new List<float>();

        for (int i = 0; i < 10; i++)
        {
            args.Add(0f);
        }

        //create table of all plinths and IDs (so the remote devices dont have to know the "name" of the plinth objects in the Unity Editor

        globals = GameObject.Find("Globals").GetComponent<Globals>();
    }

    bool newMessage = true;
    // Update is called once per frame
    void Update()
    {
        //send messags when there are state updates
        if (newMessage)
        {
            OSCHandler.Instance.SendMessageToClient("Hololens_Desktop", "/mouse/" + deviceID, args);
            Debug.Log("OSC Desktop Comm: new status message sent");
            newMessage = false;
        }
    }

    void OnApplicationQuit()
    {

    }


    //format arguments to be sent to remote server (real-world plinth light-control)

    public void setArg(Vector2 point, float click)
    {
        newMessage = true;
        if (args != null)
        {
            args[0] = point.x;
            args[1] = point.y;
            args[2] = click;
        }
            
    }
}