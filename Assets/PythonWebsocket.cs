using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class PythonWebsocket : MonoBehaviour
{
    private WebSocket ws;

    // Start is called before the first frame update
    void Start()
    {
        ws = new WebSocket("ws://127.0.0.1:8081");
        ws.OnMessage += (sender, e) =>
                                  Debug.Log("Laputa says: " + e.Data);
        InvokeRepeating("sendMessage", 1f, 0.2f);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void sendMessage()
    {
        ws.Connect();
        ws.Send("HI THERE");
    }

}
