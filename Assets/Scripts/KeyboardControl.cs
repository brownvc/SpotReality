using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
	public class KeyboardControl : UnityPublisher<MessageTypes.Geometry.Twist>
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

