using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;

public class DepthCompletion : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool activate_averaging;
    //public bool activate_depth_preprocess;
    public bool use_BPNet;
    public float fx, cx, fy, cy;

    public int num_frames;
    float[,] depth_buffer;
    float[,] confidence_buffer;
    int buffer_pos = 0;

    public ComputeShader average_shader;
    private ComputeBuffer depthBufferCompute;
    private ComputeBuffer confidenceBufferCompute;
    private ComputeBuffer depthArCompute;
    private ComputeBuffer confidenceArCompute;

    //public bool activate_margin_cut;
    //public int l_margin, r_margin, b_margin, t_margin;

    //public float lower_bound, upper_bound;

    public ModelAsset modelAsset;
    public ModelAsset modelAssetBP;
    Model runtimeModel;
    IWorker worker;

    float[] confidence_ar;
    float[] k_ar;

    private DateTime current_time;

    int kernel;

    // Start is called before the first frame update
    void Start()
    {
        if (use_BPNet)
        {
            runtimeModel = ModelLoader.Load(modelAssetBP);
        }
        else
        {
            runtimeModel = ModelLoader.Load(modelAsset);
        }

        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);

        confidence_buffer = new float[num_frames, 480 * 640];
        depth_buffer = new float[num_frames, 480 * 640];

        depthBufferCompute = new ComputeBuffer(num_frames * 480 * 640, sizeof(float));
        confidenceBufferCompute = new ComputeBuffer(num_frames * 480 * 640, sizeof(float));
        depthArCompute = new ComputeBuffer(480 * 640, sizeof(float));
        confidenceArCompute = new ComputeBuffer(480 * 640, sizeof(float));

        // kernel
        kernel = average_shader.FindKernel("CSMain");

        average_shader.SetInt("num_frames", num_frames);

        confidenceBufferCompute.SetData(confidence_buffer);
        depthBufferCompute.SetData(depth_buffer);
        average_shader.SetBuffer(kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(kernel, "confidence_buffer", confidenceBufferCompute);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public (float[], float[]) complete_depth(float[] depth_ar, Texture2D color_image)
    {
        current_time = DateTime.Now;
        //if (activate_depth_preprocess)
        //{
        //    depth_ar = preprocess(depth_ar);
        //}
        if (activate_depth_estimation)
        {
            if (use_BPNet) 
            {
                k_ar = new float[] { fx, 0.0f, cx, 0.0f, fy, cy, 0.0f, 0.0f, 1.0f };
                (depth_ar, confidence_ar) = bp_complete(depth_ar, color_image, k_ar);
            }
            else
            {
                (depth_ar, confidence_ar) = complete(depth_ar, color_image);
            }
        }
        else
        {
            confidence_ar = new float[480 * 640];
            for (int i = 0; i < confidence_ar.Length; i++) 
            {
                confidence_ar[i] = 1.0f;
            }
        }

        if (activate_averaging)
        { 
            average_shader.SetInt("buffer_pos", buffer_pos);

            depthArCompute.SetData(depth_ar);
            confidenceArCompute.SetData(confidence_ar);

            average_shader.SetBuffer(kernel, "depth_ar", depthArCompute);
            average_shader.SetBuffer(kernel, "confidence_ar", confidenceArCompute);

            average_shader.Dispatch(kernel, 1, 480, 1);
            depthArCompute.GetData(depth_ar);
            confidenceArCompute.GetData(confidence_ar);
            //depthBufferCompute.GetData(depth_buffer);
            //confidenceBufferCompute.GetData(confidence_buffer);

            buffer_pos = (buffer_pos + 1) % num_frames;
        }

        //if (activate_margin_cut)
        //{
        //    depth_ar = cut_margin(depth_ar);
        //}


        //if (activate_averaging)
        //{
        //    for (int i = 0; i < depth_ar.Length; i++)
        //    {
        //        depth_ar[i] = depth_ar[i] * 0.2f + depth_ar_0[i] * 0.3f + depth_ar_1[i] * 0.5f;

        //    }
        //    depth_ar = complete(depth_ar, color_image);
        //}

        UnityEngine.Debug.Log("Execution Time: " + (DateTime.Now - current_time) + " s");

        return (depth_ar, confidence_ar);
    }

    //private float[] cut_margin(float[] depth_data)
    //{
    //    int width = 640;
    //    int height = 480;

    //    // Validate input dimensions
    //    if (depth_data.Length != width * height)
    //    {
    //        Debug.LogError("Invalid depth data dimensions.");
    //        return depth_data;
    //    }

    //    // Create a copy of the depth data to modify
    //    float[] modified_data = (float[])depth_data.Clone();

    //    // Set top margin to 0
    //    for (int y = 0; y < t_margin; y++)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            modified_data[y * width + x] = 0;
    //        }
    //    }

    //    // Set bottom margin to 0
    //    for (int y = height - b_margin; y < height; y++)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            modified_data[y * width + x] = 0;
    //        }
    //    }

    //    // Set left margin to 0
    //    for (int y = 0; y < height; y++)
    //    {
    //        for (int x = 0; x < l_margin; x++)
    //        {
    //            modified_data[y * width + x] = 0;
    //        }
    //    }

    //    // Set right margin to 0
    //    for (int y = 0; y < height; y++)
    //    {
    //        for (int x = width - r_margin; x < width; x++)
    //        {
    //            modified_data[y * width + x] = 0;
    //        }
    //    }

    //    return modified_data;
    //}

    //private float[] preprocess(float[] depth_data)
    //{

    //    for (int i = 0; i < depth_data.Length; i++)
    //    {
    //        if (depth_data[i] < lower_bound || depth_data[i] > upper_bound)
    //        {
    //            depth_data[i] = 0.0f;
    //        }

    //    }
    //    return depth_data;
    //}

    private (float[], float[]) complete(float[] depth_data, Texture2D color_data)
    {
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
        TensorShape color_shape = new TensorShape(1, 3, 480, 640);

        TensorFloat depth_tensor = new TensorFloat(depth_shape, depth_data);
        TensorFloat color_tensor = TextureConverter.ToTensor(color_data, channels: 3);
        color_tensor.Reshape(color_shape);

        Dictionary<string, Tensor> input_tensors = new Dictionary<string, Tensor>()
        {
            { "rgb", color_tensor },
            { "depth", depth_tensor },
        };

        worker.Execute(input_tensors);
        //TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        //outputTensor.CompleteOperationsAndDownload();
        //float[] output_depth = outputTensor.ToReadOnlyArray();

        //foreach (var key in input_tensors.Keys)
        //{
        //    input_tensors[key].Dispose();
        //}
        //input_tensors.Clear();

        //return output_depth;

        TensorFloat depth_outputTensor = worker.PeekOutput("output_depth") as TensorFloat;
        depth_outputTensor.CompleteOperationsAndDownload();
        float[] output_depth = depth_outputTensor.ToReadOnlyArray();

        TensorFloat confidence_outputTensor = worker.PeekOutput("output_confidence") as TensorFloat;
        confidence_outputTensor.CompleteOperationsAndDownload();
        float[] output_confidence = confidence_outputTensor.ToReadOnlyArray();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        return (output_depth, output_confidence);
    }

    private (float[], float[]) bp_complete(float[] depth_data, Texture2D color_data, float[] k_data)
    {
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
        TensorShape color_shape = new TensorShape(1, 3, 480, 640);
        TensorShape k_shape = new TensorShape(1, 3, 3);

        TensorFloat depth_tensor = new TensorFloat(depth_shape, depth_data);
        TensorFloat color_tensor = TextureConverter.ToTensor(color_data, channels: 3);
        TensorFloat k_tensor = new TensorFloat(k_shape, k_data);
        color_tensor.Reshape(color_shape);

        Dictionary<string, Tensor> input_tensors = new Dictionary<string, Tensor>()
        {
            { "sparseDepth", depth_tensor },
            { "K", k_tensor },
            { "img", color_tensor },
        };

        worker.Execute(input_tensors);
        TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        outputTensor.CompleteOperationsAndDownload();
        float[] output_depth = outputTensor.ToReadOnlyArray();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        confidence_ar = new float[480 * 640];
        for (int i = 0; i < confidence_ar.Length; i++)
        {
            confidence_ar[i] = 1.0f;
        }

        return (output_depth, confidence_ar);

    }
}
