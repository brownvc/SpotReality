/*
© Siemens AG, 2018
Author: Berkay Alp Cakal (berkay_alp.cakal.ct@siemens.com)

Modified by Brown University

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

//using System.Diagnostics;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using static RosSharp.Urdf.Link.Visual.Material;

namespace RosSharp.RosBridgeClient
{
    public class DepthImageSubscriber : UnitySubscriber<MessageTypes.Sensor.Image>
    {
        //public MessageTypes.Sensor.Image image;

        public Texture2D texture2D;
        private byte[] imageData;
        private bool isMessageReceived;
        public uint height;
        public uint width;
        public MeshRenderer meshRenderer;

        // placeholder to get image data to work

        protected override void Start()
        {
			base.Start();
            texture2D = new Texture2D(1, 1);
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image inputImage)
        {
            imageData = inputImage.data;
            isMessageReceived = true;

            //laserScanWriter.Write(laserScan);

            //// receive the image
            //imageData = new float[inputImage.height * inputImage.step];
            //height = inputImage.height;
            //width = inputImage.step;
            //byte[] bytes = new byte[4];

            //// hypothesis -- Input image is encoded as two bytes per pixel, which together combine to a float depth
            //for (int i = 0; i < inputImage.data.Length; i+=2)
            //{
            //    // build a float from the bytes
            //    bytes[0] = inputImage.data[i];
            //    bytes[1] = inputImage.data[i+1];
            //    //imageData[i/2] = System.BitConverter.ToSingle(bytes, 0);
            //    imageData[i] = (float)inputImage.data[i];
            //}

            //bytes[0] = 0;
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        private void ProcessMessage()
        {
            texture2D.LoadRawTextureData(imageData);
            //texture2D.LoadImage(imageData);
            texture2D.Apply();
            Debug.Log(texture2D.height + ", " + texture2D.width);
            meshRenderer.material.SetTexture("_MainTex", texture2D);
            isMessageReceived = false;
        }
    }
}