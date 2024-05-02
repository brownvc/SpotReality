
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RosSharp.RosBridgeClient
{
    public class JoyArmPublisher : UnityPublisher<MessageTypes.Geometry.Twist>
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

        //Press 'O' to open
        //Press 'P' to close
        private void Update()
        {
            
        }


 

        public void setCoordinate(float x, float y, float z)
        {
            Vector3 angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            message.linear = new MessageTypes.Geometry.Vector3(x, y, z);
            message.angular = new MessageTypes.Geometry.Vector3(0.0f, 0.0f, 0.0f);

            //message.angular = GetGeometryVector3(-angularVelocity.Unity2Ros());
            Publish(message);
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
