using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NumSharp;
using System;
using System.Text.Json;

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

public class PolicyGradient
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

    public int featureSize = 11;

    public int actionSize { get; }


    public PolicyGradient(Transform aT, Transform gT, int numSteps, double aTheta, double aW, double gamm, bool use_stored_weights = true)
    {
        originalPos = aT.position;
        originalRot = aT.rotation;
        agentTransform = aT;
        goalTransform = gT;
        actionSize = (int)Action.NumActions;

        if (use_stored_weights)
        {
            // Load stored weights
            // double[][] list = new double[][]
            // {
            //     new double[] { 0.0181830470019604, 0.160137449867225, -0.102560896381722, -0.100325995295111, 0.000376578373871349 },
            //     new double[] { -0.277513478245677, 0.205147927671313, -0.0018498067815981, -0.2370149013913, -0.0025177141727952 },
            //     new double[] { -0.340186684415659, -0.13815865116341, -0.0794612111036826, -0.120589869379266, 0.000371813728074248 },
            //     new double[] { -0.565688526784278, 0.0130413336167028, -0.631452321534621, -0.342492199013991, 0.000369076802778451 },
            //     new double[] { 1.07119925473673, 0.00710089504899006, 0.171878876212245, 0.632953711417075, 0.000263063210694675 },
            //     new double[] { -0.520615160041992, -0.111372908992269, -0.0865962398617954, -0.269497388302164, 0.000372099925858803 },
            //     new double[] { 0.889395241553879, -0.0665227743830269, 0.561442687445505, 0.587684353062698, 0.000392509925671927 },
            //     new double[] { -0.274773646646645, -0.0693732608708621, 0.168598941128973, -0.150717750730691, 0.000372572237890571 }
            // };

            double[][] list = new double[][]
        {
            new double[] {0.138288110384552, 0.159720091679826, -0.0340661340521807, -0.0487450218999172, 0.000401304835829784},
            new double[] {-0.422902745852534, 0.239518203083597, -0.044330497578551, -0.289886945091596, -0.0024970581938141},
            new double[] {-0.304807129881734, -0.108316331632656, -0.0547620837850433, -0.116305007851248, 0.000390929697138619},
            new double[] {-0.676865818472881, 0.0237386641294755, -0.650448457214696, -0.416237666311161, 0.000383593286258116},
            new double[] {1.19458126547329, 0.125094719430393, 0.20946151520725, 0.620734193949569, 9.31849317919252E-05},
            new double[] {-0.677713285064781, -0.0743515862084128, -0.190793170880945, -0.389400286534946, 0.000388828230422433},
            new double[] {1.11133708435196, -0.264829333156451, 0.568158470396313, 0.856161071757953, 0.000445560094852138},
            new double[] {-0.361917437954387, -0.100574421521889, 0.196780407025869, -0.216320382365654, 0.000393657147887056}
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
        // TODO factor in total cumulative change in pitch as a negative, to prevent spiraling. try to find a way to penalize moving back and forth less, and focus on spirals
        // TODO scale reward based on if the position and orientation of the agent creates a ray that goes near center of the goal. Scale based on distance between closest point on ray and goal center. 

        float distance = Vector3.Distance(goalTransform.position, agentTransform.position);

        if (terminal())
        {
            // Debug.Log("Agent x rot: " + agentTransform.rotation.eulerAngles.x);
            // Only reward if pointing down above object
            double _reward;
            if (distance > TERMINALDIST)
            {
                _reward = -100 - (.0001 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90)));
                Debug.Log("Max steps exceeded " + _reward);
                return _reward;
            }
            if (agentTransform.rotation.eulerAngles.x > 80 && agentTransform.rotation.eulerAngles.x < 100 && agentTransform.position.y > goalTransform.position.y)
            {
                double angle_diff = Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90));
                double num_steps = currentStep - lastRolloutStart;
                double z_diff = Math.Abs(agentTransform.position.z - goalTransform.position.z);
                // Debug.Log("angle: " + angle_diff + ", steps: " + num_steps + ", z_diff: " + z_diff);
                double deductions = (2 * angle_diff) + (1.6 * num_steps) + (100 * z_diff);
                _reward = Math.Max(1000 - deductions, 2); // TODO change rate from 1 to .1 for both if starting with new weights
                // Debug.Log("Max reward achieved " + _reward + "        angle: " + angle_diff + ", steps: " + num_steps + ", z_diff: " + z_diff);
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
                // _reward = Math.Max(1 - ((.01 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90))) + (.01 * (currentStep - lastRolloutStart))), .1);
                _reward = 1 - ((.1 * Math.Abs(Mathf.DeltaAngle(agentTransform.rotation.eulerAngles.x, 90))) + (.1 * (currentStep - lastRolloutStart)));
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

    //       phi(s),    R,    terminal
    private (NDArray, double, bool) step(Action action)
    {
        // Take the action
        act(action);
        NDArray newS = phi();

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

}



//public void oneSampleActorCritic()
//{
//    // One neural network that spits out a number and a distribution
//    // The number is the critic
//    // The second head is the actor
//    // Domain is your observations, range is R^d
//    // Now that we have the embedding, we feed it two an actor NN and a critic NN
//    // Actor spits out a probability distribution over actions
//    // Critic spits out a value

//    // For critic, loss is Mean squared TD error
//    // For actor, loss is TD error times log of softmax output (actor)

//    // Then call autograd to get the gradient of the NN with respect to its parameters

//    // Clean RL has implementations of everything, have a wrapper

//    (NDArray, double, bool) stepRet;
//    currentStep += 1;

//    if (currentStep < totalSteps)
//    {
//        // Store old state
//        NDArray obs = phi();

//        // Decide on an action
//        Action a = 0;// softmaxPi(thetaGlobal, wGlobal);

//        // Take a step
//        stepRet = step(a);
//        NDArray newObs = stepRet.Item1;
//        double R = stepRet.Item2;
//        bool term = stepRet.Item3;

//        // compute vhat, delta
//        double vhatNew;
//        if (term)
//        {
//            vhatNew = 0;
//        }
//        else
//        {
//            vhatNew = np.sum((wGlobal * newObs).astype(np.float32));
//        }

//        // Take gradient steps
//        double delta = R + gamma * vhatNew - np.sum((wGlobal * obs).astype(np.float32));
//        wGlobal += alphaW * delta * obs;
//        //thetaGlobal += alphaTheta * delta * I * gradLogSoftmax(thetaGlobal, obs, (int)a);

//        // Modify I
//        I *= gamma;

//        if (term)
//        {
//            Debug.Log("Reset");
//            reset();
//            I = 1;
//            int stepsThisRollout = currentStep - lastRolloutStart;
//            stepsPerRollout.Add(stepsThisRollout);
//            string str = "";
//            foreach (int i in stepsPerRollout)
//            {
//                str += i + ", ";
//            }
//            Debug.Log(str);
//            lastRolloutStart = currentStep;
//        }
//        if (currentStep - lastRolloutStart < 100) // TODO fix these, only hits if it happens to land on an exact multiple
//        {
//            Debug.Log("Step " + currentStep);
//            Debug.Log("Theta: " + thetaGlobal);
//            Debug.Log("W: " + wGlobal);
//        }
//    }
//    else
//    {
//        Debug.Log("Theta: " + thetaGlobal);
//        Debug.Log("W: " + wGlobal);
//    }

//    //return (np.zeros(1), np.zeros(1));
//}