using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthPipeline : MonoBehaviour
{
    public ComputeShader DilateEmptyShader;
    public ComputeShader InvertShader;
    public ComputeShader ClosureShader;

    public ComputeShader MedianBlurShader;
    public ComputeShader BilateralFilterShader;
    public ComputeShader TextureGenerateShader;

    private RenderTexture depthTexture;
    private ComputeBuffer depthBuffer;

    private int H = 480;
    private int W = 640;

    public float[] PreprocessDepth(float[] depth)
    {
        int kernel;

        depthBuffer = new ComputeBuffer(depth.Length, sizeof(float));
        depthBuffer.SetData(depth);

        // Invert
        // kernel = InvertShader.FindKernel("CSMain");
        // InvertShader.SetBuffer(kernel, "depth", depthBuffer);
        // InvertShader.Dispatch(kernel, 1, H, 1);

        //// Multiscale Dilate (just a single dilate for now)
        //kernel = DilateEmptyShader.FindKernel("CSMain");
        //DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
        //DilateEmptyShader.SetInt("kernelSize", 9);
        //DilateEmptyShader.Dispatch(kernel, 1, H, 1);

        // Small hole closure
        // expand
        // kernel = ClosureShader.FindKernel("CSMain");
        // ClosureShader.SetBuffer(kernel, "depth", depthBuffer);
        // ClosureShader.SetInt("kernelSize", 5);
        // ClosureShader.SetBool("isDilation", true);
        // ClosureShader.Dispatch(kernel, 1, H, 1);
        // // contract
        // ClosureShader.SetInt("kernelSize", 5);
        // ClosureShader.SetBool("isDilation", false);
        // ClosureShader.Dispatch(kernel, 1, H , 1);

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

        // Invert(and offset)
        // kernel = InvertShader.FindKernel("CSMain");
        // InvertShader.SetBuffer(kernel, "depth", depthBuffer);
        // //InvertShader.SetTexture(kernel, "data", depthTexture);
        // InvertShader.Dispatch(kernel, 1, H, 1);

        float[] res = new float[depth.Length];
        depthBuffer.GetData(res);

        depthBuffer.Release();

        return res;
    }
}
