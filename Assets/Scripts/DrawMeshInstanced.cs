using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using System.Text;
using static RosSharp.Urdf.Link.Visual.Material;
//using System;

public class DrawMeshInstanced : MonoBehaviour
{
    public float range;

    public Texture2D color_image;

    public int imageScriptIndex;

    public Material material;

    public ComputeShader compute;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;

    public Transform target;

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

    public float size_scale; //hack to current pointcloud viewing
    
    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private float[] depth_ar;

    private MeshProperties[] globalProps;

    //private MeshProperties[] generalUseProps;
    

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Vector4 pos;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 + // matrix;
                sizeof(float) * 4; // color;
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

        //Mesh mesh = CreateQuad(size_scale, size_scale, size_scale);
        //Mesh mesh = CreateQuad(size_scale, size_scale);
        Mesh mesh = CreateQuad(size_scale, size_scale);
        //Mesh mesh = CreateQuad(0.01f,0.01f);
        this.mesh = mesh;

        //generalUseProps = new MeshProperties[population];

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

            //color_image = Resources.Load<Texture2D>("Assets/PointClouds/Color_" + imageScriptIndex + ".png");
        }
        else if (false)// Read in new scene data from ROS? 
        {
            depth_ar = new float[height * width];
            int counter = 0;

            StreamReader inp_stm = new StreamReader("./Assets/PointClouds/color2_depth_unity.txt");

            GameObject rosConnector = GameObject.Find("RosConnector");
            //ImageSubscriber imageScript = rosConnector.GetComponents<ImageSubscriber>()[2];

            while (!inp_stm.EndOfStream)
            {
                string inp_ln = inp_stm.ReadLine();
                string[] split_arr = inp_ln.Split(',');
                foreach (var spli in split_arr)
                {
                    depth_ar[counter] = float.Parse(spli);
                    counter += 1;
                    //Debug.Log(spli);
                }
                // Do Something with the input. 
            }
        }




        //inp_stm.Close();

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        // bounds = new Bounds(transform.position, Vector3.one * (range + 1));
        bounds = new Bounds(Vector3.zero, Vector3.one * (range + 1));

        material.SetFloat("width",1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        material.SetFloat("a",facedAngle);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics",intr);
        Vector4 screenData = new Vector4((float)width, (float)height, 1/(float)width, FY);
        compute.SetVector("screenData", screenData);
        compute.SetFloat("samplingSize",downsample);

        InitializeBuffers();

    }


    private MeshProperties[] GetProperties()
    {
        // Initialize buffer with the given population.
        //MeshProperties[] properties = new MeshProperties[population];

        //return properties;
        MeshProperties[] properties = new MeshProperties[population];

        if (width == 0 || height == 0 || depth_ar == null || depth_ar.Length == 0)
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

            
            //x = i % (width);
            //y = (uint)Mathf.Floor(i / width);
            
            /*
            depth_idx = (width * (height - y - 1)) + (width - x - 1);

            if (depth_idx >= depth_ar.Length)
            {
                continue;
            }
            */
            /*
            Vector3 position = Vector3.one;


            position = new Vector4(10000, 1000, 1000, 1);
            */
            /*
            if (depth_ar[depth_idx] == 0)
            {
                

                props.pos = new Vector4(0,0,0,1);
                //properties[pop_i].pos = new Vector4(0, 0, 0, 1);

                //props.mat = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), Vector3.one * 0);
                //props.color = Color.Lerp(Color.red, Color.blue, Random.value);

                props.color = new Vector4(0, 0, 0, 1);
                //properties[pop_i].color = new Vector4(0, 0, 0, 1);

                //properties[pop_i] = props;
                continue;

            }
            else
            {
                //position = new Vector3(10000, 1000, 1000);
                position = pixel_to_vision_frame(x, y, depth_ar[depth_idx]); //TODO: Get 4x4 matrix instead
            }
            */

            //Quaternion rotation = Quaternion.Euler(0, 0, 0);
            //Vector3 scale = Vector3.one * 1;
            //Vector3 some_noise = new Vector3(Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range));
            //props.mat = Matrix4x4.TRS(position + some_noise, rotation, scale);


            //props.color = Color.Lerp(Color.red, Color.blue, Random.value);

            //Vector3 some_noise = new Vector3(Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range), Random.Range(-noise_range, noise_range));
            //Vector3 intermediatePos = position + some_noise;

            props.pos = new Vector4(0, 0, 0, 1); ;
            props.color.x = (float)i;
            //props.color.y = (float)y;
            props.color.z = 1;//(float)depth_ar[depth_idx];

            //props.color = color_image.GetPixel((int)(width-x)-1, (int)y);
            //props.color[3] = 1.0f;
            
            //properties[pop_i].pos = position;

            //properties[pop_i].color = color_image.GetPixel((int)(width - x) - 1, (int)y);
            //properties[pop_i].color[3] = 1.0f;
            //props.color = new Color(0, 0, 0, 0);

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

        depthBuffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
        depthBuffer.SetData(depth_ar);

        SetProperties();
        SetGOPosition();
    }

    private void SetGOPosition()
    {
        compute.SetMatrix("_GOPose", Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1)));
        // compute.SetMatrix("_GOPose", Matrix4x4.TRS(Vector3.zero, transform.rotation, new Vector3(1, 1, 1)));
    }

    private void SetProperties()
    {
        int kernel = compute.FindKernel("CSMain");

        //if (globalProps == null) && use_saved_meshes)
        {
            globalProps = GetProperties();
        }

        meshPropertiesBuffer.SetData(globalProps);
        depthBuffer.SetData(depth_ar);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        material.SetTexture("_colorMap",color_image);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Depth", depthBuffer);
    }

    private void UpdateTexture()
    {           
        if(use_saved_meshes) {
            return;
        }
        GameObject rosConnector = GameObject.Find("RosConnector");
        color_image = rosConnector.GetComponents<ImageSubscriber>()[imageScriptIndex].texture2D;
        //color_image = rosConnector.GetComponents<ImageSubscriber>()[1].texture2D;
        // = rosConnector.GetComponents<ImageSubscriber>()[0].depth_data;

        depth_ar = rosConnector.GetComponents<RawImageSubscriber>()[imageScriptIndex].image_data;

        // save the point cloud if desired
        MoveSpot move = rosConnector.GetComponent<MoveSpot>();
        if (move.save)
        {
            using (FileStream file = File.Create("Assets/PointClouds/mesh_array_" + imageScriptIndex))
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    writer.Write((int)depth_ar.Length);
                    foreach (float value in depth_ar)
                    {
                        writer.Write(value);
                    }
                }
            }

            byte[] bytes = color_image.EncodeToPNG();
            //var dirPath = Application.dataPath + "/../SaveImages/";
            //if (!Directory.Exists(dirPath))
            //{
            //    Directory.CreateDirectory(dirPath);
            //}
            File.WriteAllBytes("Assets/PointClouds/Color_" + imageScriptIndex + ".png", bytes);


            //using (var stream = File.Open("mesh_array_" + imageScriptIndex, FileMode.Open))
            //{
            //    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            //    {
            //        int length = reader.ReadInt32();
            //        depth_ar = new float[length];
            //        for (int i = 0; i < length; i++)
            //        {
            //            depth_ar[i] = reader.ReadSingle();
            //        }
            //    }
            //}

            //move.save = false;
        }

    }

    private void Update()
    {
        int kernel = compute.FindKernel("CSMain");
        //SetProperties enables point cloud to move when game object moves, but is laggier due to redrawing. Just comment it out for performance improvement;
        //transform.LookAt(target);
        SetProperties();
        SetGOPosition();

        //update the color image
        UpdateTexture();

        // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
        // This is probably for the best, but we have to do some more calculation.  Divide population by numthreads.x (declared in compute shader).
        compute.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private Vector4 pixel_to_vision_frame(uint i, uint j, float depth)
    {
        //int CX = 320;
        //int CY = 240;
        
        //float FX = (float)552.029101;
        //float FY = (float)552.029101;

        float x = (j - CX) * depth / FX;
        float y = (i - CY) * depth / FY;

        Vector4 ret = new Vector4(x, y, depth,1f);
        return (ret);

    }

    //private Mesh CreateQuad(float width = 1f, float height = 1f, float depth = 1f)
    //{
    //    // Create a quad mesh.
    //    var mesh = new Mesh();

    //    float w = width * .5f;
    //    float h = height * .5f;
    //    float d = depth * .5f;

    //    var vertices = new Vector3[8] {
    //        new Vector3(-w, -h, -d),
    //        new Vector3(w, -h, -d),
    //        new Vector3(w, h, -d),
    //        new Vector3(-w, h, -d),
    //        new Vector3(-w, -h, d),
    //        new Vector3(w, -h, d),
    //        new Vector3(w, h, d),
    //        new Vector3(-w, h, d)
    //    };

    //    var tris = new int[3 * 2 * 6] {
    //        0, 3, 1,
    //        3, 2, 1,

    //        0,4,5,
    //        0,5,1,

    //        1,5,2,
    //        2,5,6,

    //        7,3,6,
    //        3,6,2,

    //        0,4,3,
    //        4,7,3,

    //        4,7,5,
    //        7,5,6
    //    };

    //    var normals = new Vector3[8] {
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,

    //    };

    //    var uv = new Vector2[8] {
    //        new Vector2(0, 0),
    //        new Vector2(1, 0),
    //        new Vector2(1, 1),
    //        new Vector2(0, 1),
    //        new Vector2(0, 0),
    //        new Vector2(1, 0),
    //        new Vector2(1, 1),
    //        new Vector2(0, 1),
    //    };

    //    mesh.vertices = vertices;
    //    mesh.triangles = tris;
    //    mesh.normals = normals;
    //    mesh.uv = uv;

    //    return mesh;
    //}

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

    private Mesh CreateTri(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[3] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0)
        };

        var tris = new int[3] {
            // lower left tri.
            0, 2, 1
        };

        var normals = new Vector3[3] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[3] {
            new Vector2(0, 0),
            new Vector2(1, 0),
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