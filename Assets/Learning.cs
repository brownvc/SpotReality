using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NumSharp;
using UnityEngine.InputSystem.Android;
using System.Drawing.Printing;
using System.Linq;

public class Learning : MonoBehaviour
{
    public Transform greenHand;
    public Transform goalObj;
    private PolicyGradient sac;
    public bool useOpt;

    // Start is called before the first frame update
    void Start()
    {
        bool use_stored_weights = true;
        // this.sac = new SoftActorCritic(greenHand, goalObj, 1000000, 0.000005, 0.00005, 0.999, use_stored_weights);
        //this.sac = new PolicyGradient(greenHand, goalObj, 1000000, 0.000005, 0.00005, 0.999, use_stored_weights); // TODO tune hyperparameters 

    }

    // Update is called once per frame
    void Update()
    {
        //base  
        //var phi = np.array(new int[] { 0, 1, 2, 3, 4, 5, 0 }, np.float32);
        //var theta = np.ones((12, 7), np.float32);
        //int a = 3;
        //theta[11, 5] = 3;
        //Debug.Log(sac.softmaxPi(theta, phi));
        //theta[4, 5] = 3;
        //Debug.Log(sac.gradLogSoftmax(theta, phi, 2));


        //this.sac.rollout();

        //if (useOpt)
        //{
        //    this.sac.useOpt = true;
        //}

    }

    void OnDestroy()
    {
        this.sac.printWeights();
    }
    void OnApplicationQuit()
    {
        this.sac.printWeights();
    }
}

