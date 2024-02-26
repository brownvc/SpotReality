using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthPipeline1 : MonoBehaviour
{
    public ComputeShader DilateEmptyShader;
    public ComputeShader InvertShader;
    public ComputeShader ClosureShader;

    private RenderTexture depthTexture;
    private ComputeBuffer depthBuffer;

    private int H = 480;
    private int W = 640;

    public float[] DepthPipeline(float[] depth)
    {
        int kernel;

        depthTexture = new RenderTexture(W, H, 0);
        depthTexture.enableRandomWrite = true;
        depthTexture.Create();

        depthBuffer = new ComputeBuffer(depth.Length, sizeof(float));
        depthBuffer.SetData(depth);

        // Invert
        kernel = InvertShader.FindKernel("CSMain");
        InvertShader.SetBuffer(kernel, "depth", depthBuffer);
        InvertShader.Dispatch(kernel, 1, H, 1);

        // Multiscale Dilate (just a single dilate for now)
        kernel = DilateEmptyShader.FindKernel("CSMain");
        DilateEmptyShader.SetBuffer(kernel, "depth", depthBuffer);
        DilateEmptyShader.SetInt("kernelSize", 9);
        DilateEmptyShader.Dispatch(kernel, 1, H, 1);

        // Small hole closure
        // expand
        kernel = ClosureShader.FindKernel("CSMain");
        ClosureShader.SetBuffer(kernel, "depth", depthBuffer);
        ClosureShader.SetInt("kernelSize", 5);
        ClosureShader.SetBool("isDilation", true);
        ClosureShader.Dispatch(kernel, 1, H, 1);
        // contract
        ClosureShader.SetInt("kernelSize", 5);
        ClosureShader.SetBool("isDilation", false);
        ClosureShader.Dispatch(kernel, 1, H , 1);
        
        float[] res = new float[H * W];
        depthBuffer.GetData(res);

        return res;
    }
}
