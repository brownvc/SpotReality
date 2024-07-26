using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class DepthCompletion : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool activate_averaging;
    public bool activate_depth_preprocess;

    public float lower_bound, upper_bound;

    public ModelAsset modelAsset;
    Model runtimeModel;
    IWorker worker;


    // Start is called before the first frame update
    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public float[] complete_depth(float[] depth_ar, Texture2D color_image)
    {
        if (activate_depth_preprocess)
        {
            depth_ar = preprocess(depth_ar);
        }
        if (activate_depth_estimation)
        {
            depth_ar = complete(depth_ar, color_image);
        }
        //if (activate_averaging)
        //{
        //    for (int i = 0; i < depth_ar.Length; i++)
        //    {
        //        depth_ar[i] = depth_ar[i] * 0.2f + depth_ar_0[i] * 0.3f + depth_ar_1[i] * 0.5f;

        //    }
        //    depth_ar = complete(depth_ar, color_image);
        //}

        return depth_ar;
    }

    private float[] preprocess(float[] depth_data)
    {

        for (int i = 0; i < depth_data.Length; i++)
        {
            if (depth_data[i] < lower_bound || depth_data[i] > upper_bound)
            {
                depth_data[i] = 0.0f;
            }

        }
        return depth_data;
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
        TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        outputTensor.CompleteOperationsAndDownload();
        float[] output_depth = outputTensor.ToReadOnlyArray();

        foreach (var key in input_tensors.Keys)
        {
            input_tensors[key].Dispose();
        }
        input_tensors.Clear();

        return output_depth;
    }
}
