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
        float term = terminal() ? 1f : 0f;

        // Return the positions, plus whether terminal
        return new float[] { pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w, term };

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
        TensorShape thetaShape = new TensorShape(3);
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
