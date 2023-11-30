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
    public InputActionReference rightPress;
    public InputActionReference leftPress;
    public RosSharp.RosBridgeClient.MoveSpot drive;

    private float height;
    private const float HEIGHT_INC = 0.005f;
    private const float HEIGHT_MIN = -0.1f;
    private const float HEIGHT_MAX = 0.3f;

    void Start()
    {
        height = 0f;
    }

    void Update()
    {
        Vector2 leftMove;
        Vector2 rightMove;
        bool heightChanged = false;


        // Detect joystick press values
        if (leftPress.action.IsPressed() && (height - HEIGHT_INC) > HEIGHT_MIN)
        {
            height -= HEIGHT_INC;
            heightChanged = true;
        }
        // No else so that if both are pressed, nothing happens
        if (rightPress.action.IsPressed() && (height + HEIGHT_INC) < HEIGHT_MAX)
        {
            height += HEIGHT_INC;
            heightChanged = true;
        }
        

        // Read base movement values
        rightMove = RAx.action.ReadValue<Vector2>() * 0.75f;
        leftMove = LAx.action.ReadValue<Vector2>() * 0.75f;

        // Move the robot if any adjustments have been made
        if (rightMove.x != 0f || leftMove.magnitude != 0f || heightChanged)
        {
            // Set movement so only one direction is moved with the left stick at a time
            if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
            else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }
            drive.drive(leftMove, rightMove.x, height);
        }
    }
}
