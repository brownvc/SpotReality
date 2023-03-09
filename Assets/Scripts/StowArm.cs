using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class StowArm : UnityPublisher<MessageTypes.Std.Bool>
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
            bool primary2DAxisClick;
            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);

            foreach (var device in gameControllers)
            {
                // check if this is the right hand controller
                if ((((uint)device.characteristics & 512) != 0))
                {
                    // If the menu button is pressed stow the arm
                    if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out primary2DAxisClick) && primary2DAxisClick)
                    {
                        Debug.Log("Stow arm");
                        message.data = true;
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
