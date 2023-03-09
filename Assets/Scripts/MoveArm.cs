using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class MoveArm : UnityPublisher<MessageTypes.Geometry.Twist>
    {

        private MessageTypes.Geometry.Twist message;
        private bool triggerWasPressed = false;
        private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private GameObject rightController;

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.Twist();
            message.linear = new MessageTypes.Geometry.Vector3();
            message.angular = new MessageTypes.Geometry.Vector3();
        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
            rightController = GameObject.Find("RightHand Controller");
            Debug.Log(rightController);
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
                    // If the  trigger is pressed, we want to start tracking the position of the arm and sending it to Spot
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue)
                    {
                        // If trigger is just now getting pressed, save the location but don't send a command
                        if (!triggerWasPressed)
                        {
                            triggerWasPressed = true;
                            lastHandLocation = rightController.transform.position; 
                        }
                        else
                        {
                            // build a command based on position change
                            locationChange = (rightController.transform.position - lastHandLocation) / 2.0f;
                            message.linear = GetGeometryVector3(locationChange.Unity2Ros());
                            message.angular = GetGeometryVector3(new Vector3(0.0f, 0.0f, 0.0f).Unity2Ros());
                            Publish(message);
                        }

                        Debug.Log("Location of controller: " + rightController.transform.position);
                    }
                    else
                    {
                        // trigger is not pressed
                        triggerWasPressed = false;    
                    }
                }
            }
        }

        public static MessageTypes.Geometry.Vector3 GetGeometryVector3(Vector3 vector3)
        {
            MessageTypes.Geometry.Vector3 geometryVector3 = new MessageTypes.Geometry.Vector3();
            geometryVector3.x = vector3.x;
            geometryVector3.y = vector3.y;
            geometryVector3.z = vector3.z;
            return geometryVector3;
        }
    }
}
