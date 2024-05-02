using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthPipeline : MonoBehaviour
{
    public ComputeShader DilateEmptyShader;
    public ComputeShader MedianBlurShader;
    public ComputeShader BilateralFilterShader;
    public ComputeShader HysteresisThresholdShader;

    private RenderTexture depthTexture;
    private ComputeBuffer depthBuffer;

    private int H = 480;
    private int W = 640;

    public float[] PreprocessDepth(float[] depth)
    {
        int kernel;

        depthBuffer = new ComputeBuffer(depth.Length, sizeof(float));
        depthBuffer.SetData(depth);

        // Hysteresis thresholding
        ComputeBuffer maskBuffer = new ComputeBuffer(depth.Length, sizeof(int));

        kernel = HysteresisThresholdShader.FindKernel("CSMain");
        HysteresisThresholdShader.SetBuffer(kernel, "depth", depthBuffer);
        HysteresisThresholdShader.SetBuffer(kernel, "mask", maskBuffer);
        HysteresisThresholdShader.SetFloat("depthThreshold", 3.0f);
        HysteresisThresholdShader.SetFloat("edgeThreshold", 0.5f);
        HysteresisThresholdShader.Dispatch(kernel, 1, H, 1);     
      
        // [NV] - Unabridged version of pipeline on branch depth_origin
        // Median blur
        kernel = MedianBlurShader.FindKernel("CSMain");
        MedianBlurShader.SetBuffer(kernel, "depth", depthBuffer);
        MedianBlurShader.Dispatch(kernel, 1, H, 1);
        // Apply morphological operations only to depth samples likely not on edges
        kernel = DilateEmptyShader.FindKernel("CSMain");
        DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
        DilateEmptyShader.SetBuffer(kernel, "mask", maskBuffer);
        DilateEmptyShader.SetInt("kernelSize", 9);
        DilateEmptyShader.Dispatch(kernel, 1, H, 1);

        for (int i = 0; i < 3; i++)
        {
            DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
            DilateEmptyShader.SetBuffer(kernel, "mask", maskBuffer);
            DilateEmptyShader.SetInt("kernelSize", 5);
            DilateEmptyShader.Dispatch(kernel, 1, H, 1);
        }

        // Median blur
        kernel = MedianBlurShader.FindKernel("CSMain");
        MedianBlurShader.SetBuffer(kernel, "depth", depthBuffer);
        MedianBlurShader.Dispatch(kernel, 1, H, 1);

        // Bilateral filtering
        kernel = BilateralFilterShader.FindKernel("CSMain");
        BilateralFilterShader.SetBuffer(kernel, "depth", depthBuffer);
        BilateralFilterShader.Dispatch(kernel, 1, H, 1);

        depthBuffer.GetData(depth);

        depthBuffer.Release();
        maskBuffer.Release();

        return depth;
    }
}
