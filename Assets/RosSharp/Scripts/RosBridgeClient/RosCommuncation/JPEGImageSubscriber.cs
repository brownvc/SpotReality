/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

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

using UnityEngine;
using System;

namespace RosSharp.RosBridgeClient
{                                            
    [RequireComponent(typeof(RosConnector))]
    public class JPEGImageSubscriber : UnitySubscriber<MessageTypes.Sensor.Image>
    {
        public MeshRenderer meshRenderer;

        public Texture2D texture2D;
        private byte[] imageData;
        private bool isMessageReceived;
        private bool freezeColor = false;
        private DateTime lastMessageRetrieved;
        public bool printRate;

        protected override void Start()
        {
			base.Start();
            texture2D = new Texture2D(1, 1);
            meshRenderer.material = new Material(Shader.Find("Standard"));
            freezeColor = false;
            lastMessageRetrieved = DateTime.Now;

        }
        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            double totalSeconds;
            if (printRate)
            {
                totalSeconds = (DateTime.Now - lastMessageRetrieved).TotalSeconds;
                Debug.Log("Time between messages: " + totalSeconds.ToString("0.0000") + " seconds, " + (1 / totalSeconds).ToString("0.00") + " FPS");
            }

            imageData = image.data;
            isMessageReceived = true;
            lastMessageRetrieved = DateTime.Now;
        }

        private void ProcessMessage()
        {
            if (!freezeColor)
            { 
                texture2D.LoadImage(imageData);
                texture2D.Apply();
                meshRenderer.material.SetTexture("_MainTex", texture2D);
            }
            isMessageReceived = false;
        }

        public void toggleFreeze()
        {
            freezeColor = !freezeColor; 
        }

    }
}

