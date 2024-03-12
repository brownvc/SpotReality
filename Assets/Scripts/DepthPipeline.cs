using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthPipeline : MonoBehaviour
{
    public ComputeShader DilateEmptyShader;
    public ComputeShader MedianBlurShader;
    public ComputeShader BilateralFilterShader;

    private RenderTexture depthTexture;
    private ComputeBuffer depthBuffer;

    private int H = 480;
    private int W = 640;

    public float[] PreprocessDepth(float[] depth)
    {
        int kernel;

        depthBuffer = new ComputeBuffer(depth.Length, sizeof(float));
        depthBuffer.SetData(depth);

        // [NV] - Unabridged version of pipeline on branch depth_origin
        // Median blur to remove outliers
        kernel = MedianBlurShader.FindKernel("CSMain");
        MedianBlurShader.SetBuffer(kernel, "depth", depthBuffer);
        MedianBlurShader.Dispatch(kernel, 1, H, 1);

        // Hole fill
        kernel = DilateEmptyShader.FindKernel("CSMain");
        DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
        DilateEmptyShader.SetInt("kernelSize", 9);
        DilateEmptyShader.Dispatch(kernel, 1, H, 1);

        // Fill large holes with masked dilations
        for (int i = 0; i < 3; i++)
        {
            DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
            DilateEmptyShader.SetInt("kernelSize", 5);
            DilateEmptyShader.Dispatch(kernel, 1, H, 1);
        }

        // Median blur
        kernel = MedianBlurShader.FindKernel("CSMain");
        MedianBlurShader.SetBuffer(kernel, "depth", depthBuffer);
        MedianBlurShader.Dispatch(kernel, 1, H, 1);

        kernel = BilateralFilterShader.FindKernel("CSMain");
        BilateralFilterShader.SetBuffer(kernel, "depth", depthBuffer);
        BilateralFilterShader.Dispatch(kernel, 1, H, 1);

        float[] res = new float[depth.Length];
        depthBuffer.GetData(res);

        depthBuffer.Release();

        return res;
    }
}
