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
    }


    // =============================================================================== //
    //                               Depth Completion                                  //
    // =============================================================================== //
    public (float[], float[]) complete(float[] depth_data_0, Texture2D color_data_0, float[] depth_data_1, Texture2D color_data_1)
    {
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640);
        TensorShape color_shape = new TensorShape(1, 3, 480, 640);

        TensorFloat depth_tensor_0 = new TensorFloat(depth_shape, depth_data_0);
        TensorFloat color_tensor_0 = TextureConverter.ToTensor(color_data_0, channels: 3);
        color_tensor_0.Reshape(color_shape);

        TensorFloat depth_tensor_1 = new TensorFloat(depth_shape, depth_data_1);
        TensorFloat color_tensor_1 = TextureConverter.ToTensor(color_data_1, channels: 3);
        color_tensor_1.Reshape(color_shape);

        Dictionary<string, Tensor> input_tensors = new Dictionary<string, Tensor>()
        {
            { "rgb_0", color_tensor_0 },
            { "depth_0", depth_tensor_0 },
            { "rgb_1", color_tensor_1 },
            { "depth_1", depth_tensor_1 },
        };

        worker.Execute(input_tensors);

        TensorFloat depth_outputTensor_0 = worker.PeekOutput("output_depth_0") as TensorFloat;
        depth_outputTensor_0.CompleteOperationsAndDownload();
        float[] output_depth_0 = depth_outputTensor_0.ToReadOnlyArray();

        TensorFloat depth_outputTensor_1 = worker.PeekOutput("output_depth_1") as TensorFloat;
        depth_outputTensor_1.CompleteOperationsAndDownload();
        float[] output_depth_1 = depth_outputTensor_1.ToReadOnlyArray();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        depth_tensor_0.Dispose();
        color_tensor_0.Dispose();
        depth_outputTensor_0.Dispose();

        depth_tensor_1.Dispose();
        color_tensor_1.Dispose();
        depth_outputTensor_1.Dispose();

        return (output_depth_0, output_depth_1);
    }
}
