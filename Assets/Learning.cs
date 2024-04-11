using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Barracuda;

public class Learning : MonoBehaviour
{
    public Transform greenHand;
    public Transform goalObj;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       //base  
    }

}


public class SoftActorCritic
{
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform agentTransform;
    private Transform goalTransform;
    private const float TERMINALDIST = 0.5f;

    public int featureSize = 8;

    enum Action
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Back,
        RollP, // P = positive
        RollN, // N = negative
        YawP,
        YawN,
        PitchP,
        PitchN,
        NumActions
    }


    public SoftActorCritic(Transform aT, Transform gT)
    {
        originalPos = agentTransform.position;
        originalRot = agentTransform.rotation;
        agentTransform = aT;
        goalTransform = gT;
    }


    private float[] reset()
    {
        float[] phiS;

        // Reset the gripper to its original state, and return current state
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;
        phiS = phi();

        return phiS;
    }

    private void act(Action action)
    {
        float modifier = 0.1f;

        switch(action)
        {
            case Action.Up:
                agentTransform.Translate(Vector3.up * modifier);
                break;
            case Action.Down:
                agentTransform.Translate(Vector3.down * modifier);
                break;
            case Action.Left:
                agentTransform.Translate(Vector3.left * modifier);
                break;
            case Action.Right:
                agentTransform.Translate(Vector3.right * modifier);
                break;
            case Action.Forward:
                agentTransform.Translate(Vector3.forward * modifier);
                break;
            case Action.Back:
                agentTransform.Translate(Vector3.back * modifier);
                break;
            case Action.RollP:
                agentTransform.Rotate(new Vector3(0, 0, 1) * modifier);
                break;
            case Action.RollN:
                agentTransform.Rotate(new Vector3(0, 0, -1) * modifier);
                break;
            case Action.YawP:
                agentTransform.Rotate(new Vector3(0, 1, 0) * modifier);
                break;
            case Action.YawN:
                agentTransform.Rotate(new Vector3(0, -1, 0) * modifier);
                break;
            case Action.PitchP:
                agentTransform.Rotate(new Vector3(1, 0, 0) * modifier);
                break;
            case Action.PitchN:
                agentTransform.Rotate(new Vector3(-1, 0, 0) * modifier);
                break;
            default:
                break;
        }
    }

    public float[] featurized_reset()
    {
        // Move the hand back to its original location
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;
        return phi();
    }

    private float[] phi()
    {
        Vector3 pos = agentTransform.position;
        Quaternion rot = agentTransform.rotation;
        float term = terminal() ? 1f : 0f;

        float [] phiS = new float[] { pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, term };

        // Assert that phiS is equal to the feature size
        if (phiS.Length != featureSize)
        {
            throw new Exception("Need to set featureSize to equal length of features returned from phi()");
        }

        // Return the positions, plus whether terminal
        return phiS;

        // TODO: add whether in a place to pick up the goal object
    }

    private double reward()
    {
        // Return the inverse of the distance between the objects
        float distance = Vector3.Distance(agentTransform.position, goalTransform.position);

        // Account for getting very close
        if (distance < TERMINALDIST)
        {
            return 1 / TERMINALDIST;
        }
        else
        {
            return 1 / distance;
        }

        // TODO: reward more if pointing down        

    }

    private bool terminal()
    {
        return Vector3.Distance(agentTransform.position, goalTransform.position) < TERMINALDIST;
    }

    // Softmax policy
    private float[] softmaxPi(float[] theta, float[] stateFeats)
    {
        // Create the policy tensor
        TensorShape thetaShape = new TensorShape((int)Action.NumActions, featureSize);
        Tensor pi = new Tensor(thetaShape);

        // Get the denominator



        return new float[] { 1 };
    }


            // phi(s), R, terminal
    private (int[], double, double) step()
    {
        // Take a step according to the policy


        return (new int[] { 1 }, 2, 3);
    }


    //public float rollout()

}
