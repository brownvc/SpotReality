using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
    Vector3 originalPos;
    Quaternion originalRot;
    Transform agentTransform;
    Transform goalTransform;

    public SoftActorCritic(Transform aT, Transform gT)
    {
        originalPos = agentTransform.position;
        originalRot = agentTransform.rotation;
        agentTransform = aT;
        goalTransform = gT;
    }


    private void reset()
    {

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

        // Return the positions
        return new float[] { pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w };

        // TODO: add whether in a place to pick up the distance
    }

    private double getReward()
    {
        // Return the inverse of the distance between the objects
        float distance = Vector3.Distance(agentTransform.position, goalTransform.position);

        // Account for getting very close
        if (distance < 0.5)
        {
            return 2;
        }
        else
        {
            return 1 / distance;
        }

        // TODO: reward more if pointing down        

    }

    private bool terminal()
    {
        return Vector3.Distance(agentTransform.position, goalTransform.position) < 0.5;
    }

    // Softmax policy
    private float[] softmaxPi(float[] theta, float[] stateFeats)
    {
        //Get numsharp for matrix operations!

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
