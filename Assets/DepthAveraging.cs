using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthAveraging : MonoBehaviour
{
    public ComputeShader average_shader;

    int edge_kernel;
    int mean_kernel;
    int clear_kernel;
    int median_kernel;
    int fast_median_kernel;
    int inpainting_kernel;

    int num_frames = 20;

    float[,] depth_buffer = new float[20, 480 * 640];
    private ComputeBuffer depthArCompute;
    private ComputeBuffer depthBufferCompute;

    private ComputeBuffer edgeMaskCompute;
    private ComputeBuffer gradientXCompute;
    private ComputeBuffer gradientYCompute;
    private ComputeBuffer depthArEdgeCompute;

    int buffer_pos = 0;
    private bool activate_fast_median_calculation = false;

    int groupsX = (640 + 16 - 1) / 16;
    int groupsY = (480 + 16 - 1) / 16;

    float[] output = new float[480 * 640];

    // Update is called once per frame
    void Update()
    {
        
    }

    void Start()
    {
        // kernel
        edge_kernel = average_shader.FindKernel("EdgeDetection");
        mean_kernel = average_shader.FindKernel("MeanAveraging");
        clear_kernel = average_shader.FindKernel("ClearBuffer");
        median_kernel = average_shader.FindKernel("MedianAveragingNaive");
        fast_median_kernel = average_shader.FindKernel("MedianAveragingFast");
        inpainting_kernel = average_shader.FindKernel("Inpainting");

        // Data & Buffer
        depthArCompute = new ComputeBuffer(480 * 640, sizeof(float));
        depthBufferCompute = new ComputeBuffer(480 * 640 * 20, sizeof(float));

        edgeMaskCompute = new ComputeBuffer(480 * 640, sizeof(float));
        gradientXCompute = new ComputeBuffer(480 * 640, sizeof(float));
        gradientYCompute = new ComputeBuffer(480 * 640, sizeof(float));
        depthArEdgeCompute = new ComputeBuffer(480 * 640, sizeof(float));

        average_shader.SetInt("num_frames", num_frames);
        depthBufferCompute.SetData(depth_buffer);

        average_shader.SetBuffer(mean_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(clear_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(median_kernel, "depth_buffer", depthBufferCompute);
        average_shader.SetBuffer(fast_median_kernel, "depth_buffer", depthBufferCompute);

        average_shader.SetBuffer(edge_kernel, "edge_mask", edgeMaskCompute);
        average_shader.SetBuffer(edge_kernel, "GradientX", gradientXCompute);
        average_shader.SetBuffer(edge_kernel, "GradientY", gradientYCompute);
        average_shader.SetBuffer(edge_kernel, "depth_ar_temp", depthArEdgeCompute);

        average_shader.SetBuffer(inpainting_kernel, "edge_mask", edgeMaskCompute);
        average_shader.SetBuffer(inpainting_kernel, "GradientX", gradientXCompute);
        average_shader.SetBuffer(inpainting_kernel, "GradientY", gradientYCompute);
        average_shader.SetBuffer(inpainting_kernel, "depth_ar_temp", depthArEdgeCompute);
    }

    void OnDestroy()
    {
        depthBufferCompute.Release();
        depthArCompute.Release();
    }

    public float[] averaging(float[] depth_ar, bool is_not_moving, bool mean_averaging, bool median_averaging, bool edge_detection, float edge_threshold)
    {
        float[] temp = new float[480 * 640];

        bool is_moving = !is_not_moving;

        if (!median_averaging)
        {
            activate_fast_median_calculation = false;
        }

        // aceraging && edge detection
        if (mean_averaging || median_averaging || edge_detection || is_not_moving)
        {
            average_shader.SetBool("activate", is_not_moving);   // activate = activate_averaging
            average_shader.SetInt("buffer_pos", buffer_pos);
            depthArCompute.SetData(depth_ar);

            // set buffer data CPU -> GPU
            if (edge_detection && is_not_moving)
            {
                average_shader.SetFloat("edgeThreshold", edge_threshold);
                average_shader.SetBuffer(edge_kernel, "depth_ar", depthArCompute);
                average_shader.SetBuffer(inpainting_kernel, "depth_ar", depthArCompute);
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
            else if (mean_averaging || is_moving)
            {
                average_shader.SetBuffer(mean_kernel, "depth_ar", depthArCompute);
            }

            // dispatch shader kernel
            if (edge_detection && is_not_moving)
            {
                average_shader.Dispatch(edge_kernel, groupsX, groupsY, 1);
                average_shader.Dispatch(inpainting_kernel, groupsX, groupsY, 1);
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

                if (buffer_pos == num_frames - 2)
                {
                    activate_fast_median_calculation = true;
                }
            }
            else if (mean_averaging || is_moving)
            {
                average_shader.Dispatch(mean_kernel, groupsX, groupsY, 1);
            }

            // depth GPU -> CPU
            buffer_pos = (buffer_pos + 1) % (num_frames - 1);
            depthArCompute.GetData(temp);

            return temp;
        }

        return depth_ar;
    }
}
