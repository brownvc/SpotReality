using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class SetGripper: UnityPublisher<MessageTypes.Geometry.Twist>
    {

        private MessageTypes.Geometry.Twist message;

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.Twist();

        }

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }


        // Update is called once per frame
        void Update()
        {
            
            bool secondaryButton;
            bool triggerButton;
            float triggerValue;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            Vector3 linearVelocity;
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);

            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);


            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    // Pressing "b" opens the gripper all the way
                    // Pressing the trigger slowly closes the gripper
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out secondaryButton) && secondaryButton)
                    {
                        linearVelocity = new Vector3(1.0f, 0.0f, 0.0f);
                        message.linear = new MessageTypes.Geometry.Vector3(100.0f, 0.0f, 0.0f);
                        message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
                        Debug.Log(message.linear);
                        Publish(message);
                    }
                    // Pressing the trigger slowly closes the gripper
                    else if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerButton) && triggerButton)
                    {
                        // get how hard the trigger is pressed -- won't use for now
                        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerValue);

                        // build a message that just sends -1 value
                        message.linear = new MessageTypes.Geometry.Vector3(-10.0f, 0.0f, 0.0f);
                        message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
                        Publish(message);
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
