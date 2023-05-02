/*
© Siemens AG, 2018
Author: Berkay Alp Cakal (berkay_alp.cakal.ct@siemens.com)

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

using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class Float32ArraySubscriber : UnitySubscriber<MessageTypes.Std.Float32MultiArray>
    {
        public float[] data;
        public bool isColor;
        public Texture2D texture2D;
        private bool isMessageReceived;


        protected override void Start()
        {
            if (isColor)
            {
                texture2D = new Texture2D(640, 480, TextureFormat.RGBAFloat, true);
                //texture2D = new Texture2D(1, 1);
            }
            base.Start();
        }

        protected override void ReceiveMessage(MessageTypes.Std.Float32MultiArray floatarray)
        {
            //laserScanWriter.Write(laserScan);
            data = floatarray.data;
            //Debug.Log("Received Color");
            isMessageReceived = true;

        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        private void ProcessMessage()
        {
            int width = 640;
            int height = 480;
            int offset;

            //Debug.Log("Data length for topic " + Topic + ": " + data.Length);

            if (isColor)
            {
                /*
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        offset = i * width + j * 3;
                        if (offset + 2 >= width)
                        {
                            continue;
                        }
                        Color color = new Color(data[offset] / 255.0f, data[offset + 1] / 255.0f, data[offset + 2] / 255.0f);
                        texture2D.SetPixel(j, i, color);
                        var x = texture2D.GetPixel(j, i);
                        //Debug.Log(color);
                    }
                }
                */
            }

            isMessageReceived = false;
        }
    }
}