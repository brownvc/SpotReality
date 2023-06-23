//using System.Collections.Generic;
//using UnityEngine;
//namespace RosSharp.RosBridgeClient
//{
//    public class StopPublisher : UnityPublisher<MessageTypes.Std.Bool>
//    {

//        private MessageTypes.Std.Bool message;

//        private void InitializeMessage()
//        {
//            message = new MessageTypes.Std.Bool();
//        }

//        protected override void Start()
//        {
//            base.Start();
//            InitializeMessage();
//        }


//        public void Stop()
//        {
//            message.data = true;
//            Publish(message);

//        }
//    }
//}
