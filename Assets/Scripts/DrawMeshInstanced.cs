using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using System.Text;
using static RosSharp.Urdf.Link.Visual.Material;
using System;
using JetBrains.Annotations;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
//using System;
using Unity.Sentis;
using static UnityEngine.Analytics.IAnalytic;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;

public class DrawMeshInstanced : MonoBehaviour
{
    public GameObject Arm;
    public GameObject Drive;
    public bool freeze_without_action;
    public int latency_frames;
    bool ready_to_freeze = false;

    public DepthCompletion depthCompletion;
    public float y_min;
    public float z_max;

    public float range;

    public Texture2D color_image;
    public Texture2D depth_image;

    public int imageScriptIndex;

    public Material material;

    public RawImageSubscriber depthSubscriber;  // ROS subscriber that holds the depth array
    public JPEGImageSubscriber colorSubscriber; // ROS subscriber holding the color image

    public ComputeShader compute;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;

    public Transform target;
    public Transform auxTarget; // In case someone changes the offset rotation

    private Mesh mesh;
    private Bounds bounds;

    public float noise_range;

    private uint total_population;
    private uint population;
    public uint downsample;
    public uint height;
    public uint width;
    public int CX;
    public int CY;
    public float FX;
    public float FY;
    public float facedAngle;
    public float t;
    public float pS;    // point scalar

    public float size_scale; //hack to current pointcloud viewing
    
    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar;
    private float[] depth_ar_saved;

    private MeshProperties[] globalProps;

    private float deltaTime = 0.0f;
    private float timer = 0.0f;

    public float delta_x;
    public float delta_y;
    public float delta_z;

    bool freeze_lock = false;


    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Vector4 pos;

        public static int Size()
        {
            return
                sizeof(float) * 4; // position;
        }
    }

    private void Update()
    {
        int kernel = compute.FindKernel("CSMain");
        //SetProperties enables point cloud to move when game object moves, but is laggier due to redrawing. Just comment it out for performance improvement;
        SetProperties();
        compute.SetMatrix("_GOPose", Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1)));
        compute.SetFloat("t", t);
        compute.SetFloat("y_min", y_min);
        compute.SetFloat("z_max", z_max);

        compute.SetFloat("dx", delta_x);
        compute.SetFloat("dy", delta_y);
        compute.SetFloat("dz", delta_z);

        UpdateTexture();
        // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
        // This is probably for the best, but we have to do some more calculation.  Divide population by numthreads.x (declared in compute shader).
        compute.Dispatch(kernel, Mathf.CeilToInt(population / 6f), 1, 1);

        //uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        //argsBuffer.GetData(args);
        //args[0] += 480 * 640;
        //argsBuffer.SetData(args);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);

        // get current fps
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timer += Time.unscaledDeltaTime;

        if (timer >= 1.0f) // Log FPS every second
        {
            float fps = 1.0f / deltaTime;
            Debug.Log("FPS: " + Mathf.Ceil(fps));
            timer = 0.0f; // Reset timer after logging
        }

        //if (GetComponent<SpotMovingDetection>().is_moving())
        //{
        //    continue_update();
        //}
    }

    private void SetProperties()                        
    {
        int kernel = compute.FindKernel("CSMain");        
        material.SetFloat("a", target.eulerAngles.y * 0.00872f * 2.0f);
        material.SetFloat("pS", pS);
        material.SetTexture("_colorMap",color_image);

        //meshPropertiesBuffer = new ComputeBuffer((int)population, MeshProperties.Size());
        //depthBuffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
        //globalProps = new MeshProperties[population];
        depthBuffer.SetData(depth_ar);
        meshPropertiesBuffer.SetData(globalProps);

        material.SetBuffer("_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Depth", depthBuffer);
    }

    private IEnumerator ToggleReadyToFreezeAfterDelay(float waitTime)
    {
        freeze_lock = true;
        yield return new WaitForSeconds(waitTime);
        ready_to_freeze = true;
        freeze_lock = false;
    }

    public void continue_update()
    {
        if (ready_to_freeze & !freeze_lock)
        {
            StartCoroutine(ToggleReadyToFreezeAfterDelay(1.0f / 30.0f * latency_frames));
            ready_to_freeze = false;
        }
    }

    private void UpdateTexture()
    {
        //Debug.Log(ready_to_freeze);
        if (freezeCloud || (ready_to_freeze && freeze_without_action))
        {
            return;
        }

        if (use_saved_meshes) {
            depth_ar = depth_ar_saved;
        }
        else
        {
            // Get the depth and color
            color_image = colorSubscriber.texture2D;
            depth_ar = depthSubscriber.getDepthArr();
        }

        depth_ar = depthCompletion.complete_depth(depth_ar, color_image);
    }

    private void OnDisable()
    {
        // Release gracefully.
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;

        if (depthBuffer != null)
        {
            depthBuffer.Release();
        }
        depthBuffer = null;
    }

    private void OnDestroy()
    {
        OnDisable();
    }

    // =============================================================================== //
    //                                     INIT                                        //
    // =============================================================================== //

    private void OnEnable()
    {
        StartCoroutine(ToggleReadyToFreezeAfterDelay(10.0f));

        pS = 1.0f;

        total_population = height * width;
        population = (uint)(total_population / downsample);

        Mesh mesh = CreateQuad(size_scale, size_scale);
        this.mesh = mesh;


        // Use saved meshes
        if (use_saved_meshes)
        {
            using (var stream = File.Open("Assets/PointClouds/mesh_array_" + imageScriptIndex, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    int length = reader.ReadInt32();
                    depth_ar = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        depth_ar[i] = reader.ReadSingle();
                    }
                }
            }

            // read the texture 2D
            byte[] bytes;
            using (var stream = File.Open("Assets/PointClouds/Color_" + imageScriptIndex + ".png", FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    bytes = reader.ReadBytes(int.MaxValue);
                }
            }
            color_image = new Texture2D(1, 1);
            color_image.LoadImage(bytes);

            // [2023-10-30][JHT] TODO Complete. What is the depth width/height? Is it really 'width' and 'height'?
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);

            depth_ar_saved = depth_ar;

            //(depth_ar, confidence_ar) = depthCompletion.complete_depth(depth_ar, color_image);
            //color_image = Resources.Load<Texture2D>("Assets/PointClouds/Color_" + imageScriptIndex + ".png");
        }
        else
        {
            depth_ar = new float[height * width];
            // [2023-10-30][JHT] TODO Complete. What is the depth width/height? Is it really 'width' and 'height'?
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
        }

        globalProps = new MeshProperties[population];

        bounds = new Bounds(Vector3.zero, Vector3.one * (range + 1));

        InitializeMaterials();
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        int kernel = compute.FindKernel("CSMain");

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = mesh.GetIndexCount(0);
        args[1] = population;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];

        meshPropertiesBuffer = new ComputeBuffer((int)population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(globalProps);

        depthBuffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
    }

    private void InitializeMaterials()
    {
        // Pass 1 / width and 1 / height to material shader
        // [2023-10-30][JHT] Why do we need to do this? That data is (almost) all in 'screenData' that gets passed in later.
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        material.SetFloat("a", target.eulerAngles.y * 0.00872f * 2.0f);
        material.SetFloat("pS", pS);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);
    }

    // Actually a cube, not a quad
    private Mesh CreateQuad(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            // lower left tri.
            0, 2, 1,
            // lower right tri
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    // =============================================================================== //
    //                                     PUBLIC                                        //
    // =============================================================================== //

    public void toggleFreezeCloud()
    {
        float[] temp_depth;
        Texture2D temp_texture;

        freezeCloud = !freezeCloud;

        //if turning on freeze, deep copy arrays
        if (freezeCloud)
        {
            temp_depth = new float[depth_ar.Length];
            Array.Copy(depth_ar, temp_depth, depth_ar.Length);
            depth_ar = temp_depth;

            temp_texture = new Texture2D(color_image.width, color_image.height);
            temp_texture.SetPixels(color_image.GetPixels());
            temp_texture.Apply();
            color_image = temp_texture;
        }
    }
}