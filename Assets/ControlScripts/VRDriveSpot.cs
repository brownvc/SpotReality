using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class VRDriveSpot : MonoBehaviour
{
    public InputActionReference LAx;
    public InputActionReference RAx;
    public InputActionReference rightPress;
    public InputActionReference leftPress;
    public RosSharp.RosBridgeClient.MoveSpot drive;
    public RawImageSubscriber[] depthSubscribers;
    public JPEGImageSubscriber[] colorSubscribers; // Must be in the same order as depthSubscribers
    public OdometrySubscriber odometrySubscriber;
    public DrawMeshInstanced[] pointClouds;

    private Vector3 lastOdomPos;
    private Quaternion lastOdomRot;
    private Tuple<Vector3, Quaternion>[] origCloudTransforms; // Original location of each point cloud
    private bool[] depthsTempChanged;

    public bool defaultLow;

    private float height;
    private const float HEIGHT_INC = 0.005f;
    private const float HEIGHT_MIN = -0.1f;
    private const float HEIGHT_MAX = 0.3f;

    void Start()
    {
        if (defaultLow)
        {
            height = HEIGHT_MIN;
        }
        else
        {
            height = 0f;
        }
        origCloudTransforms = new Tuple<Vector3, Quaternion>[pointClouds.Length];
        for (int i = 0; i < origCloudTransforms.Length; i++ )
        {
            origCloudTransforms[i] = new Tuple<Vector3, Quaternion>(pointClouds[i].transform.localPosition, pointClouds[i].transform.localRotation);
        }

        depthsTempChanged = new bool[pointClouds.Length];
    }

    void Update()
    {
        Vector2 leftMove;
        Vector2 rightMove;
        Vector3 relativePos;
        Quaternion relativeRot;
        Vector3 newPos;
        Quaternion newRot;
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


        // Read base movement values, adjust speeds
        rightMove = RAx.action.ReadValue<Vector2>() * 0.75f;
        leftMove = LAx.action.ReadValue<Vector2>() * 0.5f;
        leftMove.x *= 0.5f;

        Debug.Log(rightMove);
        Debug.Log(leftMove);

        // Move the robot if any adjustments have been made
        if (rightMove.x != 0f || leftMove.magnitude != 0f || heightChanged)
        {
            // Set movement so only one direction is moved with the left stick at a time
            if (Mathf.Abs(leftMove.x) > Mathf.Abs(leftMove.y)) { leftMove.y = 0; }
            else if (Mathf.Abs(leftMove.y) > Mathf.Abs(leftMove.x)) { leftMove.x = 0; }
            drive.drive(leftMove, rightMove.x, height);

            // Pause depth history for 1.5 seconds
            foreach (RawImageSubscriber ds in depthSubscribers)
            {
                ds.pauseDepthHistory(1.5f);
            }
        }


    }


    private bool vectorEqual(Vector3 a, Vector3 b)
    {
        Vector3 diff;
        float thresh;
        thresh = 0.001f;
        diff = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        diff.x = Math.Abs(diff.x);
        diff.y = Math.Abs(diff.y);
        diff.z = Math.Abs(diff.z);
        if (diff.x > thresh || diff.y > thresh || diff.z > thresh)
        {
            return false;
        }
        return true;
    }

    private bool quatEqual(Quaternion a, Quaternion b)
    {
        Quaternion diff;
        float thresh;
        thresh = 0.001f;
        diff = new Quaternion(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        diff.x = Math.Abs(diff.x);  
        diff.y = Math.Abs(diff.y);
        diff.z = Math.Abs(diff.z);
        diff.w = Math.Abs(diff.w);

        if (diff.x > thresh || diff.y > thresh || diff.z > thresh) // || diff.w > 0.001)
        {
            return false;
        }
        return true;
    }
}
