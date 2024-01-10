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
using System.Linq;

namespace RosSharp.RosBridgeClient
{
    public class RawImageSubscriber: UnitySubscriber<MessageTypes.Sensor.Image>
    {
        private byte[] data;               
        private bool isMessageReceived; 
        private float[] globalData;            // final depth array returned
        private float[ , ] image_data_cbuffer; // buffer of previous frames' depth value
        private float[] image_data_avg;        // sum of non-zero values in each pixel's history
        private byte[] image_data_pixcount;    // number of non-zero values in each pixel's history
        private int image_data_cbuffer_pos;    // tracker for spot in image_data_cbuffer
        private int image_data_cbuffer_length; // number of frames to average depth over
        private bool useDepthHistory;          // whether to use depth buffer for averaging, or just use current frame
        private bool clearCbuf;                // whether to clear image_data_cbuffer at beginning of next message
        private float turnDepthOnTime;         // when to turn depth history back on        

        /* struct to hold the recency of a depth value */
        private struct DepthInfo
        {
            public DepthInfo(float d, int r)
            {
                depth = d;
                recency = r;
            }

            public float depth { get; set; }
            public int recency { get; set; }
        };
        private DepthInfo[] depthHistory;


        protected override void Start()
        {
            base.Start();

            image_data_cbuffer_pos = 0;

            //how many frames to average depth over
            image_data_cbuffer_length = 6;

            globalData = new float[1];
            useDepthHistory = true;
            clearCbuf = false;
            turnDepthOnTime = 0f;
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            DateTime start = System.DateTime.Now;
            DateTime end;
            float depthVal;
            float[] image_data = new float[1];

            data = image.data;
            isMessageReceived = true;
            
            image_data = new float[data.Length/2];

            // Initialize the buffer containing past frame values for each pixel
            if (image_data_cbuffer == null || clearCbuf)
            {
                image_data_cbuffer = new float[image_data.Length, image_data_cbuffer_length];
            }            

            byte[] bytes = new byte[2];
            int j = 0;
            for (int i = 0; i < data.Length; i+=2)
            {
                bytes[0] = data[i];
                bytes[1] = data[i+1];
                depthVal = (BitConverter.ToUInt16(bytes)) / 1000.0f;

                image_data[j] = depthVal;
                image_data_cbuffer[j, image_data_cbuffer_pos] = depthVal;
                j++;
            }

            image_data_cbuffer_pos = (image_data_cbuffer_pos + 1) % image_data_cbuffer_length;

            // Average depth frames
            if ( useDepthHistory ) // If robot is not moving
            {
                // Accumulate
                image_data_avg = new float[image_data.Length];
                image_data_pixcount = new byte[image_data.Length];
                string cbuf = "cbuf ";
                int ind = 128238;
                for (int m = 0; m < image_data_cbuffer_length; m++)
                {
                    for (j = 0; j < image_data.Length; j++)
                    {
                        if (j == ind)
                            cbuf += image_data_cbuffer[j, m] + " ";

                        // Exclude readings of zeros
                        if (image_data_cbuffer[j,m] > 0f)
                        {
                            image_data_avg[j] += image_data_cbuffer[j, m];
                            image_data_pixcount[j] += 1;
                        }
                    }

                }
                cbuf += "   avg " + image_data_avg[ind] / (image_data_pixcount[ind]+0.0001f);
                Debug.Log(cbuf);

                // Normalize
                for (j = 0; j < image_data.Length; j++)
                {
                    if (image_data_pixcount[j] > 0)
                    {
                        image_data[j] = image_data_avg[j] / image_data_pixcount[j];
                    }
                }

            }

            globalData = new float[image_data.Length];
            Array.Copy(image_data, globalData, image_data.Length);
            
            end = System.DateTime.Now;
            //Debug.Log("Depth retrieval took " + (end - start).TotalSeconds + " seconds");
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();

            // Turn depth history back on after the allotted time has passed
            if (useDepthHistory == false && UnityEngine.Time.time > turnDepthOnTime)
            {
                turnDepthOn();
            }
        }

        // Get the most recently calculated depth array
        public float[] getDepthArr()
        {
            return globalData;
        }

        // Turn off depth history for specified time
        public void pauseDepthHistory(float howLong)
        {
            useDepthHistory = false;
            clearCbuf = true;
            
            // Turn depth on after time has passed             
            turnDepthOnTime = UnityEngine.Time.time + howLong;
        }

        // Turn depth info back on
        private void turnDepthOn()
        {
            useDepthHistory = true;
            clearCbuf = false;
        }

        private void ProcessMessage()
        {
        }
    }
}