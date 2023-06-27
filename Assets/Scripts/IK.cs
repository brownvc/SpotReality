using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IK : MonoBehaviour
{
    /*private bool triggerWasPressed = false;
    private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion lastHandRotation = Quaternion.identity;*/

    private GameObject rightController;
    private Vector3 initPosition;
    private Vector3 offset;
    private Vector3 initHandPosition;

    private GameObject armObj;
    private GameObject shoulder;
    private GameObject elbow0;
    private GameObject elbow1;
    private GameObject wrist0;
    private GameObject wrist1;
    private GameObject finger;

    private float l1;
    private float l2;
    private float l3;
    private float l4;
    private float l5;


    void Start()
    {
        rightController = GameObject.Find("RightHand Controller");
        // change to actual finger
        armObj = GameObject.Find("ghost arm");
        shoulder = GameObject.Find("newarm0.link_sh1");
        elbow0 = GameObject.Find("newarm0.link_el0");
        elbow1 = GameObject.Find("newarm0.link_el1");
        wrist0 = GameObject.Find("newarm0.link_wr0");
        wrist1 = GameObject.Find("newarm0.link_wr1");
        finger = GameObject.Find("newarm0.link_fngr");

        l1 = Vector3.Distance(elbow0.transform.position, shoulder.transform.position);
        l2 = Vector3.Distance(elbow0.transform.position, elbow1.transform.position);
        l3 = Vector3.Distance(elbow1.transform.position, wrist0.transform.position);
        l4 = Vector3.Distance(wrist0.transform.position, wrist1.transform.position);
        l5 = Vector3.Distance(wrist1.transform.position, finger.transform.position);

        //dummyFinger.transform.position = GameObject.Find("arm0.link_fngr").transform.position;
        initPosition = finger.transform.position;
        initHandPosition = rightController.transform.position;
        Debug.Log(rightController);
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Include rotation feedback
        // TODO: Add a drag functionality (i.e. when holding button) similar to Move Arm
        var gameControllers = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

        // query current position or offset
        offset = rightController.transform.position - initHandPosition + initPosition;

        armObj.transform.position = offset;

        // TODO: Calculate triangulation within Update function

        //finger.transform.position = offset;
        //wrist1.transform.position = offset;
        //wrist0.transform.position = offset;
        //elbow1.transform.position = offset;
        //elbow0.transform.position = offset;
        //shoulder.transform.position = offset;


        // query current orientations

    }
}
