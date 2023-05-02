using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LastRun : MonoBehaviour
{

    public GameObject dummyFinger;
    public GameObject realFinger;
    private int startSecond;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start called");
        dummyFinger.transform.position = realFinger.transform.position;
        //dummyFinger.transform.rotation = realFinger.transform.rotation;
        startSecond = DateTime.Now.Second;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
