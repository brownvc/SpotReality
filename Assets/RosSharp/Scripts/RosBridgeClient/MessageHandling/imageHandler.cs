using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using RosSharp.RosBridgeClient;
using UnityEngine;
using static RosSharp.Urdf.Link.Visual.Material;

public class imageHandler : MonoBehaviour
{

    public PriorityQueue<Texture2D> ringBufferFront = null;
    public JPEGImageSubscriber front = null;

    public RawImageSubscriber frontDepth = null;

    public PriorityQueue<Texture2D> ringBufferBack = null;
    public JPEGImageSubscriber back = null;
    public RawImageSubscriber backDepth = null;

    public PriorityQueue<Texture2D> ringBufferLeft = null;
    public JPEGImageSubscriber left = null;
    public RawImageSubscriber leftDepth = null;

    public PriorityQueue<Texture2D> ringBufferRight = null;
    public JPEGImageSubscriber right = null;
    public RawImageSubscriber rightDepth = null;

    public PriorityQueue<Texture2D> ringBufferFrontRight = null;
    public JPEGImageSubscriber frontRight = null;
    public RawImageSubscriber frontRightDepth = null;

    public PriorityQueue<Texture2D> ringBufferFrontLeft = null;
    public JPEGImageSubscriber frontLeft = null;
    public RawImageSubscriber frontLeftDepth = null;

    public PriorityQueue<Texture2D> ringBufferHand = null;
    public JPEGImageSubscriber hand = null;
    public RawImageSubscriber handDepth = null;

    public Texture2D backTexture = null;
    public Texture2D leftTexture = null;
    public Texture2D rightTexture = null;
    public Texture2D frontRightTexture = null;
    public Texture2D frontLeftTexture = null;
    public Texture2D handTexture = null;

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
        ringBufferBack = new PriorityQueue<Texture2D>();
        back = new JPEGImageSubscriber();
        backDepth = new RawImageSubscriber();
        ringBufferLeft = new PriorityQueue<Texture2D>();
        left = new JPEGImageSubscriber();
        leftDepth = new RawImageSubscriber();
        ringBufferRight = new PriorityQueue<Texture2D>();
        right = new JPEGImageSubscriber();
        rightDepth = new RawImageSubscriber();
        ringBufferFrontRight = new PriorityQueue<Texture2D>();
        frontRight = new JPEGImageSubscriber();
        frontRightDepth = new RawImageSubscriber();
        ringBufferFrontLeft = new PriorityQueue<Texture2D>();
        frontLeft = new JPEGImageSubscriber();
        frontLeftDepth = new RawImageSubscriber();
        ringBufferHand = new PriorityQueue<Texture2D>();
        hand = new JPEGImageSubscriber();
        handDepth = new RawImageSubscriber();
    }
    Texture2D handleBuffer(JPEGImageSubscriber cam, RawImageSubscriber depth, PriorityQueue<Texture2D> ringBuffer) {
        (Texture2D, uint) currCam = cam.ProcessMessage();
        ringBuffer.Enqueue(currCam.Item1, currCam.Item2);
        if (depth.isMessageReceived) {
            uint depthTime = depth.timeStamp; //in nanoseconds
            double optimalFrameDiff = 10000000;
            Texture2D optimalFrameTexture = null;
            for (int i = 0; i < ringBuffer.Count; i++) {
                System.Tuple<Texture2D, double> curr = ringBuffer.Dequeue();
                double currBest = Math.Abs(curr.Item2 - depthTime);
                if (currBest < optimalFrameDiff) {
                    optimalFrameDiff = currBest;
                    optimalFrameTexture = curr.Item1;
                }
            }
            return optimalFrameTexture;
        };
        return null;
    }
    // Update is called once per frame
    void Update()
    {//ASK ARE ABOUT OPTIMIZATION BC I MIGHT BE CALLING THIS TWICE
        backTexture = handleBuffer(back, backDepth, ringBufferBack);
        leftTexture = handleBuffer(left, leftDepth, ringBufferLeft);
        rightTexture = handleBuffer(right, rightDepth, ringBufferRight);
        frontRightTexture = handleBuffer(frontRight, frontRightDepth, ringBufferFrontRight);
        frontLeftTexture = handleBuffer(frontLeft, frontLeftDepth, ringBufferFrontLeft);
        handTexture = handleBuffer(hand, handDepth, ringBufferHand);

        //return new Texture2D[] { handTexture, frontLeftTexture, frontRightTexture, backTexture, rightTexture, leftTexture };
    }
}
