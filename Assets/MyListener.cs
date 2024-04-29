using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UnityEngine;
using System.Threading;

public class MyListener : MonoBehaviour
{
    Thread thread;
    public int connectionPort = 25001;
    TcpListener server;
    TcpClient client;
    bool running;

    public Transform greenHand;
    public Transform targetTipTransform;
    public Transform goalObj;
    public Transform greenFinger;
    public Transform goalPlane;
    private PolicyGradient pg;

    private bool newResponse;
    private bool newRequest;
    private RLSocketString response;
    private string request;
    private bool read;

    void Start()
    {
        //pg = new PolicyGradient(greenHand, goalObj, 10000, 0, 0, 0);
        newResponse = false;
        newRequest = false;
        read = true;

        pg = new PolicyGradient(greenHand, targetTipTransform, goalObj, greenFinger, goalPlane);


        // Receive on a separate thread so Unity doesn't freeze waiting for data
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start();
    }


    void GetData()
    {
        // Create the server
        server = new TcpListener(IPAddress.Any, connectionPort);
        server.Start();

        // Create a client to get the data stream
        client = server.AcceptTcpClient();

        // Start listening
        running = true;
        while (true)
        {
            Connection();
        }
        server.Stop();
    }

    void Connection()
    {
        // Read data from the network stream
        NetworkStream nwStream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];

        if (read)
        {
            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

            // Decode the bytes into a string
            string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Make sure we're not getting an empty string
            //dataReceived.Trim();
            if (dataReceived != null && dataReceived != "")
            {
                // Convert the received string of data to the format we are using
                response = ParseData(dataReceived);
                newResponse = true;
            }
            read = false;
        }
        if (newRequest)
        {
            // Send a request back
            buffer = Encoding.UTF8.GetBytes(request);
            nwStream.Write(buffer, 0, buffer.Length);
            newRequest = false;
            read = true;
        }
    }

    // Use-case specific function, need to re-write this to interpret whatever data is being sent
    private RLSocketString ParseData(string dataString)
    {
        RLSocketString data = JsonSerializer.Deserialize<RLSocketString>(dataString);
        return data;
    }

    void Update()
    {
        // Process a new response from the thread, then tell the thread what to request next
        if (newResponse)
        {
            if (response.instruction == "step")
            {
                request = pg.handleStep((Action)response.action);
            }
            else if (response.instruction == "reset")
            {
                request = pg.handleReset();
            }
            else if (response.instruction == "init")
            {
                request = pg.handleInit();
            }
            newResponse = false;
            newRequest = true;
        }
    }

    private void OnApplicationQuit()
    {
        thread.Abort();
    }
}

public class RLSocketString
{
    public string instruction { get; set; }
    public int action { get; set; }
}