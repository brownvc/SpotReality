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
    public ModelAsset modelAsset;
    Model runtimeModel;
    IWorker worker;

    //// =============================================================================== //
    ////                               Init & OnRelease                                  //
    //// =============================================================================== //
    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
    }

    void OnDestroy()
    {
        worker.Dispose();
        if (runtimeModel != null)
        {
            runtimeModel = null;
        }
    }

    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    public (float[], float[], float[], float[]) complete(float[] depth_data_0, Texture2D color_data_0, 
        float[] depth_data_1, Texture2D color_data_1, 
        float[] depth_data_2, Texture2D color_data_2,
        float[] depth_data_3, Texture2D color_data_3)
    {
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
        TensorShape color_shape = new TensorShape(1, 3, 480, 640);

        using TensorFloat depth_tensor_0 = new TensorFloat(depth_shape, depth_data_0);
        using TensorFloat color_tensor_0 = TextureConverter.ToTensor(color_data_0, channels: 3);
        color_tensor_0.Reshape(color_shape);

        using TensorFloat depth_tensor_1 = new TensorFloat(depth_shape, depth_data_1);
        using TensorFloat color_tensor_1 = TextureConverter.ToTensor(color_data_1, channels: 3);
        color_tensor_1.Reshape(color_shape);

        using TensorFloat depth_tensor_2 = new TensorFloat(depth_shape, depth_data_2);
        using TensorFloat color_tensor_2 = TextureConverter.ToTensor(color_data_2, channels: 3);
        color_tensor_2.Reshape(color_shape);

        using TensorFloat depth_tensor_3 = new TensorFloat(depth_shape, depth_data_3);
        using TensorFloat color_tensor_3 = TextureConverter.ToTensor(color_data_3, channels: 3);
        color_tensor_3.Reshape(color_shape);

        Dictionary<string, Tensor> input_tensors = new Dictionary<string, Tensor>()
        {
            { "rgb_0", color_tensor_0 },
            { "depth_0", depth_tensor_0 },
            { "rgb_1", color_tensor_1 },
            { "depth_1", depth_tensor_1 },
            { "rgb_2", color_tensor_2 },
            { "depth_2", depth_tensor_2 },
            { "rgb_3", color_tensor_3 },
            { "depth_3", depth_tensor_3 },
        };

        worker.Execute(input_tensors);

        using TensorFloat depth_outputTensor_0 = worker.PeekOutput("output_depth_0") as TensorFloat;
        depth_outputTensor_0.CompleteOperationsAndDownload();
        float[] output_depth_0 = depth_outputTensor_0.ToReadOnlyArray();

        using TensorFloat depth_outputTensor_1 = worker.PeekOutput("output_depth_1") as TensorFloat;
        depth_outputTensor_1.CompleteOperationsAndDownload();
        float[] output_depth_1 = depth_outputTensor_1.ToReadOnlyArray();

        using TensorFloat depth_outputTensor_2 = worker.PeekOutput("output_depth_2") as TensorFloat;
        depth_outputTensor_2.CompleteOperationsAndDownload();
        float[] output_depth_2 = depth_outputTensor_2.ToReadOnlyArray();

        using TensorFloat depth_outputTensor_3 = worker.PeekOutput("output_depth_3") as TensorFloat;
        depth_outputTensor_3.CompleteOperationsAndDownload();
        float[] output_depth_3 = depth_outputTensor_3.ToReadOnlyArray();

        foreach (var tensor in input_tensors.Values)
        {
            tensor.Dispose();
        }

        return (output_depth_0, output_depth_1, output_depth_2, output_depth_3);
    }
}