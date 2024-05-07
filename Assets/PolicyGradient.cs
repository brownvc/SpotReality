using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NumSharp;
using System;
using System.Text.Json;
using RosSharp.RosBridgeClient;

public enum Action
{
    Up,
    Down,
    Left,
    Right,
    Forward,
    Back,
    //RollP, // P = positive
    //RollN, // N = negative
    //YawP,
    //YawN,
    PitchP,
    PitchN,
    OpenGripper,
    CloseGripper,
    NumActions
}

public class PolicyGradient
{
    private bool realArm; // Is this deployed on the real arm
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform agentTransform;
    private Transform agentTipTransform;
    private Transform targetTransform;
    private Transform fingerTransform;
    private Transform goalPlaneTransform;
    private StowArm stowPublisher;
    private SetGripper gripperPublisher;
    private const float OBJDIST = 0.2f; // Distance that gripper can be away from target object
    private const float TERMINALDIST = 0.1f; // Distance that target object can be from 
    public NDArray thetaGlobal;
    public NDArray wGlobal;
    private int currentStep;
    private int lastRolloutStart;
    private int stepsThisRollout;
    public bool useOpt;

    public int featureSize = 14;

    public int actionSize { get; }

    /* Feature trackers */
    private bool gripperOpen; // 1 if gripper open
    private bool holdingObject; // Is the object being held by the gripper?


    public PolicyGradient(Transform agentT, Transform targetTip, Transform goalT, Transform finger, Transform plane, StowArm stowPub, SetGripper gripPub, bool real)
    {
        originalPos = agentT.position;
        originalRot = agentT.rotation;
        agentTransform = agentT;
        agentTipTransform = targetTip;
        targetTransform = goalT;
        goalPlaneTransform = plane;
        stowPublisher = stowPub;
        gripperPublisher = gripPub;
        fingerTransform = finger;
        actionSize = (int)Action.NumActions;
        realArm = real;

        wGlobal = np.zeros(featureSize);
        wGlobal = wGlobal.astype(np.float32);
        currentStep = 0;
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;
        lastRolloutStart = 0;
        useOpt = false;

        reset();
    }

    private NDArray reset()
    {
        // Reset the gripper to its original state, and return current state
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;
        stepsThisRollout = 0;
        holdingObject = false;
        gripperOpen = false;

        //// Randomize target object location
        //var randZ = np.random.uniform(originalPos.z, goalPlaneTransform.position.z);
        //var randX = np.random.uniform(originalPos.x + 0.2f, goalPlaneTransform.position.x);
        //targetTransform.position = new Vector3(randX, targetTransform.position.y, randZ);

        return phi();
    }

    private bool outsideBoundary()
    {
        if (agentTransform.position.z < originalPos.z
            || agentTransform.position.z > targetTransform.position.z + 0.5f
            || agentTransform.position.y > originalPos.y
            || agentTransform.position.y < targetTransform.position.y
            || agentTransform.position.x > originalPos.x + 0.01f
            || agentTransform.position.x < originalPos.x - 0.01f
            )
        {
            return true;
        }
        return false;
    }

    private Vector3 targetPosition()
    {
        // Modify target position by adding to y so we don't go too far above
        return new Vector3(targetTransform.position.x, targetTransform.position.y + 0.02f, targetTransform.position.z);
    }

    private bool angleCorrect()
    {
        // return whether the gripper is at the correct angle
        return agentTransform.rotation.eulerAngles.x > 50 && agentTransform.rotation.eulerAngles.x < 100;
    }


    public void act(Action action)
    {
        float meterMovement = 0.05f;
        float degreeRotation = 5f;
        Vector3 posMov = Vector3.zero;
        Vector3 rotMov = Vector3.zero;
        Vector3 startActPos = agentTransform.position;
        Quaternion startActRot = agentTransform.rotation;

        switch (action)
        {
            case Action.Up:
                posMov = Vector3.up;
                break;
            case Action.Down:
                posMov = Vector3.down;
                break;
            case Action.Left:
                posMov = Vector3.left;
                break;
            case Action.Right:
                posMov = Vector3.right;
                break;
            case Action.Forward:
                posMov = Vector3.forward;
                break;
            case Action.Back:
                posMov = Vector3.back;
                break;
            //case Action.RollP:
            //    rotMov = new Vector3(0, 0, 1);
            //    break;
            //case Action.RollN:
            //    rotMov = new Vector3(0, 0, -1);
            //    break;
            //case Action.YawP:
            //    rotMov = new Vector3(0, 1, 0);
            //    break;
            //case Action.YawN:
            //    rotMov = new Vector3(0, -1, 0);
            //    break;
            case Action.PitchP:
                rotMov = new Vector3(1, 0, 0);
                break;
            case Action.PitchN:
                rotMov = new Vector3(-1, 0, 0);
                break;
            case Action.OpenGripper:
                // Simulate physics of dropping the object
                if (holdingObject)
                {
                    holdingObject = false;
                    targetTransform.position = new Vector3(targetTransform.position.x, goalPlaneTransform.position.y, targetTransform.position.z);
                }
                gripperOpen = true;

                // If real, actually open the gripper
                if (realArm)
                {
                    gripperPublisher.openGripper();
                }
                else
                {
                    fingerTransform.rotation = Quaternion.Euler(270, 0, 0);
                }

                break;
            case Action.CloseGripper:
                // Figure out if we just grabbed the object -- Gripper must have just closed, must be in right angle, and need to be above the object
                if (gripperOpen
                    && 
                    Vector3.Distance(targetPosition(), agentTipTransform.position) < OBJDIST 
                    && angleCorrect()
                    && agentTransform.position.y > targetPosition().y - 0f
                    )
                {
                    holdingObject = true;
                }

                gripperOpen = false;

                // If real, actually close the gripper
                if (realArm)
                {
                    gripperPublisher.closeGripper();
                }
                else
                {
                    fingerTransform.rotation = new Quaternion(0, 0, 0, 0);
                }

                break;
            default:
                break;
        }

        agentTransform.Translate(posMov * meterMovement);
        agentTransform.Rotate(rotMov * degreeRotation);

        // Reset the position to the beginning of this function if we moved outside the boundary
        if (outsideBoundary())
        {
            agentTransform.position = startActPos;
            agentTransform.rotation = startActRot;
        }
        // If not outside the boundary and holding the object, also move the goal object
        //else if (holdingObject)
        //{
        //    targetTransform.Translate(posMov * meterMovement);
        //    targetTransform.Rotate(rotMov * degreeRotation);
        //}

        stepsThisRollout += 1;
    }


    private NDArray phi()
    {
        Vector3 pos = agentTransform.position;
        Quaternion rot = agentTransform.rotation;
        Vector3 goalPos = targetTransform.position;
        Quaternion goalRot = targetTransform.rotation;
        float term = terminal() ? 1f : 0f;

        NDArray phiS = np.array(new float[] {
            pos.x, // uncomment this for randomized goal position
            pos.y,
            pos.z,
            rot.eulerAngles.x / 360,
            rot.eulerAngles.y / 360,
            rot.eulerAngles.z / 360,
            //pos.x*pos.x,
            //pos.y*pos.y,
            //pos.z*pos.z,
            goalPos.x, // uncomment this for randomized goal position
            goalPos.y, // uncomment this for randomized goal position
            goalPos.z, // uncomment this for randomized goal position
            //goalPos.x * goalPos.x,
            //goalPos.y * goalPos.y,
            //goalPos.z * goalPos.z,
            Vector3.Distance(targetPosition(), agentTipTransform.position),
            //rot.eulerAngles.x * rot.eulerAngles.x,
            //rot.eulerAngles.y * rot.eulerAngles.y,
            //rot.eulerAngles.z * rot.eulerAngles.z,
            gripperOpen ? 1f : 0f, // track if gripper open
            holdingObject ? 1f : 0f,
            angleCorrect() ? 1f : 0f,
            term
        }, np.float32);

        // Assert that phiS is equal to the feature size
        if (phiS.shape[0] != featureSize)
        {
            throw new Exception("Need to set featureSize to equal length of features returned from phi()");
        }

        // Return the positions, plus whether terminal
        return phiS;

    }


    private double _reward()
    {
        // TODO factor in total cumulative change in pitch as a negative, to prevent spiraling. try to find a way to penalize moving back and forth less, and focus on spirals
        // TODO scale reward based on if the position and orientation of the agent creates a ray that goes near center of the goal. Scale based on distance between closest point on ray and goal center. 

        float distance = Vector3.Distance(targetPosition(), agentTipTransform.position);

        // Reward if you finished in the right state
        if (holdingObject)
        {
            return 1000;
        }

        var angleMult = angleCorrect() ? 0.5f : 1f;

        if (distance < OBJDIST*2)
        {
            return -0.5 * angleMult;
        }



        //if (agentTransform.rotation.eulerAngles.x > 80 && agentTransform.rotation.eulerAngles.x < 100 && agentTransform.position.y > targetTransform.position.y)
        //{
        //    return 0;
        //}
        //if (terminal())
        //{
        //    return 0;
        //}

        return -2 * angleMult; //-.005 * (currentStep - lastRolloutStart);
    }
    private double reward()
    { 
        float distance = Vector3.Distance(targetTransform.position, agentTipTransform.position);
        double _reward;
        double angle_weight = .01;
        double step_weight = 0.0;
        double z_weight = 100;

        if (holdingObject)
        {
            double angle_diff = Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90));
            double z_diff = Math.Abs(agentTransform.position.z - targetTransform.position.z);
            // Debug.Log("angle: " + angle_diff + ", steps: " + num_steps + ", z_diff: " + z_diff);
            double deductions = (angle_weight * (angle_diff*angle_diff)) + (step_weight * stepsThisRollout) + (z_weight * z_diff);
            _reward = Math.Max(1000 - deductions, 100); // TODO change rate from 1 to .1 for both if starting with new weights
            Debug.Log("Max reward achieved " + _reward + "        angle: " + angle_diff + ", steps: " + stepsThisRollout + ", z_diff: " + z_diff);
            return _reward;
        }
        if (distance < OBJDIST) // && agentTransform.rotation.eulerAngles.x > 80 && agentTransform.rotation.eulerAngles.x < 100 && agentTransform.position.y > targetTransform.position.y) // If we reach optimal goal state range
        {
            double angle_diff = Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90));
            double z_diff = Math.Abs(agentTipTransform.position.z - targetTransform.position.z);
            if (agentTipTransform.position.z > targetTransform.position.z)
            {
                z_weight /= 4;
            }
            // Debug.Log("angle: " + angle_diff + ", steps: " + num_steps + ", z_diff: " + z_diff);
            double deductions = (angle_weight * (angle_diff*angle_diff)) + (step_weight * stepsThisRollout) + (z_weight * z_diff);

            _reward = Math.Max(100 - deductions, 1); // TODO change rate from 1 to .1 for both if starting with new weights
            Debug.Log("High reward achieved " + _reward + "        angle: " + angle_diff + ", steps: " + stepsThisRollout + ", z_diff: " + z_diff);
            return _reward;
        }

        //_reward = -.005 * (stepsThisRollout);
        double _angle_diff = Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90));

        _reward = -1 - ((2*distance) +  (.01 * (_angle_diff*_angle_diff)));
        //Debug.Log((currentStep-lastRolloutStart).ToString() + ", " + _reward);
        return _reward;
    }


    private bool terminal()
    {
        // Terminal if the target object is at the goal position and we have released the object
        //return (!holdingObject && (Vector3.Distance(targetTransform.position, goalPlaneTransform.position) < TERMINALDIST));

        float distance = Vector3.Distance(targetPosition(), agentTipTransform.position);

        // Temporary -- terminal if holding object or taken too many steps
        return (distance < OBJDIST || holdingObject || stepsThisRollout > 2000f);

        // Old
        //return Vector3.Distance(agentTransform.position, targetTransform.position) < OBJDIST || currentStep - lastRolloutStart > 10000;
    }

    //       phi(s),    R,    terminal
    private (NDArray, double, bool) step(Action action)
    {
        // Take the action
        act(action);
        NDArray newS = phi();

        // If this is on the real arm, wait 2 seconds
        if (realArm)
        {
            // Wait 2s
            System.Threading.Thread.Sleep(2);
        }
        return (newS, reward(), terminal());
    }


    public string handleStep(Action action)
    {
        // Take the action, get the reward
        NDArray obs;
        bool term = false;
        double R;
        (obs, R, term) = step(action);


        // If terminal, tell python to compute the gradient
        if (term)
        {
            if (stepsThisRollout < 2000f) Debug.Log("Got there");

            // Stow arm if real robot
            if (realArm)
            {
                stowPublisher.Stow();
            }
            return toSerial("update_weights", R, obs);
        }
        // If not terminal, ask python for the next action
        else
        {
            return toSerial("forward", R, obs);
        }
    }


    public string handleReset()
    {
        // Reset the environment
        reset();

        // Tell python to compute a step
        return toSerial("forward", 0, phi());
    }


    public string handleInit()
    {
        Debug.Log("Handling Init");
        return toSerial("forward", 0, phi());
    }


    public void printWeights()
    {
        Debug.Log("Theta: " + thetaGlobal);
        Debug.Log("W: " + wGlobal);
    }


    private string toSerial(string inst, double R, NDArray feats)
    {
        SocketSerializer serialObj = new SocketSerializer
        {
            instruction = inst,
            reward = R,
            feat1 = feats[0],
            feat2 = feats[1],
            feat3 = feats[2],
            feat4 = feats[3],
            feat5 = feats[4],
            feat6 = feats[5],
            feat7 = feats[6],
            feat8 = feats[7],
            feat9 = feats[8],
            feat10 = feats[9],
            feat11 = feats[10],
            feat12 = feats[11],
            feat13 = feats[12],
            feat14 = feats[13]
        };

        return JsonSerializer.Serialize(serialObj);
    }
}

class SocketSerializer
{
    public string instruction { get; set; }
    public double reward { get; set; }
    public float feat1 { get; set; }
    public float feat2 { get; set; }
    public float feat3 { get; set; }
    public float feat4 { get; set; }
    public float feat5 { get; set; }
    public float feat6 { get; set; }
    public float feat7 { get; set; }
    public float feat8 { get; set; }
    public float feat9 { get; set; }
    public float feat10 { get; set; }
    public float feat11 { get; set; }
    public float feat12 { get; set; }
    public float feat13 { get; set; }
    public float feat14 { get; set; }

}
