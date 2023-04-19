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
        void Update()
        {
            
            bool primaryButton;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out primaryButton) && primaryButton)
                    {
                        message.data = true;
                        Publish(message);
                    }
                }
            }
        }

    }
}
