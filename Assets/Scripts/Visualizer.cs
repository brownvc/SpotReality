using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class Visualizer : UnityPublisher<MessageTypes.Geometry.PoseStamped>
    {
        private bool triggerWasPressed = false;
        private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private Quaternion lastHandRotation = Quaternion.identity;
        private GameObject rightController;
        private GameObject dummyFinger;
        public string FrameId = "Unity";
        private MessageTypes.Geometry.PoseStamped message;
        public Transform PublishedTransform;
        private Vector3 initialPos;


        protected override void Start()
        {
            rightController = GameObject.Find("RightHand Controller");
            dummyFinger = GameObject.Find("dummy_link_fngr");
            //dummyFinger.transform.position = GameObject.Find("arm0.link_fngr").transform.position;
            Debug.Log(rightController);
            base.Start();
            InitializeMessage();
            initialPos = dummyFinger.transform.position;
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.PoseStamped
            {
                header = new MessageTypes.Std.Header()
                {
                    frame_id = FrameId
                }
            };
        }
        private static void GetGeometryPoint(Vector3 position, MessageTypes.Geometry.Point geometryPoint)
        {
            geometryPoint.x = position.x;
            geometryPoint.y = position.y;
            geometryPoint.z = position.z;
        }

        private static void GetGeometryQuaternion(Quaternion quaternion, MessageTypes.Geometry.Quaternion geometryQuaternion)
        {
            geometryQuaternion.x = quaternion.x;
            geometryQuaternion.y = quaternion.y;
            geometryQuaternion.z = quaternion.z;
            geometryQuaternion.w = quaternion.w;
        }


        // Update is called once per frame
        // This function moves the robot arm according to the right controller's 3D location if the trigger is pressed
        void Update()
        {
            bool triggerValue;
            Vector3 locationChange;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    //locationChange = (rightController.transform.position - lastHandLocation);
                    dummyFinger.transform.position = rightController.transform.position + initialPos; //+= locationChange;
                    dummyFinger.transform.rotation = rightController.transform.rotation;
                    // lastHandLocation = rightController.transform.position;
                    // If the  trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue)
                    {
                        if (!triggerWasPressed)
                        {
                            triggerWasPressed = true;
                            // publish?
                            message.header.Update();
                            GetGeometryPoint(PublishedTransform.localPosition.Unity2Ros(), message.pose.position);
                            GetGeometryQuaternion(PublishedTransform.localRotation.Unity2Ros(), message.pose.orientation);

                            Publish(message);
                            triggerWasPressed = false;

                        }

                        //Debug.Log("Location of controller: " + rightController.transform.position);
                    }
                    else
                    {
                        // trigger is not pressed
                        triggerWasPressed = false;

                        //// turn off dummy hand tracking
                        //connector = GameObject.Find("RosConnector");
                        //(connector.GetComponent("PoseStampedRelativePublisher") as MonoBehaviour).enabled = false;
                    }
                }
            }
            /*bool gripValue;
            Vector3 locationChange;
            GameObject connector;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    // If the  trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out gripValue) && gripValue)
                    {
                        // If trigger is just now getting pressed, save the location but don't send a command
                        // Also turn on the publisher to track the dummy hand
                        if (!triggerWasPressed)
                        {
                            triggerWasPressed = true;

                            connector = GameObject.Find("RosConnector");
                            (connector.GetComponent("PoseStampedRelativePublisher") as MonoBehaviour).enabled = true;
                        }
                        else
                        {
                            // Change the location of the finger the same way
                            locationChange = (rightController.transform.position - lastHandLocation);
                            //rotationChange = rightController.transform.rotation - lastHandRotation;
                            dummyFinger.transform.position += locationChange;
                            dummyFinger.transform.rotation = rightController.transform.rotation;
                        }
                        lastHandLocation = rightController.transform.position;

                        //Debug.Log("Location of controller: " + rightController.transform.position);
                    }
                    else
                    {
                        // trigger is not pressed
                        triggerWasPressed = false;

                        // turn off dummy hand tracking
                        connector = GameObject.Find("RosConnector");
                        (connector.GetComponent("PoseStampedRelativePublisher") as MonoBehaviour).enabled = false;
                    }
                }
            }*/
        }
    }
}
