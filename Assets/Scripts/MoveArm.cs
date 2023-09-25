using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class MoveArm : MonoBehaviour
    {
        public RosSharp.RosBridgeClient.PoseStampedRelativePublisher armPublisher; // Reference to RosConnnector's arm publisher
        public GameObject rightController; // Reference to right controller object
        public GameObject dummyFinger; // Reference to dummy finger object

        private MessageTypes.Geometry.Twist message;
        private bool triggerWasPressed = false;
        private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private Quaternion lastHandRotation = Quaternion.identity;


        void Start()
        {
        }


        // Update is called once per frame
        // This function moves the robot arm according to the right controller's 3D location if the trigger is pressed
        void Update()
        {
            bool gripValue;
            Vector3 locationChange;

            Quaternion rotationChange;
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

                            armPublisher.enabled = true;
                        }
                        else
                        {
                            // Change the location of the finger the same way
                            //Debug.Log(rightController.transform.localEulerAngles);
                            locationChange = (rightController.transform.position - lastHandLocation);
                            rotationChange = rightController.transform.rotation * Quaternion.Inverse(lastHandRotation);
                            //rotationChange = rightController.transform.rotation - lastHandRotation;
                            dummyFinger.transform.position += locationChange;
                            //dummyFinger.transform.rotation = rightController.transform.rotation;
                            dummyFinger.transform.rotation *= rotationChange;
                        }
                        lastHandLocation = rightController.transform.position;
                        lastHandRotation = rightController.transform.rotation;

                        //Debug.Log("Location of controller: " + rightController.transform.position);
                    }
                    else
                    {
                        // trigger is not pressed
                        triggerWasPressed = false;

                        //turn off dummy hand tracking
                        armPublisher.enabled = false;
                    }
                }
            }
        }
    }
}
