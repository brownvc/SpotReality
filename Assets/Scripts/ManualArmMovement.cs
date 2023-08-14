using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace RosSharp.RosBridgeClient
{
    public class ManualArmMovement : UnityPublisher<MessageTypes.Std.Float64MultiArray>
    {
        public bool controlWithInpector;
        public bool sending;
        public bool useTransformAngle;

        //input all 6 slider values
        public ManualRotationSlider sh0Slider;
        public ManualRotationSlider sh1Slider;
        public ManualRotationSlider el0Slider;
        public ManualRotationSlider el1Slider;
        public ManualRotationSlider wr0Slider;
        public ManualRotationSlider wr1Slider;

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

        [Range(-180, 180)]
        public double sh0;

        //for debugging purposes
        [SerializeField]
        private double[] ghostArmRads;
        [SerializeField]
        private double[] realArmDegrees;
        [SerializeField]
        private double[] ghostArmDegrees;
        [SerializeField]
        private double[] realArmRads;
        [SerializeField]
        private double[] published;

        private MessageTypes.Std.Float64MultiArray message;

        //turn on/off kinematics to allow manual control of ghost arms
        //this script is enabled/disabled by 'control mode - manual arm movement' when toggling modes
        public void OnEnable()
        {

            armObjGhost = GameObject.Find("ghost arm");
            shoulder1Ghost = GameObject.Find("newarm0.link_sh1");
            elbow0Ghost = GameObject.Find("newarm0.link_el0");
            elbow1Ghost = GameObject.Find("newarm0.link_el1");
            wrist0Ghost = GameObject.Find("newarm0.link_wr0");
            wrist1Ghost = GameObject.Find("newarm0.link_wr1");
            fingerGhost = GameObject.Find("newarm0.link_fngr");

            armObjGhost.GetComponent<Rigidbody>().isKinematic = true;
            shoulder1Ghost.GetComponent<Rigidbody>().isKinematic = true;
            elbow0Ghost.GetComponent<Rigidbody>().isKinematic = true;
            elbow1Ghost.GetComponent<Rigidbody>().isKinematic = true;
            wrist0Ghost.GetComponent<Rigidbody>().isKinematic = true;
            wrist1Ghost.GetComponent<Rigidbody>().isKinematic = true;
            fingerGhost.GetComponent<Rigidbody>().isKinematic = true;
        }
        public void OnDisable()
        {
            armObjGhost.GetComponent<Rigidbody>().isKinematic = false;
            shoulder1Ghost.GetComponent<Rigidbody>().isKinematic = false;
            elbow0Ghost.GetComponent<Rigidbody>().isKinematic = false;
            elbow1Ghost.GetComponent<Rigidbody>().isKinematic = false;
            wrist0Ghost.GetComponent<Rigidbody>().isKinematic = false;
            wrist1Ghost.GetComponent<Rigidbody>().isKinematic = false;
            fingerGhost.GetComponent<Rigidbody>().isKinematic = false;
        }

        protected override void Start()
        {
            
            sending = true;

            realArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
            ghostArmRads = new double[] { 0, 0, 0, 0, 0, 0 };
            realArmDegrees = new double[] { 0, 0, 0, 0, 0, 0 };
            ghostArmDegrees = new double[] { 0, 0, 0, 0, 0, 0 };
            //published = new double[] { 0, 0, 0, 0, 0, 0 };

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
            ManualJointRotation();
        }


        public void ManualJointRotation()
        {
            string orderOfJointAxes = "yxxzxz";

            ghostArmDegrees[0] = sh0;


            if (controlWithInpector)
            {
                //GET INPUT FROM SLIDERS
                ghostArmRads[0] = sh0 * Mathf.Deg2Rad;
                ghostArmRads[1] = sh1Slider.sliderVal;
                ghostArmRads[2] = el0Slider.sliderVal;
                ghostArmRads[3] = el1Slider.sliderVal;
                ghostArmRads[4] = wr0Slider.sliderVal;
                ghostArmRads[5] = wr1Slider.sliderVal;
            }
            else
            {
                //GET INPUT FROM SLIDERS
                ghostArmRads[0] = sh0Slider.sliderVal;
                ghostArmRads[1] = sh1Slider.sliderVal;
                ghostArmRads[2] = el0Slider.sliderVal;
                ghostArmRads[3] = el1Slider.sliderVal;
                ghostArmRads[4] = wr0Slider.sliderVal;
                ghostArmRads[5] = wr1Slider.sliderVal;
            }

            //ROTATE GHOST ARM ANGLES
            armObjGhost.transform.localEulerAngles = new Vector3(0, (float)(-1.0 * ghostArmRads[0] * Mathf.Rad2Deg), 0);
            shoulder1Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[1] * Mathf.Rad2Deg), 0, 0);
            elbow0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[2] * Mathf.Rad2Deg), 0, 0);
            elbow1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[3] * Mathf.Rad2Deg));
            wrist0Ghost.transform.localEulerAngles = new Vector3((float)(ghostArmRads[4] * Mathf.Rad2Deg), 0, 0);
            wrist1Ghost.transform.localEulerAngles = new Vector3(0, 0, (float)(-1.0 * ghostArmRads[5] * Mathf.Rad2Deg));
            //bound the angles to -pi to pi

            if (useTransformAngle)
            {
                for (int i = 0; i < 6; i++)
                    published[i] = transformAngle(ghostArmRads[i]);
            }
            else
            {
                published = ghostArmRads;
            }

            //send the angles to spot
            if (sending)
            {
                message.data = published;
                //Debug.Log("sending");
                Publish(message);
            }

            //FOR DEBUGGING PURPOSES
            //recieve real arm's angles in radians,, view in inspector
            realArmRads[0] = getJointRotation(armObjReal).y;
            realArmRads[1] = getJointRotation(shoulder1Real).x;
            realArmRads[2] = getJointRotation(elbow0Real).x;
            realArmRads[3] = getJointRotation(elbow1Real).z;
            realArmRads[4] = getJointRotation(wrist0Real).x;
            realArmRads[5] = getJointRotation(wrist1Real).z;
            //see real arm's angles in degrees,, view in inspector
            for (int i = 0; i < 6; i++)
            {
                realArmDegrees[i] = realArmRads[i] * Mathf.Rad2Deg;
            }


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

