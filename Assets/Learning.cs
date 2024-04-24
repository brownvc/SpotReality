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
        bool use_stored_weights = true;
        this.sac = new SoftActorCritic(greenHand, goalObj, 1000000, 0.000005, 0.00005, 0.999, use_stored_weights);

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


        this.sac.rollout();

        if (useOpt)
        {
            this.sac.useOpt = true;
        }

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
    public NDArray thetaGlobal;
    public NDArray wGlobal;
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

    public int featureSize = 5;

    public int actionSize { get; }


    public SoftActorCritic(Transform aT, Transform gT, int numSteps, double aTheta, double aW, double gamm, bool use_stored_weights = true)
    {
        originalPos = aT.position;
        originalRot = aT.rotation;
        agentTransform = aT;
        goalTransform = gT;
        actionSize = (int)Action.NumActions;

        if (use_stored_weights)
        {
            // Load stored weights
            // Create the list of lists
            // double[][] list = new double[][]
            // {
            // new double[] {-0.149857809813663, 0.224498940665993, -0.0792345530151628, -0.177251873976455, 0.000361198288136511},
            // new double[] {-0.091086138076753, 0.156258552130798, 0.0961937633306631, -0.150846945836251, -0.00252838801695558},
            // new double[] {-0.0759413055661327, -0.15986672132788, 0.0278787131705902, 0.0222744854063179, 0.000361198288136511},
            // new double[] {-0.520484433714969, -0.00918990814048947, -0.561558900436911, -0.296301709327981, 0.000361198288136511},
            // new double[] {0.718029441548807, 0.100051711304395, 0.182129467866025, 0.37409499252551, 0.000361198288136511},
            // new double[] {-0.307007049986844, -0.0879748156023372, -0.0919664440692011, -0.155647354275353, 0.000361198288136511},
            // new double[] {0.402485521373028, -0.181330827166818, 0.39327828344938, 0.345854110700327, 0.000361198288136511},
            // new double[] {0.0238617410417896, -0.0424469278527988, 0.0332796747936071, 0.0378242144014703, 0.000361198288136511}
            // };

            // Create the list of lists
            double[][] list = new double[][]
            {
            new double[] {0.031422511345937, 0.212453131750298, -0.0218344861315378, -0.0785889141526493, 0.00034958249290992},
            new double[] {-0.22498962355685, 0.207087498434513, 0.0543894052650787, -0.245977666736726, -0.00254067915457728},
            new double[] {-0.264986428763966, -0.117637147876635, -0.0473498872896212, -0.0961559692662337, 0.000349075265872792},
            new double[] {-0.607971532193491, -0.00272117804949102, -0.603987034116024, -0.34903435727116, 0.000353238132282678},
            new double[] {0.871191627889778, 0.071935431336154, 0.234355430996507, 0.46108782412158, 0.000447074351617141},
            new double[] {-0.395769363805432, -0.17757859716609, -0.0506487663608775, -0.157187938624172, 0.000351214405470739},
            new double[] {0.749163440609915, -0.112881679380638, 0.433751892096605, 0.512386110241525, 0.000342541782859778},
            new double[] {-0.158060606935189, -0.0806574779958731, 0.00132347181909295, -0.0465291273110982, 0.000347952727635685}
            };

            // Create the NumSharp array
            thetaGlobal = np.array(list);
        }
        else
        {
            thetaGlobal = np.zeros((actionSize, featureSize), np.float32);
        }



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
                rotMov = new Vector3(3, 0, 0);
                break;
            case Action.PitchN:
                rotMov = new Vector3(-3, 0, 0);
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
            //pos.x, // uncomment this for randomized goal position
            pos.y,
            pos.z,
            rot.eulerAngles.x / 360,
            //rot.eulerAngles.y,
            //rot.eulerAngles.z,
            //pos.x*pos.x,
            //pos.y*pos.y,
            //pos.z*pos.z,
            //goalPos.x, // uncomment this for randomized goal position
            //goalPos.y, // uncomment this for randomized goal position
            //goalPos.z, // uncomment this for randomized goal position
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
        // TODO maybe have reward be based on the difference in angle between optimal angle and terminal angle
        // TODO factor in total cumulative change in pitch as a negative, to prevent spiraling. try to find a way to penalize moving back and forth less, and focus on spirals
        // TODO add termination from main loop if weights converge

        float distance = Vector3.Distance(goalTransform.position, agentTransform.position);

        if (terminal())
        {
            // Debug.Log("Agent x rot: " + agentTransform.rotation.eulerAngles.x);
            // Only reward if pointing down above object
            double _reward;
            if (Vector3.Distance(agentTransform.position, goalTransform.position) > TERMINALDIST)
            {
                _reward = -100 - (.0001 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90)));
                Debug.Log("Max steps exceeded " + _reward);
                return _reward;
            }
            if (agentTransform.rotation.eulerAngles.x > 70 && agentTransform.rotation.eulerAngles.x < 95 && agentTransform.position.y > goalTransform.position.y)
            {
                double angle_diff = Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90));
                double num_steps = currentStep - lastRolloutStart;
                double z_diff = Math.Abs(agentTransform.position.z - goalTransform.position.z);
                // Debug.Log("angle: " + angle_diff + ", steps: " + num_steps + ", z_diff: " + z_diff);
                double deductions = (1.3 * angle_diff) + (1.1 * num_steps) + (100 * z_diff);
                _reward = Math.Max(1000 - deductions, 2); // TODO change rate from 1 to .1 for both if starting with new weights
                // Debug.Log("Max reward achieved " + _reward);
                return _reward;
            }
            // else if (agentTransform.rotation.eulerAngles.x > 40 && agentTransform.rotation.eulerAngles.x < 150 && agentTransform.position.y > goalTransform.position.y)
            // {
            //     _reward = 100 - ((.1 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90))) + (.1 * (currentStep - lastRolloutStart)));
            //     Debug.Log("Secondary reward achieved " + _reward);
            //     return _reward;
            // }
            // else if (agentTransform.rotation.eulerAngles.x > 10 && agentTransform.rotation.eulerAngles.x < 180)
            // {
            //     _reward = 15 - ((.001 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90))) + (.001 * (currentStep - lastRolloutStart)));
            //     Debug.Log("Tertiary reward achieved " + _reward);
            //     return _reward;
            // }
            else
            {
                _reward = Math.Max(1 - ((.01 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90))) + (.01 * (currentStep - lastRolloutStart))), .1);
                // Debug.Log("Default reward achieved " + _reward);
                return _reward;
            }
        }
        return -.005 * (currentStep - lastRolloutStart);
    }


    private bool terminal()
    {
        return Vector3.Distance(agentTransform.position, goalTransform.position) < TERMINALDIST || currentStep - lastRolloutStart > 10000;
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
        for (int i = 0; i < pi.shape[0]; i++)
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
        for (int i = 0; i < theta.shape[0]; i++)
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
                if (stepsThisRollout < 90 && R > 800)
                {
                    Debug.Log("Reward: " + R);
                    Debug.Log("Theta: " + thetaGlobal);
                    Debug.Log("W: " + wGlobal);
                    Debug.Log(str);
                }
                // Debug.Log(str);
                lastRolloutStart = currentStep;

            }
        }

        //return (epFeatures, epActions, epRewards);
    }


    private double discountedReturn(NDArray epRewards)
    {
        double ret = 0;
        NDArray gammaRet = np.zeros(epRewards.shape[0]);

        for (int i = 0; i < epRewards.shape[0]; i++)
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
        for (int i = 0; i < episodeLength; i++)
        {
            sumD += gradLogSoftmax(thetaGlobal, epFeatures[i], (int)epActions[i]);
        }

        // Update weights
        thetaGlobal += alphaTheta * discountedRet * sumD;

        //Debug.Log(thetaGlobal);

    }


    public void printWeights()
    {
        Debug.Log("Theta: " + thetaGlobal);
        Debug.Log("W: " + wGlobal);
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
                foreach (int i in stepsPerRollout)
                {
                    str += i + ", ";
                }
                Debug.Log(str);
                lastRolloutStart = currentStep;
            }
            if (currentStep - lastRolloutStart < 100) // TODO fix these, only hits if it happens to land on an exact multiple
            {
                Debug.Log("Step " + currentStep);
                Debug.Log("Theta: " + thetaGlobal);
                Debug.Log("W: " + wGlobal);
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

