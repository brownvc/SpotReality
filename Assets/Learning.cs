using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NumSharp;

public class Learning : MonoBehaviour
{
    public Transform greenHand;
    public Transform goalObj;
    private SoftActorCritic sac;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(greenHand.position);
        sac = new SoftActorCritic(greenHand, goalObj);
    }

    // Update is called once per frame
    void Update()
    {
        //base  
        var phi = np.array(new float[] { 0, 1, 2, 3, 4, 5, 0 });
        var theta = np.ones((sac.actionSize, sac.featureSize));
        sac.softmaxPi(theta, phi);
    }

}


public enum Action
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

public class SoftActorCritic
{
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform agentTransform;
    private Transform goalTransform;
    private const float TERMINALDIST = 0.5f;

    public int featureSize = 7;

    public int actionSize { get; }


    public SoftActorCritic(Transform aT, Transform gT)
    {
        originalPos = aT.position;
        originalRot = aT.rotation;
        agentTransform = aT;
        goalTransform = gT;
        actionSize = (int)Action.NumActions;
    }


    private NDArray reset()
    {
        // Reset the gripper to its original state, and return current state
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;

        return phi();
    }

    private void act(Action action)
    {
        float meterMovement = 0.1f;
        float degreeRotation = 9f;

        switch(action)
        {
            case Action.Up:
                agentTransform.Translate(Vector3.up * meterMovement);
                break;
            case Action.Down:
                agentTransform.Translate(Vector3.down * meterMovement);
                break;
            case Action.Left:
                agentTransform.Translate(Vector3.left * meterMovement);
                break;
            case Action.Right:
                agentTransform.Translate(Vector3.right * meterMovement);
                break;
            case Action.Forward:
                agentTransform.Translate(Vector3.forward * meterMovement);
                break;
            case Action.Back:
                agentTransform.Translate(Vector3.back * meterMovement);
                break;
            case Action.RollP:
                agentTransform.Rotate(new Vector3(0, 0, 1) * degreeRotation);
                break;
            case Action.RollN:
                agentTransform.Rotate(new Vector3(0, 0, -1) * degreeRotation);
                break;
            case Action.YawP:
                agentTransform.Rotate(new Vector3(0, 1, 0) * degreeRotation);
                break;
            case Action.YawN:
                agentTransform.Rotate(new Vector3(0, -1, 0) * degreeRotation);
                break;
            case Action.PitchP:
                agentTransform.Rotate(new Vector3(1, 0, 0) * degreeRotation);
                break;
            case Action.PitchN:
                agentTransform.Rotate(new Vector3(-1, 0, 0) * degreeRotation);
                break;
            default:
                break;
        }
    }


    private NDArray phi()
    {
        Vector3 pos = agentTransform.position;
        Quaternion rot = agentTransform.rotation;
        float term = terminal() ? 1f : 0f;

        NDArray phiS = np.array(new float[] { pos.x, pos.y, pos.z, rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z, term });

        // Assert that phiS is equal to the feature size
        if (phiS.shape[0] != featureSize)
        {
            throw new Exception("Need to set featureSize to equal length of features returned from phi()");
        }

        // Return the positions, plus whether terminal
        return phiS;

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
    public Action softmaxPi(NDArray theta, NDArray stateFeats)
    {
        NDArray denom;
        NDArray actionArr;
        Action action;
        NDArray pi = np.zeros(actionSize);

        // Get the denominator
        Debug.Log(theta.shape[1]);
        Debug.Log(stateFeats.shape[0]);
        var dot = np.dot(theta, stateFeats);
        var exp = np.exp(dot);
        denom = np.sum(exp);

        // Calculate the probability for each action
        for(int i = 0; i < pi.shape[0]; i++)
        {
            pi[i] = np.exp(np.matmul(theta[i].T, stateFeats));
        }

        Debug.Log(pi);

        // Randomly sample an action
        actionArr = np.random.choice(actionSize, default, true, (double[])pi);
        action = (Action)(int)actionArr;

        // Randomly sample an action according to vals
        return action;
    }


    //       phi(s), R,    terminal
    private (NDArray, double, double) step()
    {
        // Take a step according to the policy


        return (np.array(1), 2, 3);
    }


    //public float rollout()

}
