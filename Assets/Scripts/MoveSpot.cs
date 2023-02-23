using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
	public class MoveSpot : UnityPublisher<MessageTypes.Geometry.Twist>
	{
        private MessageTypes.Geometry.Twist message;
 	protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.Twist();
            message.linear = new MessageTypes.Geometry.Vector3();
            message.angular = new MessageTypes.Geometry.Vector3();
        }

    	// Update is called once per frame
    	void Update()
    	{

            bool triggerValue;
            Vector2 primary2DAxis;

            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);
            foreach (var device in gameControllers)
            {
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue) && triggerValue)
                {
                    Debug.Log("Trigger button is pressed.");
                    Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.5f);
                    Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
                    message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
                    message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

                    Publish(message);
                }

                if ((((uint)device.characteristics & 256) != 0) && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out primary2DAxis) && !(primary2DAxis == new Vector2(0, 0)))
                {
                    Debug.Log("Left controller 2D axis value: " + primary2DAxis);
                    Vector3 linearVelocity = new Vector3(primary2DAxis[0], 0.0f, primary2DAxis[1]);
                    Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
                    message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
                    message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

                    Publish(message);
                    //Debug.Log((uint)device.characteristics & 256);
                }

                if ((((uint)device.characteristics & 512) != 0) && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out primary2DAxis) && !(primary2DAxis == new Vector2(0, 0)))
                {
                    Debug.Log("Right controller 2D axis value: " + primary2DAxis);
                }
            }

            if (Input.GetKeyDown("w"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKeyDown("s"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, -0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKeyDown("d"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKeyDown("a"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(-0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKeyDown("e"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.5f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
       		if (Input.GetKeyDown("q"))
        	{
            	print("w was pressed");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, -0.5f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
    	}

        private static MessageTypes.Geometry.Vector3 GetGeometryVector3(Vector3 vector3)
        {
            MessageTypes.Geometry.Vector3 geometryVector3 = new MessageTypes.Geometry.Vector3();
            geometryVector3.x = vector3.x;
            geometryVector3.y = vector3.y;
            geometryVector3.z = vector3.z;
            return geometryVector3;
        }
	}
}

