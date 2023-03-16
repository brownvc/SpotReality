using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class MoveArm : MonoBehaviour
    {

        private MessageTypes.Geometry.Twist message;
        private bool triggerWasPressed = false;
        private Vector3 lastHandLocation = new Vector3(0.0f, 0.0f, 0.0f);
        private GameObject rightController;
        private GameObject dummyFinger;


        void Start()
        {
            rightController = GameObject.Find("RightHand Controller");
            dummyFinger = GameObject.Find("dummy_link_fngr");
            //dummyFinger.transform.position = GameObject.Find("arm0.link_fngr").transform.position;
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
                        }
                        else
                        {
                            // Change the location of the finger the same way
                            locationChange = (rightController.transform.position - lastHandLocation);
                            dummyFinger.transform.position += locationChange;
                        }
                        lastHandLocation = rightController.transform.position;

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
    }
}
