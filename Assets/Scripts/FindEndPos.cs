//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


//namespace RosSharp.RosBridgeClient
//{
//    public class FindEndPos : UnityPublisher<MessageTypes.Std.Float64MultiArray>
//    {
//        public GameObject targetTransform;
//        private Vector3 initialPos;

//        private GameObject armObj;
//        private GameObject shoulder;
//        private GameObject elbow0;
//        private GameObject elbow1;
//        private GameObject wrist0;
//        private GameObject wrist1;
//        private GameObject finger;
//        private GameObject testCube;


//        [SerializeField]
//        private double[] dataArrayGhost;
//        //[SerializeField]
//        private double sh0;

//        [SerializeField]
//        private double sh1;
//        [SerializeField]
//        private double sh2;
//        //[SerializeField]
//        private double testCubeRotation;



//        private double sh1_without_transform;


//        private MessageTypes.Std.Float64MultiArray message;
//        public string FrameId = "Unity";

//        //Start is called before the first frame update
//        protected override void Start()
//        {
//            dataArrayGhost = new double[6];
//            targetTransform = GameObject.Find("TargetTransform");
//            base.Start();
//            sh1 = 0;


//            armObj = GameObject.Find("ghost arm");
//            shoulder = GameObject.Find("newarm0.link_sh1");
//            elbow0 = GameObject.Find("newarm0.link_el0");
//            elbow1 = GameObject.Find("newarm0.link_el1");
//            wrist0 = GameObject.Find("newarm0.link_wr0");
//            wrist1 = GameObject.Find("newarm0.link_wr1");
//            finger = GameObject.Find("newarm0.link_fngr");

//            testCube = GameObject.Find("testCube");

//            InitializeMessage();
//        }

//        private void InitializeMessage()
//        {
//            message = new MessageTypes.Std.Float64MultiArray
//            {
//                data = { }
//            };
//        }



//        //Update is called once per frame
//        void Update()
//        {

//            //TODO: fill in the rest, and not Debug but output them somewhere

//            //received input from red ghost arm

//            //testing targetTransform:Vector3(2.59599996,0.908999979,1.954)
//            sh0 = getJointRotation(armObj).x;
//            sh1 = getJointRotation(shoulder).x;
//            float angle;
//            Vector3 axis;
//            Quaternion currentR = shoulder.transform.localRotation;
//            currentR.ToAngleAxis(out angle, out axis);
//            sh2 = -1.0 * (2 * Mathf.PI - ((double)angle * Mathf.Deg2Rad));
//            double shoulderRotation = transformAngle(sh2);
//            testCubeRotation = testCube.transform.eulerAngles.x;

//            //// TODO: Try changing the z values to negative

//            dataArrayGhost[0] = -1.0 * transformAngle(getJointRotation(armObj).y);


//            dataArrayGhost[1] = shoulderRotation;


//            dataArrayGhost[2] = transformAngle(getJointRotation(elbow0).x);
//            dataArrayGhost[3] = -1.0 * transformAngle(getJointRotation(elbow1).z); // changed from z to x
//            dataArrayGhost[4] = transformAngle(getJointRotation(wrist0).x);
//            dataArrayGhost[5] = -1.0 * transformAngle(getJointRotation(wrist1).z); // changed from z to x

//            message.data = dataArrayGhost;

//            Publish(message);
//        }

//        public Vector3 getJointRotation(GameObject joint)
//        {

//            return joint.transform.localEulerAngles * Mathf.Deg2Rad;
//        }

//        public double transformAngle(double angle)
//        {
//            angle = angle % (2 * Mathf.PI);

//            if (angle > Mathf.PI)
//            {
//                angle = -2 * Mathf.PI + angle;
//            }

//            if (angle < -Mathf.PI)
//            {
//                angle = 2 * Mathf.PI + angle;
//            }

//            return angle;
//        }



//    }
//}


























//------------------------------------------- VR Version -------------------------------------------------------------------------//
//--------------------------------------------------------------------------------------------------------------------------------//

















using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RosSharp.RosBridgeClient
{
    public class FindEndPos : UnityPublisher<MessageTypes.Std.Float64MultiArray>
    {
        private bool triggerWasPressed = false;
        private bool trigger2WasPressed = false;
        private GameObject rightController;
        private GameObject dummyFinger;
        public GameObject targetTransform;
        private Vector3 initialPos;

        private GameObject armObjGhost;
        private GameObject shoulder1Ghost;
        private GameObject elbow0Ghost;
        private GameObject elbow1Ghost;
        private GameObject wrist0Ghost;
        private GameObject wrist1Ghost;
        private GameObject fingerGhost;


        private GameObject armObjReal;
        private GameObject shoulder1Real;
        private GameObject elbow0Real;
        private GameObject elbow1Real;
        private GameObject wrist0Real;
        private GameObject wrist1Real;
        private GameObject fingerReal;

        private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 lastDummyLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private bool queue;
        private Quaternion currentR;

        public int testing = 0;


        private MessageTypes.Std.Float64MultiArray message;
        public string FrameId = "Unity";

        float angle;
        Vector3 axis;
        double shoulderRotation;


        [SerializeField]
        private double[] dataArrayGhost;
        //[SerializeField]
        private double sh0;

        [SerializeField]
        private double sh1;
        [SerializeField]
        private double sh2;
        //[SerializeField]





        //
        public bool sending;

        //private GameObject armObjGhost;
        //private GameObject shoulder1Ghost;
        //private GameObject elbow0Ghost;
        //private GameObject elbow1Ghost;
        //private GameObject wrist0Ghost;
        //private GameObject wrist1Ghost;
        //private GameObject fingerGhost;

        [SerializeField]
        private double[] IKDataArray;
        public double[] ghostArmRads;
        [SerializeField]
        private double[] realArmDegrees;
        [SerializeField]
        private double[] realArmRads;
        [SerializeField]
        private float[] published;



        //Start is called before the first frame update
        protected override void Start()
        {
            if (testing == 0)
            {
                dataArrayGhost = new double[6];
                targetTransform = GameObject.Find("TargetTransform");
                rightController = GameObject.Find("RightHand Controller");
                dummyFinger = GameObject.Find("dummy_link_fngr");


                base.Start();
                //initialPos = dummyFinger.transform.position;


                armObjGhost = GameObject.Find("ghost arm");
                shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
                elbow0Ghost = GameObject.Find("newarm0.link_el0");
                elbow1Ghost = GameObject.Find("newarm0.link_el1");
                wrist0Ghost = GameObject.Find("newarm0.link_wr0");
                wrist1Ghost = GameObject.Find("newarm0.link_wr1");
                fingerGhost = GameObject.Find("newarm0.link_fngr");

                armObjReal = GameObject.Find("arm0.link_sh0");
                shoulder1Real = GameObject.Find("arm0.link_sh1");
                elbow0Real = GameObject.Find("arm0.link_el0");
                elbow1Real = GameObject.Find("arm0.link_el1");
                wrist0Real = GameObject.Find("arm0.link_wr0");
                wrist1Real = GameObject.Find("arm0.link_wr1");
                fingerReal = GameObject.Find("arm0.link_fngr");
                dummyFinger.transform.position = fingerReal.transform.position;

                targetTransform.transform.position = dummyFinger.transform.position;
                targetTransform.transform.rotation = dummyFinger.transform.rotation;
                InitializeMessage();
            }


            else if (testing == 1)
            {
                dataArrayGhost = new double[6];
                targetTransform = GameObject.Find("TargetTransform");
                base.Start();
                sh1 = 0;

                armObjGhost = GameObject.Find("ghost arm");
                shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
                elbow0Ghost = GameObject.Find("newarm0.link_el0");
                elbow1Ghost = GameObject.Find("newarm0.link_el1");
                wrist0Ghost = GameObject.Find("newarm0.link_wr0");
                wrist1Ghost = GameObject.Find("newarm0.link_wr1");
                fingerGhost = GameObject.Find("newarm0.link_fngr");

                InitializeMessage();
            }

            else// testing == 2
            {
                sending = true;

                realArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
                ghostArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
                realArmDegrees = new double[] { 0, 0, 0, 0, 0, 0 };
                IKDataArray = new double[] { 0, 0, 0, 0, 0, 0 };
                //published = new double[] { 0, 0, 0, 0, 0, 0 };

                targetTransform = GameObject.Find("TargetTransform");
                rightController = GameObject.Find("RightHand Controller");
                dummyFinger = GameObject.Find("newarm0.link_fngr");

                dummyFinger.transform.position = GameObject.Find("arm0.link_fngr").transform.position;
                //Debug.Log(rightController);
                base.Start();
                initialPos = dummyFinger.transform.position;


                armObjGhost = GameObject.Find("ghost arm");
                shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
                elbow0Ghost = GameObject.Find("newarm0.link_el0");
                elbow1Ghost = GameObject.Find("newarm0.link_el1");
                wrist0Ghost = GameObject.Find("newarm0.link_wr0");
                wrist1Ghost = GameObject.Find("newarm0.link_wr1");
                fingerGhost = GameObject.Find("newarm0.link_fngr");

                armObjReal = GameObject.Find("arm0.link_sh0");
                shoulder1Real = GameObject.Find("arm0.link_sh1");
                elbow0Real = GameObject.Find("arm0.link_el0");
                elbow1Real = GameObject.Find("arm0.link_el1");
                wrist0Real = GameObject.Find("arm0.link_wr0");
                wrist1Real = GameObject.Find("arm0.link_wr1");
                fingerReal = GameObject.Find("arm0.link_fngr");


                InitializeMessage();
            }



        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Std.Float64MultiArray
            {
                data = { }
            };
        }



        //Update is called once per frame
        void Update()
        {
            if (testing == 0) //Testing IK with VR controls
            {
                VRControls();
            }
            else if (testing == 1) //moving IK arm with target ball in inspector
            {
                IKTesting();
            }
            else //manually moving arm with arm angle inputs from inspector
            {
                ManualArmTesting();
            }
        }


        private void VRControls()
        {
            bool triggerValue;
            bool triggerValue2;


            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                //check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    //If the  trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue)
                    {
                        if (!triggerWasPressed)
                        {
                            triggerWasPressed = true;

                            //update the targetTransform's position, and HybridIK scripts will automatically reset the joint's positions and orientations
                            lastDummyLocation = dummyFinger.transform.position;
                            lastHandLocation = rightController.transform.position;
                        }

                        else
                        {
                            dummyFinger.transform.position = lastDummyLocation + rightController.transform.position - lastHandLocation;
                            dummyFinger.transform.rotation = rightController.transform.rotation;
                        }
                        queue = true;
                    }

                    else if (queue)
                    {
                        targetTransform.transform.position = dummyFinger.transform.position;
                        targetTransform.transform.rotation = dummyFinger.transform.rotation;
                        triggerWasPressed = false;
                        queue = false;
                    }


                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out triggerValue2) && triggerValue2)
                    {
                        if (!trigger2WasPressed)
                        {
                            currentR = shoulder1Ghost.transform.localRotation;
                            currentR.ToAngleAxis(out angle, out axis);
                            sh2 = -1.0 * (2 * Mathf.PI - ((double)angle * Mathf.Deg2Rad));  // get rid of negative, transformangle
                            shoulderRotation = transformAngle(sh2);

                            dataArrayGhost[0] = -1.0 * transformAngle(getJointRotation(armObjGhost).y);
                            dataArrayGhost[1] = shoulderRotation;
                            dataArrayGhost[2] = transformAngle(getJointRotation(elbow0Ghost).x);
                            dataArrayGhost[3] = -1.0 * transformAngle(getJointRotation(elbow1Ghost).z); // changed from z to x
                            dataArrayGhost[4] = transformAngle(getJointRotation(wrist0Ghost).x);
                            dataArrayGhost[5] = -1.0 * transformAngle(getJointRotation(wrist1Ghost).z); // changed from z to x
                            message.data = dataArrayGhost;
                            Publish(message);
                            trigger2WasPressed = true;
                        }
                        else
                        {
                            trigger2WasPressed = false;
                        }
                    }
                }
            }

        }


        private void IKTesting()
        {

            //TODO: fill in the rest, and not Debug but output them somewhere

            //received input from red ghost arm

            //testing targetTransform:Vector3(2.59599996,0.908999979,1.954)
            sh0 = getJointRotation(armObjGhost).x;
            sh1 = getJointRotation(shoulder1Ghost).x;
            float angle;
            Vector3 axis;
            Quaternion currentR = shoulder1Ghost.transform.localRotation;
            currentR.ToAngleAxis(out angle, out axis);
            sh2 = -1.0 * (2 * Mathf.PI - ((double)angle * Mathf.Deg2Rad));
            double shoulderRotation = transformAngle(sh2);

            //// TODO: Try changing the z values to negative

            dataArrayGhost[0] = -1.0 * transformAngle(getJointRotation(armObjGhost).y);


            dataArrayGhost[1] = shoulderRotation;


            dataArrayGhost[2] = transformAngle(getJointRotation(elbow0Ghost).x);
            dataArrayGhost[3] = -1.0 * transformAngle(getJointRotation(elbow1Ghost).z); // changed from z to x
            dataArrayGhost[4] = transformAngle(getJointRotation(wrist0Ghost).x);
            dataArrayGhost[5] = -1.0 * transformAngle(getJointRotation(wrist1Ghost).z); // changed from z to x

            message.data = dataArrayGhost;

            Publish(message);
            //Debug.Log(message.data.ToString() + " published");

            //for(int i = 0; i < 6; i++)
            //{
            //    Debug.Log(dataArrayGhost[i] + " published");
            //}

        }


        public void ManualArmTesting()
        {
            string orderOfRotations = "yxxzxz";
            //ROTATE GHOST ARM ANGLES
            armObjGhost.transform.localEulerAngles = new Vector3(0, (float)(-1.0 * ghostArmRads[0] * Mathf.Rad2Deg), 0);
            shoulder1Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[1] * Mathf.Rad2Deg), 0, 0);
            elbow0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[2] * Mathf.Rad2Deg), 0, 0);
            elbow1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[3] * Mathf.Rad2Deg));
            wrist0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[4] * Mathf.Rad2Deg), 0, 0);
            wrist1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[5] * Mathf.Rad2Deg));

            for (int i = 0; i < 6; i++)
                ghostArmRads[i] = transformAngle(ghostArmRads[i]);

            //double[] IKDataArray = { -1.0 * (getJointRotation(armObjGhost).y) , (getJointRotation(shoulder1Ghost).x), (getJointRotation(elbow0Ghost).x),
            //                    (-1.0*getJointRotation(elbow1Ghost).z), ( getJointRotation(wrist0Ghost).x), (-1.0*getJointRotation(wrist1Ghost).z)};

            //RECIEVE REAL FEEDBACK ANGLES
            realArmRads[0] = getJointRotation(armObjReal).y;
            realArmRads[1] = getJointRotation(shoulder1Real).x;
            realArmRads[2] = getJointRotation(elbow0Real).x;
            realArmRads[3] = getJointRotation(elbow1Real).z;
            realArmRads[4] = getJointRotation(wrist0Real).x;
            realArmRads[5] = getJointRotation(wrist1Real).z;

            for (int i = 0; i < 6; i++)
            {
                realArmDegrees[i] = realArmRads[i] * Mathf.Rad2Deg;

            }


            if (sending)
            {
                //message.data = ghostArmRads;
                message.data = ghostArmRads;
                Debug.Log("sending");
                Publish(message);
                //trigger2WasPressed = false;
            }

        }

        public Vector3 getJointPosition(GameObject joint)
        {
            return joint.transform.position;
        }

        public Vector3 getJointRotation(GameObject joint)
        {

            return joint.transform.localEulerAngles * Mathf.Deg2Rad;
        }

        public double transformAngle(double angle)
        {
            angle = angle % (2 * Mathf.PI);

            if (angle > Mathf.PI)
            {
                angle = -2 * Mathf.PI + angle;
            }

            if (angle < -Mathf.PI)
            {
                angle = 2 * Mathf.PI + angle;
            }

            return angle;
        }
    }
}









//----------------------------------------------------TESTING MANUEALLY---------------------------------------------------------------------//
//-------------------------------------------------------------------------------------------------------------------------//

















//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


//namespace RosSharp.RosBridgeClient
//{
//    public class FindEndPos : UnityPublisher<MessageTypes.Std.Float64MultiArray>
//    {
//        private bool triggerWasPressed = false;
//        private bool trigger2WasPressed = false;
//        public bool sending;
//        private GameObject rightController;
//        private GameObject dummyFinger;
//        public GameObject targetTransform;
//        private Vector3 initialPos;

//        private GameObject armObjGhost;
//        private GameObject shoulder1Ghost;
//        private GameObject elbow0Ghost;
//        private GameObject elbow1Ghost;
//        private GameObject wrist0Ghost;
//        private GameObject wrist1Ghost;
//        private GameObject fingerGhost;

//        private GameObject armObjReal;
//        private GameObject shoulder1Real;
//        private GameObject elbow0Real;
//        private GameObject elbow1Real;
//        private GameObject wrist0Real;
//        private GameObject wrist1Real;
//        private GameObject fingerReal;

//        [SerializeField]
//        private double[] IKDataArray;
//        public double[] ghostArmRads;
//        [SerializeField]
//        private double[] realArmDegrees;
//        [SerializeField]
//        private double[] realArmRads;
//        [SerializeField]
//        private float[] published;


//        private MessageTypes.Std.Float64MultiArray message;
//        public string FrameId = "Unity";

//        // Start is called before the first frame update
//        protected override void Start()
//        {
//            sending = true;

//            realArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
//            ghostArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
//            realArmDegrees = new double[] { 0, 0, 0, 0, 0, 0 };
//            IKDataArray = new double[] { 0, 0, 0, 0, 0, 0 };
//            //published = new double[] { 0, 0, 0, 0, 0, 0 };

//            targetTransform = GameObject.Find("TargetTransform");
//            rightController = GameObject.Find("RightHand Controller");
//            dummyFinger = GameObject.Find("newarm0.link_fngr");

//            dummyFinger.transform.position = GameObject.Find("arm0.link_fngr").transform.position;
//            //Debug.Log(rightController);
//            base.Start();
//            initialPos = dummyFinger.transform.position;


//            armObjGhost = GameObject.Find("ghost arm");
//            shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
//            elbow0Ghost = GameObject.Find("newarm0.link_el0");
//            elbow1Ghost = GameObject.Find("newarm0.link_el1");
//            wrist0Ghost = GameObject.Find("newarm0.link_wr0");
//            wrist1Ghost = GameObject.Find("newarm0.link_wr1");
//            fingerGhost = GameObject.Find("newarm0.link_fngr");

//            armObjReal = GameObject.Find("arm0.link_sh0");
//            shoulder1Real = GameObject.Find("arm0.link_sh1");
//            elbow0Real = GameObject.Find("arm0.link_el0");
//            elbow1Real = GameObject.Find("arm0.link_el1");
//            wrist0Real = GameObject.Find("arm0.link_wr0");
//            wrist1Real = GameObject.Find("arm0.link_wr1");
//            fingerReal = GameObject.Find("arm0.link_fngr");


//            InitializeMessage();
//        }

//        private void InitializeMessage()
//        {
//            message = new MessageTypes.Std.Float64MultiArray
//            {
//                data = { }
//            };
//        }



//        // Update is called once per frame
//        void Update()
//        {

//            string orderOfRotations = "yxxzxz";


//            ////ROTATE GHOST ARM ANGLES
//            //published[0] = (float)(-1.0 * ghostArmRads[0] * Mathf.Rad2Deg);
//            //shoulder1Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[1] * Mathf.Rad2Deg), 0, 0);
//            //elbow0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[2] * Mathf.Rad2Deg), 0, 0);
//            //elbow1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[3] * Mathf.Rad2Deg));
//            //wrist0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[4] * Mathf.Rad2Deg), 0, 0);
//            //wrist1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[5] * Mathf.Rad2Deg));


//            //for (int i = 0; i < 6; i++)
//            //{
//            //    published[i] = (float)realArmRads[i] * Mathf.Rad2Deg;

//            //}
//            //ROTATE GHOST ARM ANGLES
//            armObjGhost.transform.localEulerAngles = new Vector3(0, (float)(-1.0 * ghostArmRads[0] * Mathf.Rad2Deg), 0);
//            shoulder1Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[1] * Mathf.Rad2Deg), 0, 0);
//            elbow0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[2] * Mathf.Rad2Deg), 0, 0);
//            elbow1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[3] * Mathf.Rad2Deg));
//            wrist0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[4] * Mathf.Rad2Deg), 0, 0);
//            wrist1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[5] * Mathf.Rad2Deg));

//            for (int i = 0; i < 6; i++)
//                ghostArmRads[i] = transformAngle(ghostArmRads[i]);

//            //double[] IKDataArray = { -1.0 * (getJointRotation(armObjGhost).y) , (getJointRotation(shoulder1Ghost).x), (getJointRotation(elbow0Ghost).x),
//            //                    (-1.0*getJointRotation(elbow1Ghost).z), ( getJointRotation(wrist0Ghost).x), (-1.0*getJointRotation(wrist1Ghost).z)};

//            //RECIEVE REAL FEEDBACK ANGLES
//            realArmRads[0] = getJointRotation(armObjReal).y;
//            realArmRads[1] = getJointRotation(shoulder1Real).x;
//            realArmRads[2] = getJointRotation(elbow0Real).x;
//            realArmRads[3] = getJointRotation(elbow1Real).z;
//            realArmRads[4] = getJointRotation(wrist0Real).x;
//            realArmRads[5] = getJointRotation(wrist1Real).z;

//            for (int i = 0; i < 6; i++)
//            {
//                realArmDegrees[i] = realArmRads[i] * Mathf.Rad2Deg;

//            }


//            if (sending)
//            {
//                //message.data = ghostArmRads;
//                message.data = ghostArmRads;
//                //Debug.Log("sending");
//                Publish(message);
//                //trigger2WasPressed = false;
//            }

//        }




//        public Vector3 getJointRotation(GameObject joint)
//        {
//            return joint.transform.localEulerAngles * Mathf.Deg2Rad;
//        }

//        public double transformAngle(double angle)
//        {
//            angle = angle % (2 * Mathf.PI);

//            if (angle > Mathf.PI)
//            {
//                angle = -2 * Mathf.PI + angle;
//            }

//            if (angle < -Mathf.PI)
//            {
//                angle = 2 * Mathf.PI + angle;
//            }

//            return angle;
//        }

//    }
//}
