using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class VRDriveSpot : MonoBehaviour
{
    public InputActionReference LAx;
    public InputActionReference RAx;
    public RosSharp.RosBridgeClient.MoveSpot drive;

    void Start()
    {

    }

    void Update()
    {
        Vector2 leftMove;
        Vector2 rightMove;

        // Send command to move the robot based on controller joystick values
        rightMove = RAx.action.ReadValue<Vector2>();
        leftMove = LAx.action.ReadValue<Vector2>();
        if (rightMove.x != 0 || leftMove.magnitude != 0)
        {
            // Set movement so only one direction is moved with the left stick at a time
            Debug.Log(leftMove);
            if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
            else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }
            drive.drive(leftMove, rightMove.x);
        }
    }
}
