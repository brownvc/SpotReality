using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthPipeline2 : MonoBehaviour
{
    public ComputeShader MedianBlurShader;
    public ComputeShader BilateralFilterShader;
    public ComputeShader DilateEmptyShader;
    public ComputeShader InvertShader;
    public ComputeShader TextureGenerateShader;

    private RenderTexture depthTexture;
    private ComputeBuffer depthBuffer;

    private int H = 480;
    private int W = 640;

    //public float[] DepthPipeline(RenderTexture depthTexture)
    public float[] DepthPipeline(float[] depth)
    {
        int kernel;

        depthTexture = new RenderTexture(W, H, 0);
        depthTexture.enableRandomWrite = true;
        depthTexture.Create();

        depthBuffer = new ComputeBuffer(depth.Length, sizeof(float));
        depthBuffer.SetData(depth);

        //// Generate RenderTexture
        //kernel = TextureGenerateShader.FindKernel("CSMain");
        //TextureGenerateShader.SetBuffer(kernel, "depth", depthBuffer);
        //TextureGenerateShader.SetTexture(kernel, "data", depthTexture);
        //TextureGenerateShader.Dispatch(kernel, 1, H, 1);

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
        for (int i = 0; i < 6; i++)
        {
            DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
            DilateEmptyShader.SetInt("kernelSize", 5);
            DilateEmptyShader.Dispatch(kernel, 1, H, 1);
        }

        // Median blur
        kernel = MedianBlurShader.FindKernel("CSMain");
        MedianBlurShader.SetBuffer(kernel, "depth", depthBuffer);
        MedianBlurShader.Dispatch(kernel, 1, H, 1);

        //kernel = BilateralFilterShader.FindKernel("CSMain");
        //BilateralFilterShader.SetBuffer(kernel, "depth", depthBuffer);
        //BilateralFilterShader.Dispatch(kernel, 1, H, 1);

        // Invert(and offset)
        kernel = InvertShader.FindKernel("CSMain");
        InvertShader.SetBuffer(kernel, "depth", depthBuffer);
        //InvertShader.SetTexture(kernel, "data", depthTexture);
        InvertShader.Dispatch(kernel, 1, H, 1);

        // Get final data
        float[] res = new float[H * W];
        depthBuffer.GetData(res);

        return res;
    }
}
