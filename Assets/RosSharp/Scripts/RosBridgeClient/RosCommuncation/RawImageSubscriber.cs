/*
Modified from ImageSubscriber, which was written by
© Siemens AG, 2018
Author: Berkay Alp Cakal (berkay_alp.cakal.ct@siemens.com)

This script was created by Brown University

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using System.Collections;

namespace RosSharp.RosBridgeClient
{
    public class RawImageSubscriber: UnitySubscriber<MessageTypes.Sensor.Image>
    {
        public byte[] data;
        private bool isMessageReceived;
        public float[] image_data;

        public uint timeStamp;


        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            //laserScanWriter.Write(laserScan);
            data = image.data;
            //Debug.Log("Received Color");
            isMessageReceived = true; 
            image_data = new float[data.Length/2];

            byte[] bytes = new byte[2];
            int j = 0;
            for (int i = 0; i < data.Length; i+=2)
            {
                //value[0] = imageData[i];
                //value[1] = imageData[i + 1];
                bytes[0] = data[i];
                bytes[1] = data[i+1];
                image_data[j++] = (BitConverter.ToUInt16(bytes))/1000.0f;
                // image_data[i] = (float)(data[i]);// + imageData[i + 1]);// System.BitConverter.ToSingle(value, 0);
            }
            timeStamp = image.header.stamp.nsecs;
            isMessageReceived = true;
            //Debug.Log("Message Received, Time: " + UnityEngine.Time.realtimeSinceStartup);
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        private void ProcessMessage()
        {
            isMessageReceived = false;
        }
    }
}