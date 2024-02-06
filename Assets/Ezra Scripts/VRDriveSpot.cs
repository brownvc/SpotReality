using System;
using System.Collections;
using System.Collections.Generic;
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
    public OdometrySubscriber odometrySubscriber;
    public DrawMeshInstanced[] pointClouds;

    private Vector3 lastOdomPos;
    private Quaternion lastOdomRot;
    private Tuple<Vector3, Quaternion>[] origCloudTransforms; // Original location of each point cloud
    private double lastOdomChangeStamp;
    private bool[] depthsTempChanged;

    

    private float height;
    private const float HEIGHT_INC = 0.005f;
    private const float HEIGHT_MIN = -0.1f;
    private const float HEIGHT_MAX = 0.3f;

    void Start()
    {
        height = 0f;
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


        // Odometry logic
        if (odometrySubscriber != null)
        {
            newPos = odometrySubscriber.PublishedPosition;
            newRot = odometrySubscriber.PublishedRotation;

            if (lastOdomPos == null || lastOdomPos == Vector3.zero)
            {
                lastOdomPos = newPos;
                lastOdomRot = newRot;
            }
            else if (!vectorEqual(lastOdomPos, newPos) || !quatEqual(lastOdomRot, newRot))
            {
                lastOdomChangeStamp = odometrySubscriber.timeStamp;
                //Debug.Log("Depth was frozen at " + lastOdomChangeStamp);

                //Debug.Log(odometrySubscriber.PublishedTransform.position);
                // Get the transform between the two
                relativePos = newPos - lastOdomPos;
                relativeRot = newRot * Quaternion.Inverse(lastOdomRot);

                Debug.Log("Difference in position: " + relativePos + ", difference in rotation: " + relativeRot);

                // Freeze point clouds, and move the them to undo that transform
                foreach (DrawMeshInstanced dmi in pointClouds)
                {
                    dmi.setCloudFreeze(true);
                    dmi.transform.position -= relativePos;
                    dmi.transform.rotation = Quaternion.Inverse(relativeRot) * dmi.transform.rotation;
                }

                // Set transform
                lastOdomPos = newPos;
                lastOdomRot = newRot;

                // Mark that the depth has been changed
                for (int i = 0; i < depthsTempChanged.Length; i++)
                {
                    depthsTempChanged[i] = true;
                }
            }
            else
            {
                for (int i = 0; i < pointClouds.Length; i++)
                {
                    // if depth has been frozen and has been updated more recently than odometry
                    if (depthsTempChanged[i] && depthSubscribers[i].timestamp_synced > lastOdomChangeStamp)
                    {
                        Debug.Log("Depth was unfrozen for subscriber " + (i + 1) + " at " + depthSubscribers[i].timestamp_synced);
                        // Unfreeze the point clouds and move them back to their original relative location
                        pointClouds[i].setCloudFreeze(false);
                        pointClouds[i].transform.localPosition = origCloudTransforms[i].Item1;
                        pointClouds[i].transform.localRotation = origCloudTransforms[i].Item2;
                        depthsTempChanged[i] = false;
                    }
                }
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
