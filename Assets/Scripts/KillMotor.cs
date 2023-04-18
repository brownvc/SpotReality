using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class KillMotor : UnityPublisher<MessageTypes.Std.Bool>
    {

        private MessageTypes.Std.Bool message;

        private void InitializeMessage()
        {
            message = new MessageTypes.Std.Bool();

        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }


        // Update is called once per frame
        // This function moves the robot arm according to the right controller's 3D location if the trigger is pressed
        void Update()
        {
            
            bool primaryButton;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                //if ((((uint)device.characteristics & 512) != 0))
                {
                    // If the menu button is pressed kill the motor
                    for (int i = 0; i <= 509; i++)
                    {
                        if (Input.GetKey((KeyCode)i))
                        {
                            //Debug.Log("Pressed" + i);
                        }
                    }
                    //Debug.Log(Input.GetKey(KeyCode.JoystickButton6));
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out primaryButton) && primaryButton)
                    {
                        Debug.Log("Primary pressed");
                        message.data = true;
                        Publish(message);
                    }
                }
            }
        }

    }
}
