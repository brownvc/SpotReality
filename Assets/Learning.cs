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
    private SoftActorCritic sac;
    public bool useOpt;

    // Start is called before the first frame update
    void Start()
    {
        sac = new SoftActorCritic(greenHand, goalObj, 1000000, 0.000005, 0.00005, 0.999);

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


        sac.rollout();

        if (useOpt)
        {
            sac.useOpt = true;
        }

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
    //RollP, // P = positive
    //RollN, // N = negative
    //YawP,
    //YawN,
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
    private const float TERMINALDIST = 0.2f;
    private NDArray thetaGlobal;
    private NDArray wGlobal;
    private int currentStep;
    private int totalSteps;
    private double alphaTheta;
    private double alphaW;
    private double I;
    private double gamma;
    private List<int> stepsPerRollout;
    private int lastRolloutStart;
    List<NDArray> epFeatures;
    List<Action> epActions;
    List<double> epRewards;
    public bool useOpt;

    public int featureSize = 3;

    public int actionSize { get; }


    public SoftActorCritic(Transform aT, Transform gT, int numSteps, double aTheta, double aW, double gamm)
    {
        originalPos = aT.position;
        originalRot = aT.rotation;
        agentTransform = aT;
        goalTransform = gT;
        actionSize = (int)Action.NumActions;
        thetaGlobal = np.zeros((actionSize, featureSize), np.float32);
        wGlobal = np.zeros(featureSize);
        wGlobal = wGlobal.astype(np.float32);
        totalSteps = numSteps;
        currentStep = 0;
        alphaTheta = aTheta;
        alphaW = aW;
        gamma = gamm;
        I = 1;
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;
        stepsPerRollout = new List<int>();
        lastRolloutStart = 0;
        epFeatures = new List<NDArray>();
        epActions = new List<Action>();
        epRewards = new List<double>();
        useOpt = false;
    }

    private NDArray reset()
    {
        // Reset the gripper to its original state, and return current state
        agentTransform.position = originalPos;
        agentTransform.rotation = originalRot;

        return phi();
    }

    private bool outsideBoundary()
    {
        if (agentTransform.position.z < originalPos.z
            || agentTransform.position.z > goalTransform.position.z
            || agentTransform.position.y > originalPos.y
            || agentTransform.position.y < goalTransform.position.y
            || agentTransform.position.x > originalPos.x + 0.01f
            || agentTransform.position.x < originalPos.x - 0.01f
            )
        {
            return true;
        }
        return false;
    }

    public void act(Action action)
    {
        float meterMovement = 0.05f;
        float degreeRotation = 1f;
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
    }


    private NDArray phi()
    {
        Vector3 pos = agentTransform.position;
        Quaternion rot = agentTransform.rotation;
        Vector3 goalPos = goalTransform.position;
        Quaternion goalRot = goalTransform.rotation;
        float term = terminal() ? 1f : 0f;

        NDArray phiS = np.array(new float[] {
            //pos.x,
            //pos.y,
            //pos.z,
            rot.eulerAngles.x / 360,
            //rot.eulerAngles.y,
            //rot.eulerAngles.z,
            //pos.x*pos.x,
            //pos.y*pos.y,
            //pos.z*pos.z,
            //goalPos.x,
            //goalPos.y,
            //goalPos.z,
            //goalPos.x * goalPos.x,
            //goalPos.y * goalPos.y,
            //goalPos.z * goalPos.z,
            Vector3.Distance(goalTransform.position, agentTransform.position),
            //rot.eulerAngles.x * rot.eulerAngles.x,
            //rot.eulerAngles.y * rot.eulerAngles.y,
            //rot.eulerAngles.z * rot.eulerAngles.z,
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


    private double reward()
    {
        // Reward is dumb

        // Most obvious is you get +1 when reaching the goal

        // Maybe -1 all the way until the goal

        float distance = Vector3.Distance(goalTransform.position, agentTransform.position);

        if (terminal())
        {
            Debug.Log("Agent x rot: " + agentTransform.rotation.eulerAngles.x);
            // Only reward if pointing down above object
            if(agentTransform.rotation.eulerAngles.x > 60 && agentTransform.rotation.eulerAngles.x < 90 && agentTransform.position.y > goalTransform.position.y)
            {
                return 1000;
            }
            else
            {
                return 1;
            }
        }
        return -1;
    }


    private bool terminal()
    {
        return Vector3.Distance(agentTransform.position, goalTransform.position) < TERMINALDIST;
    }

    // Replaces np.random.choice -- returns index of array based on weighted probability
    private int randomChoice(NDArray probs)
    {
        double rand;
        System.Random random = new System.Random();
        double probAcc = 0d;
        rand = random.NextDouble(); // np.random.uniform(0, 1, np.float32);
        for (int i = 0; i < probs.shape[0]; i++)
        {
            probAcc += probs[i];

            if (rand < probAcc)
            {
                return i;
            }
        }

        return 0;
    }


    private NDArray softmaxDistribution(NDArray theta, NDArray stateFeats) 
    {
        NDArray denom;
        NDArray pi = np.zeros(actionSize);

        // Get the denominator
        var dot = np.sum(theta * stateFeats, 1, np.float32);
        var exp = np.exp(dot, np.float32);
        denom = np.sum(exp, np.float32);

        // Calculate the probability for each action
        for(int i = 0; i < pi.shape[0]; i++)
        {
            dot = np.sum(theta[i] * stateFeats, np.float32);
            pi[i] = np.exp(dot, np.float32) / denom;
        }
        return pi;
    }

    // Softmax policy
    public Action softmaxPi(NDArray theta, NDArray stateFeats)
    {
        // Randomly sample an action
        if (useOpt)
        {
            return (Action)np.argmax(softmaxDistribution(theta, stateFeats));
        }
        return (Action)randomChoice(softmaxDistribution(theta, stateFeats));
    }
    public NDArray gradLogSoftmax(NDArray theta, NDArray stateFeats, int aIndex)
    {
        NDArray grad = np.zeros(theta.shape, np.float32);
        grad[aIndex] += (stateFeats * (1 - softmaxDistribution(theta, stateFeats)[aIndex]));
        for (int i=0; i<theta.shape[0]; i++) 
        {
            if (i == aIndex) { continue; }
            grad[i] -= (stateFeats * softmaxDistribution(theta, stateFeats)[i]);
        }
        return grad;
    }


    //       phi(s),    R,    terminal
    private (NDArray, double, bool) step(NDArray phiS, Action action)
    {
        NDArray prevS = phiS.copy();

        // Take the action
        act(action);
        NDArray newS = phi();

        return (newS, reward(), terminal());
    }


    // used to be (List<NDArray>, List<Action>, List<double>)
    public void rollout()
    {
        /*
         * This function works like rollout except is designed to be called one step at a time.
         * Once a rollout reaches the terminal state, Reinforce is called to update theta and the environment resets.
         */

        Action a;
        NDArray obs;
        bool term = false;
        double R;

        currentStep += 1;

        if (currentStep < totalSteps)
        {
            obs = phi();

            // get the action
            a = softmaxPi(thetaGlobal, obs);
            // take a step
            (obs, R, term) = step(obs, a);
            // add observations
            epFeatures.Add(phi());
            epActions.Add(a);
            epRewards.Add(R);
            obs = phi();

            if (term)
            {
                // Call reinforce so it can update env
                reinforce();

                // Reset hand
                reset();

                // Reset globals for next rollout
                epFeatures = new List<NDArray>();
                epActions = new List<Action>();
                epRewards = new List<double>();

                // Initialize next features
                epFeatures.Add(phi());

                // Track steps taken
                int stepsThisRollout = currentStep - lastRolloutStart;
                stepsPerRollout.Add(stepsThisRollout);
                string str = "";
                foreach (int i in stepsPerRollout)
                {
                    str += i + ", ";
                }
                //Debug.Log("Reward: " + epRewards[epRewards.Count - 1]);
                Debug.Log(str);
                lastRolloutStart = currentStep;

            }
        }

        //return (epFeatures, epActions, epRewards);
    }


    private double discountedReturn(NDArray epRewards)
    {
        double ret = 0;
        NDArray gammaRet = np.zeros(epRewards.shape[0]);

        for(int i = 0; i < epRewards.shape[0]; i++)
        {
            gammaRet[i] = Math.Pow(gamma, i);
        }

        ret = np.sum((gammaRet * epRewards).astype(np.float32));

        return ret;
    }


    private void reinforce()
    {
        NDArray sumD = np.zeros((thetaGlobal.shape[0], thetaGlobal.shape[1]));
        double discountedRet = 0;
        int episodeLength = 0;


        episodeLength = epActions.Count;

        // Get discounted returns
        discountedRet = discountedReturn(np.array(epRewards).astype(np.float32));

        // Get sum
        for(int i = 0; i < episodeLength; i++)
        {
            sumD += gradLogSoftmax(thetaGlobal, epFeatures[i], (int)epActions[i]);
        }

        // Update weights
        thetaGlobal += alphaTheta * discountedRet * sumD;

        //Debug.Log(thetaGlobal);
                                                                 
    }




    public void oneSampleActorCritic() 
    {
        // One neural network that spits out a number and a distribution
        // The number is the critic
        // The second head is the actor
        // Domain is your observations, range is R^d
        // Now that we have the embedding, we feed it two an actor NN and a critic NN
        // Actor spits out a probability distribution over actions
        // Critic spits out a value

        // For critic, loss is Mean squared TD error
        // For actor, loss is TD error times log of softmax output (actor)

        // Then call autograd to get the gradient of the NN with respect to its parameters

        // Clean RL has implementations of everything, have a wrapper

        (NDArray, double, bool) stepRet;
        currentStep += 1;

        if (currentStep < totalSteps)
        {
            // Store old state
            NDArray obs = phi();

            // Decide on an action
            Action a = softmaxPi(thetaGlobal, wGlobal);

            // Take a step
            stepRet = step(phi(), a);
            NDArray newObs = stepRet.Item1;
            double R = stepRet.Item2;  
            bool term = stepRet.Item3;

            // compute vhat, delta
            double vhatNew;
            if (term)
            {
                vhatNew = 0;
            }
            else
            {
                vhatNew = np.sum((wGlobal * newObs).astype(np.float32));
            }

            // Take gradient steps
            double delta = R + gamma * vhatNew - np.sum((wGlobal * obs).astype(np.float32));
            wGlobal += alphaW * delta * obs;
            thetaGlobal += alphaTheta * delta * I * gradLogSoftmax(thetaGlobal, obs, (int)a);

            // Modify I
            I *= gamma;

            if (term)
            {
                Debug.Log("Reset");
                reset();
                I = 1;
                int stepsThisRollout = currentStep - lastRolloutStart;
                stepsPerRollout.Add(stepsThisRollout);
                string str = "";
                foreach(int i in stepsPerRollout)
                {
                    str += i + ", ";
                }
                Debug.Log(str);
                lastRolloutStart = currentStep;
            }
            if (currentStep % 400 == 0)
            {
                Debug.Log("Step " + currentStep);
            }
        }
        else
        {
            Debug.Log("Theta: " + thetaGlobal);
            Debug.Log("W: " + wGlobal);
        }

        //return (np.zeros(1), np.zeros(1));
    }
}

