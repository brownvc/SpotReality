using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosSharp.RosBridgeClient;
using UnityEngine;
using static RosSharp.Urdf.Link.Visual.Material;

public class imageHandler : MonoBehaviour
{

    public PriorityQueue<Texture2D> ringBufferFront = null;
    public ImageSubscriber front = null;

    /// <summary>
	/// A Queue class in which each item is associated with a Double value
	/// representing the item's priority. 
	/// Dequeue and Peek functions return item with the best priority value.
	/// </summary>
	public class PriorityQueue<T>
    {//ring buffer w max size of 20

        List<Tuple<T, double>> elements = new List<Tuple<T, double>>();


        /// <summary>
        /// Return the total number of elements currently in the Queue.
        /// </summary>
        /// <returns>Total number of elements currently in Queue</returns>
        public int Count
        {
            get { return elements.Count; }
        }


        /// <summary>
        /// Add given item to Queue and assign item the given priority value.
        /// </summary>
        /// <param name="item">Item to be added.</param>
        /// <param name="priorityValue">Item priority value as Double.</param>
        public void Enqueue(T item, double priorityValue)
        {
            elements.Add(Tuple.Create(item, priorityValue));
            elements.OrderByDescending((x => x.Item2));
            if (this.Count == 20)
            {
                this.Dequeue();
            }
        }


        /// <summary>
        /// Return lowest priority value item and remove item from Queue.
        /// </summary>
        /// <returns>Queue item with lowest priority value.</returns>
        public Tuple<T, double> Dequeue()
        {
            Tuple<T, double> lastItem = elements[elements.Count - 1];
            elements.RemoveAt(-1);
            return lastItem;
        }


        /// <summary>
        /// Return lowest priority value item without removing item from Queue.
        /// </summary>
        /// <returns>Queue item with lowest priority value.</returns>
        public T Peek()
        {
            int bestPriorityIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestPriorityIndex].Item2)
                {
                    bestPriorityIndex = i;
                }
            }

            T bestItem = elements[bestPriorityIndex].Item1;
            return bestItem;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        ringBufferFront = new PriorityQueue<Texture2D>();
        front = new ImageSubscriber();
    }

    // Update is called once per frame
    void Update()
    {//ASK ARE ABOUT OPTIMIZATION BC I MIGHT BE CALLING THIS TWICE
        (Texture2D, uint) currFront = front.ProcessMessage();
        ringBufferFront.Enqueue(currFront.Item1, currFront.Item2);
    }
    //protected override void ReceiveMessage(MessageTypes.Sensor.CompressedImage compressedImage)
    //{
    //   imageData = compressedImage.data;
    //    timeStamp = compressedImage.header.stamp.nsecs; //
    //    isMessageReceived = true;

    //}

    //public (Texture2D, uint) ProcessMessage()
    //{
    //   texture2D.LoadImage(imageData);
    //    texture2D.Apply();
    //    //Debug.Log(texture2D.height + ", " + texture2D.width);
    //    meshRenderer.material.SetTexture("_MainTex", texture2D);
    //    isMessageReceived = false;
    //    return (texture2D, timeStamp);
    //}
}
