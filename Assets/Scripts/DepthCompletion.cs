using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using static Unity.Sentis.Model;
using System;
using UnityEngine.VFX;
using System.Runtime.Remoting.Messaging;

public class DepthCompletion : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool mean_averaging;
    public bool median_averaging;
    public bool edge_detection;

    public float edge_threshold;

    public bool use_BPNet;
    public float fx, cx, fy, cy;

    private bool activate_fast_median_calculation = false;

    public int num_frames;
    float[,] depth_buffer;
    int buffer_pos = 0;

    public ComputeShader average_shader;
    private ComputeBuffer depthBufferCompute;
    private ComputeBuffer depthArCompute;

    public ModelAsset modelAsset;
    public ModelAsset modelAssetBP;
    Model runtimeModel;
    IWorker worker;

    float[] k_ar;

    private DateTime current_time;
    bool clear_buffer = false;

    int edge_kernel;
    int mean_kernel;
    int clear_kernel;
    int median_kernel;
    int fast_median_kernel;

    int groupsX = (640 + 16 - 1) / 16;
    int groupsY = (480 + 16 - 1) / 16;

    float[] output = new float[480 * 640];

    public bool buffer_prepare_status()
    {
        return activate_fast_median_calculation; 
    }

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
        depth_buffer = new float[num_frames, 480 * 640];

        depthBufferCompute = new ComputeBuffer(num_frames * 480 * 640, sizeof(float));
        depthArCompute = new ComputeBuffer(480 * 640, sizeof(float));

        // kernel
        edge_kernel = average_shader.FindKernel("EdgeDetection");
        mean_kernel = average_shader.FindKernel("MeanAveraging");
        clear_kernel = average_shader.FindKernel("ClearBuffer");
        median_kernel = average_shader.FindKernel("MedianAveragingNaive");
        fast_median_kernel = average_shader.FindKernel("MedianAveragingFast");

        average_shader.SetInt("num_frames", num_frames);
        depthBufferCompute.SetData(depth_buffer);

        average_shader.SetBuffer(mean_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(clear_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(median_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(fast_median_kernel, "depth_buffer", depthBufferCompute);
    }

    // Update is called once per frame
    void Update()
    {
    }

    // close, weighted, median
    public void switch_averaging_mode()
    {
        if (!mean_averaging && !median_averaging)
        {
            mean_averaging = true;
            median_averaging = false;
        }
        else if (mean_averaging && !median_averaging) 
        {
            mean_averaging = false;
            median_averaging = true;
        }
        else
        {
            mean_averaging = false;
            median_averaging = false;
        }

        GetComponent<DrawMeshInstanced>().continue_update();
        activate_fast_median_calculation = false;
        buffer_pos = 0;
        clear_buffer = true;
    }
    
    public void switch_depth_setimation_mode()
    {
        activate_depth_estimation = !activate_depth_estimation;
        GetComponent<DrawMeshInstanced>().continue_update();
        activate_fast_median_calculation = false;
        buffer_pos = 0;
        clear_buffer = true;
    }

    public float[] complete_depth(float[] depth_ar, Texture2D color_image)
    {
        //if (depth_ar == null || depth_ar.Length != 480 * 640)
        //{
        //    depth_ar = new float[480 * 640];
        //}

        //if (color_image == null || color_image.width < 640 || color_image.height < 480)
        //{
        //    color_image = new Texture2D(640, 480);
        //}

        if (median_averaging && mean_averaging)
        {
            mean_averaging = false;
        }

        current_time = DateTime.Now;
        if (activate_depth_estimation)
        {
            if (use_BPNet) 
            {
                k_ar = new float[] { fx, 0.0f, cx, 0.0f, fy, cy, 0.0f, 0.0f, 1.0f };
                depth_ar = bp_complete(depth_ar, color_image, k_ar);
            }
            else
            {
                depth_ar = complete(depth_ar, color_image);
            }

            if (clear_buffer)
            {
                average_shader.Dispatch(clear_kernel, groupsX, groupsY, 1);
                clear_buffer = false;
            }
        }

        if (mean_averaging || median_averaging || edge_detection)
        {
            average_shader.SetInt("buffer_pos", buffer_pos);
            depthArCompute.SetData(depth_ar);

            if (edge_detection)
            {
                average_shader.SetFloat("edgeThreshold", edge_threshold);
                average_shader.SetBuffer(edge_kernel, "depth_ar", depthArCompute);
            }

            if (mean_averaging)
            {
                average_shader.SetBuffer(mean_kernel, "depth_ar", depthArCompute);
            }

            if (median_averaging)
            {
                if (activate_fast_median_calculation)
                {
                    average_shader.SetBuffer(median_kernel, "depth_ar", depthArCompute);
                }
                else
                {
                    average_shader.SetBuffer(median_kernel, "depth_ar", depthArCompute);
                }
            }

            if (edge_detection)
            {
                average_shader.Dispatch(edge_kernel, groupsX, groupsY, 1);
            }

            if (mean_averaging) 
            {
                average_shader.Dispatch(mean_kernel, groupsX, groupsY, 1);
            }

            if (median_averaging)
            {
                if (activate_fast_median_calculation)
                {
                    average_shader.Dispatch(median_kernel, groupsX, groupsY, 1);
                }
                else
                {
                    average_shader.Dispatch(median_kernel, groupsX, groupsY, 1);
                }   
            }

            if (median_averaging && buffer_pos == num_frames - 2)
            {
                activate_fast_median_calculation = true;
            }
            buffer_pos = (buffer_pos + 1) % (num_frames - 1);

            depthArCompute.GetData(output);
            return output;
        }

        return depth_ar;
    }

    private float[] complete(float[] depth_data, Texture2D color_data)
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

        TensorFloat depth_outputTensor = worker.PeekOutput("output_depth") as TensorFloat;
        depth_outputTensor.CompleteOperationsAndDownload();
        float[] output_depth = depth_outputTensor.ToReadOnlyArray();

        TensorFloat confidence_outputTensor = worker.PeekOutput("output_confidence") as TensorFloat;
        confidence_outputTensor.CompleteOperationsAndDownload();
        float[] output_confidence = depth_outputTensor.ToReadOnlyArray();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        depth_tensor.Dispose();
        color_tensor.Dispose();
        depth_outputTensor.Dispose();
        confidence_outputTensor.Dispose();

        return output_depth;
    }

    private float[] bp_complete(float[] depth_data, Texture2D color_data, float[] k_data)
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

        // manage mem
        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        depth_tensor.Dispose();
        color_tensor.Dispose();
        k_tensor.Dispose();
        outputTensor.Dispose();

        return output_depth;

    }

    void OnDestroy()
    {
        depthBufferCompute.Release();
        depthArCompute.Release();
        worker.Dispose();
    }
}
