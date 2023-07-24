using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
	public class MoveSpot : UnityPublisher<MessageTypes.Geometry.Twist>
	{
        private MessageTypes.Geometry.Twist message;
        public bool save;

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

            Vector2 primary2DAxis;
            bool publish = false;

            var gameControllers = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, gameControllers);
            foreach (var device in gameControllers)
            {
                if ((((uint)device.characteristics & 256) != 0) && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out primary2DAxis) && !(primary2DAxis == new Vector2(0, 0)))
                {
                    //Debug.Log("Left controller 2D axis value: " + primary2DAxis);
                    Vector3 linearVelocity = new Vector3(primary2DAxis[0], 0.0f, primary2DAxis[1]);
                    Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
                    message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
                    message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
                    publish = true;
                }

                if ((((uint)device.characteristics & 512) != 0) && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out primary2DAxis) && !(primary2DAxis == new Vector2(0, 0)))
                {
                    //Debug.Log("Right controller 2D axis value: " + primary2DAxis);

                    Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
                    Vector3 angularVelocity = new Vector3(0.0f, primary2DAxis[0],0.0f);

                    //Don't change linear velocity if already publishing
                    if (!publish)
                    {
                        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
                    }
                    message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

                    publish = true;
                }
            }

            if (publish)
            {
                Publish(message);
            }

            if (Input.GetKey("w"))
        	{
            	print("move forward");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f,  0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("s"))
        	{
            	print("move back");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, -0.5f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("d"))
        	{
            	print("move right");
            	Vector3 linearVelocity = new Vector3(0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("a"))
        	{
            	print("move left");
            	Vector3 linearVelocity = new Vector3(-0.5f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());


            	Publish(message);
        	}

        	if (Input.GetKey("e"))
        	{
            	print("rotate right");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, 0.5f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
       		if (Input.GetKey("q"))
        	{
            	print("rotate left");
            	Vector3 linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            	Vector3 angularVelocity = new Vector3(0.0f, -0.5f, 0.0f);
	        message.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
            	message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());

            	Publish(message);
        	}
            if (Input.GetKeyDown("i"))
            {
                Debug.Log("Saving");
                save = true;
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

