using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RosSharp.RosBridgeClient;

public class DrawMeshInstanced : MonoBehaviour
{
    public float range;

    public float[] color_image;

    public int imageScriptIndex;

    public Material material;

    public ComputeShader compute;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

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

    public float size_scale; //hack to current pointcloud viewing

    private float[] depth_ar;

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }

    private void Setup()
    {
        //size_scale = 0.002f;
        //width = 640;
        //height = 480;
        //width = 424;
        //height = 240;
        total_population = height * width;
        population = (uint)(total_population / downsample);
        Mesh mesh = CreateQuad(size_scale, size_scale, size_scale);
        //Mesh mesh = CreateQuad(0.01f,0.01f);
        this.mesh = mesh;

        //depth_ar = new float[height * width];
        //int counter = 0;

        //StreamReader inp_stm = new StreamReader("./Assets/PointClouds/color2_depth_unity.txt");

        //GameObject rosConnector = GameObject.Find("RosConnector");
        ////ImageSubscriber imageScript = rosConnector.GetComponents<ImageSubscriber>()[2];

        //while (!inp_stm.EndOfStream)
        //{
        //    string inp_ln = inp_stm.ReadLine();
        //    string[] split_arr = inp_ln.Split(',');
        //    foreach (var spli in split_arr)
        //    {
        //        depth_ar[counter] = float.Parse(spli);
        //        counter += 1;
        //        //Debug.Log(spli);
        //    }
        //    // Do Something with the input. 
        //}

        //inp_stm.Close();

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * (range + 1));

        InitializeBuffers();

    }

    private MeshProperties[] GetProperties()
    {
        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];

        if(width == 0 || height == 0 || depth_ar == null || depth_ar.Length == 0 || color_image.Length == 0)
        {
            return properties;
        }

        uint x;
        uint y;
        uint depth_idx;
        uint i;
        for (uint pop_i = 0; pop_i < population; pop_i++)
        {
            i = pop_i * downsample;
            MeshProperties props = new MeshProperties();
            x = i % (width);
            y = (uint)Mathf.Floor(i / width);
            depth_idx = (width * (height - y - 1)) + x;

            if(depth_idx >= depth_ar.Length)
            {
                continue;
            }

            Vector3 position;

            if (depth_ar[depth_idx] == 0)
            {
                position = new Vector3(10000, 1000, 1000);

                props.mat = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), Vector3.one * 0);
                //props.color = Color.Lerp(Color.red, Color.blue, Random.value);

                props.color = new Vector4(0, 0, 0, 0);

                properties[pop_i] = props;
                continue;

            }
            else
            {
                position = pixel_to_vision_frame(x, y, depth_ar[depth_idx]); //TODO: Get 4x4 matrix instead
            }

            Quaternion rotation = Quaternion.Euler(0, 0, 0);
            Vector3 scale = Vector3.one * 1;
            Vector3 some_noise = new Vector3(Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range));
            props.mat = Matrix4x4.TRS(position + some_noise, rotation, scale);
            //props.color = Color.Lerp(Color.red, Color.blue, Random.value);

            //props.color = color_image.GetPixel((int)(width-x)-1, (int)y);
            int new_x = (int)(width - x) - 1;

            int offset = (int)((height - y) - 1) * (int)width*3 + (int)new_x * 3;
            //Debug.Log(color_image.Length);
            props.color = new Color(color_image[offset] / 255.0f, color_image[offset + 1] / 255.0f, color_image[offset + 2] / 255.0f);
            //Debug.Log(props.color);

            properties[pop_i] = props;
        }

        return (properties);
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
        meshPropertiesBuffer.SetData(GetProperties());

        SetProperties();
        SetGOPosition();
    }

    private void SetGOPosition()
    {
        compute.SetMatrix("_GOPose", Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1)));
    }

    private void SetProperties()
    {
        int kernel = compute.FindKernel("CSMain");

        meshPropertiesBuffer.SetData(GetProperties());
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
    }

    private void UpdateTexture()
    {
        GameObject rosConnector = GameObject.Find("RosConnector");
        //color_image = rosConnector.GetComponents<ImageSubscriber>()[imageScriptIndex].texture2D;
        color_image = rosConnector.GetComponents<Float32ArraySubscriber>()[4].data;
        depth_ar = rosConnector.GetComponents<Float32ArraySubscriber>()[imageScriptIndex].data;

    }

    private void Update()
    {
        int kernel = compute.FindKernel("CSMain");
        //SetProperties enables point cloud to move when game object moves, but is laggier due to redrawing. Just comment it out for performance improvement;
        SetProperties();
        SetGOPosition();

        //update the color image
        UpdateTexture();

        // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
        // This is probably for the best, but we have to do some more calculation.  Divide population by numthreads.x (declared in compute shader).
        compute.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private Vector3 pixel_to_vision_frame(uint i, uint j, float depth)
    {
        //int CX = 320;
        //int CY = 240;

        //float FX = (float)552.029101;
        //float FY = (float)552.029101;

        float x = (j - CX) * depth / FX;
        float y = (i - CY) * depth / FY;

        Vector3 ret = new Vector3(x, y, depth);
        return (ret);

    }

    private Mesh CreateQuad(float width = 1f, float height = 1f, float depth = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        float d = depth * .5f;

        var vertices = new Vector3[8] {
            new Vector3(-w, -h, -d),
            new Vector3(w, -h, -d),
            new Vector3(w, h, -d),
            new Vector3(-w, h, -d),
            new Vector3(-w, -h, d),
            new Vector3(w, -h, d),
            new Vector3(w, h, d),
            new Vector3(-w, h, d)
        };

        var tris = new int[3 * 2 * 6] {
            0, 3, 1,
            3, 2, 1,

            0,4,5,
            0,5,1,

            1,5,2,
            2,5,6,

            7,3,6,
            3,6,2,

            0,4,3,
            4,7,3,

            4,7,5,
            7,5,6
        };

        var normals = new Vector3[8] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,

        };

        var uv = new Vector2[8] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private void Start()
    {
        Setup();
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
    }

}