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
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
//using System;

public class DrawMeshInstanced : MonoBehaviour
{
    public int voxel_x;
    public int voxel_y;
    public int voxel_z;
    public float voxel_size;
    public float threshold_distance;
    public int truncation_scale;

    private int num_voxel;

    public float range;

    public Texture2D color_image;
    public Texture2D depth_image;

    public int imageScriptIndex;

    public Material material;

    public RawImageSubscriber depthSubscriber;  // ROS subscriber that holds the depth array
    public JPEGImageSubscriber colorSubscriber; // ROS subscriber holding the color image
    public bool savePointCloud;                 // allow user to save point cloud

    public ComputeShader compute;
    public ComputeShader preprocess_shader;
    public ComputeShader tsdf_shader;
    public ComputeShader postprocess_shader;

    private ComputeBuffer meshPropertiesBufferX;
    private ComputeBuffer meshPropertiesBufferY;
    private ComputeBuffer meshPropertiesBufferZ;
    private ComputeBuffer meshPropertiesBufferW;
    private ComputeBuffer meshPropertiesBuffer;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;
    private ComputeBuffer tsdfVolume;
    private ComputeBuffer resBuffer;
    private ComputeBuffer coorBuffer;

    private ComputeBuffer tempx;
    private ComputeBuffer tempy;
    private ComputeBuffer tempz;
    private ComputeBuffer tempw;

    private float[] tsdf_res;

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
    //public uint counter;
    //private uint numUpdates;

    public float size_scale; //hack to current pointcloud viewing

    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar;

    private MeshProperties[] globalProps;

    //private MeshProperties[] generalUseProps;

    private MeshProperties[] tempProps;

    private bool executed = false;


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

    private void Setup()
    {
        //pS = 1.0f;

        //size_scale = 0.002f;
        //width = 640;
        //height = 480;
        //width = 424;
        //height = 240;
        //counter = 0;
        //numUpdates = 0;
        num_voxel = voxel_x * voxel_y * voxel_z;
        total_population = height * width;
        population = (uint)(total_population / downsample);

        //Mesh mesh = CreateQuad(size_scale, size_scale);
        Mesh mesh = CreateCube(size_scale, size_scale, size_scale);
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

            // preprocess depth_ar


            // [2023-10-30][JHT] TODO Complete. What is the depth width/height? Is it really 'width' and 'height'?
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);

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
            // [2023-10-30][JHT] TODO Complete. What is the depth width/height? Is it really 'width' and 'height'?
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);
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
        else
        {
            depth_ar = new float[height * width];
            // [2023-10-30][JHT] TODO Complete. What is the depth width/height? Is it really 'width' and 'height'?
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);
        }

        globalProps = GetProperties();


        //inp_stm.Close();

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        // bounds = new Bounds(transform.position, Vector3.one * (range + 1));
        bounds = new Bounds(Vector3.zero, Vector3.one * (range + 1)); // center, size

        // Pass 1 / width and 1 / height to material shader
        // [2023-10-30][JHT] Why do we need to do this? That data is (almost) all in 'screenData' that gets passed in later.
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        //material.SetFloat("a",target.rotation.y * 0);
        //Debug.Log(auxTarget.eulerAngles);
        material.SetFloat("a", get_target_rota());
        material.SetFloat("pS", pS);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);

        InitializeBuffers();

    }

    private float get_target_rota()
    {
        //Debug.Log(convert_angle(target.eulerAngles.y).ToString() + "     " + convert_angle(auxTarget.eulerAngles.y).ToString());
        if (auxTarget == null || true) { return convert_angle(target.eulerAngles.y) * 2; }
        else
        {
            return convert_angle(target.eulerAngles.y) + convert_angle(auxTarget.eulerAngles.y);
        }
    }

    private float convert_angle(float a) // Unity is giving me the sin of an angle when I just want the angle
    {
        //a = (a + 180) % 360 - 180;
        return a * (float)0.00872;
    }

    private MeshProperties[] GetProperties()
    {
        // Initialize buffer with the given population.
        //MeshProperties[] properties = new MeshProperties[population];

        //return properties;
        MeshProperties[] properties = new MeshProperties[population];

        if (width == 0 || height == 0 || depth_ar == null || depth_ar.Length == 0 || true)
        {
            Debug.Log("Fail to GetProperties");
            return properties;
        }


        // uint x;
        // uint y;
        // uint depth_idx;
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

            props.pos = new Vector4(0, 0, 0, 1);
            //props.color.x = (float)i;
            //props.color.y = (float)y;
            //props.color.z = 1;//(float)depth_ar[depth_idx];

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

    private void ChangeProperties()
    {
        //tempProps = globalProps.ToList();

        preprocess_cloud(); // remove outlier
    }

    private void preprocess_cloud()
    {
        //int kernel1 = preprocess_shader.FindKernel("CSMain1");
        ////meshPropertiesBuffer.SetData(globalProps);
        //preprocess_shader.SetBuffer(kernel1, "_Properties", meshPropertiesBuffer);
        //preprocess_shader.Dispatch(kernel1, Mathf.CeilToInt(population / 64f), 1, 1);

        //meshPropertiesBuffer.GetData(globalProps);
        //List<MeshProperties> res = globalProps.ToList().Where((n, index) => n.pos.x + n.pos.y + n.pos.z > 0.1 || n.pos.x + n.pos.y + n.pos.z < -0.1).ToList();
        //globalProps = res.ToArray();

        //int kernel2 = preprocess_shader.FindKernel("CSMain2");
        ////meshPropertiesBuffer.SetData(globalProps);
        //preprocess_shader.SetBuffer(kernel2, "_Properties", meshPropertiesBuffer);
        //preprocess_shader.Dispatch(kernel2, Mathf.CeilToInt(population / 64f), 1, 1);
    }

    //private static double Distance(MeshProperties a, MeshProperties b)
    //{
    //    return Math.Sqrt(Math.Pow(a.pos.x - b.pos.x, 2) + Math.Pow(a.pos.y - b.pos.y, 2) + Math.Pow(a.pos.z - b.pos.z, 2));
    //}

    //private void RemoveStatisticalOutliers(int k, double stdRatio)
    //{
    //    var distances = new List<double>(tempProps.Count);
    //    var removeIndices = new HashSet<int>();

    //    // Calculate mean distance to k nearest neighbors for each point
    //    for (int i = 0; i < tempProps.Count; i++)
    //    {
    //        var distancesToNeighbors = tempProps.Select((p, idx) => new { Index = idx, Distance = Distance(tempProps[i], p) })
    //                                          .OrderBy(p => p.Distance)
    //                                          .Skip(1) // Skip the first one because it's the point itself
    //                                          .Take(k)
    //                                          .Select(p => p.Distance)
    //                                          .ToList();

    //        double mean = distancesToNeighbors.Average();
    //        double stdDev = Math.Sqrt(distancesToNeighbors.Sum(d => Math.Pow(d - mean, 2)) / k);
    //        distances.Add(stdDev);
    //    }

    //    double globalMean = distances.Average();
    //    double threshold = globalMean * stdRatio;

    //    // Identify outliers
    //    for (int i = 0; i < distances.Count; i++)
    //    {
    //        if (distances[i] > threshold)
    //        {
    //            removeIndices.Add(i);
    //        }
    //    }

    //    // Remove outliers
    //    tempProps = tempProps.Where((p, idx) => !removeIndices.Contains(idx)).ToList();
    //}

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
        //argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];

        meshPropertiesBuffer = new ComputeBuffer((int)population, MeshProperties.Size());
        meshPropertiesBufferX = new ComputeBuffer((int)population, sizeof(float));
        meshPropertiesBufferY = new ComputeBuffer((int)population, sizeof(float));
        meshPropertiesBufferZ = new ComputeBuffer((int)population, sizeof(float));
        meshPropertiesBufferW = new ComputeBuffer((int)population, sizeof(float));
        //meshPropertiesBuffer.SetData(GetProperties());

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

        //if (globalProps == null)// && use_saved_meshes)
        //{
        //    globalProps = GetProperties();
        //}


        //meshPropertiesBuffer.SetData(globalProps);
        //Debug.Log(globalProps[0].pos);
        material.SetFloat("a", get_target_rota());
        material.SetFloat("pS", pS);
        depthBuffer.SetData(depth_ar);
        //material.SetBuffer("_Properties", meshPropertiesBuffer);
        material.SetTexture("_colorMap", color_image);
        //compute.SetBuffer(kernel, "_PropertiesX", meshPropertiesBufferX);
        //compute.SetBuffer(kernel, "_PropertiesY", meshPropertiesBufferY);
        //compute.SetBuffer(kernel, "_PropertiesZ", meshPropertiesBufferZ);
        //compute.SetBuffer(kernel, "_PropertiesW", meshPropertiesBufferW);
        compute.SetBuffer(kernel, "pp", meshPropertiesBuffer);

        compute.SetBuffer(kernel, "_Depth", depthBuffer);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);
    }

    private void UpdateTexture()
    {
        if (use_saved_meshes || freezeCloud)
        {
            //Debug.Log("use_saved_meshes");
            //Debug.Log("UpdateTexture, Time: " + UnityEngine.Time.realtimeSinceStartup);
            return;
        }

        // Get the depth and color
        color_image = colorSubscriber.texture2D;
        if (t == 0)
        {
            depth_ar = new float[width * height];
        }
        else
        {
            depth_ar = depthSubscriber.getDepthArr();
        }

        // save the point cloud if desired
        if (savePointCloud)
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
            File.WriteAllBytes("Assets/PointClouds/Color_" + imageScriptIndex + ".png", bytes);
        }

    }

    private void Update()
    {
        if (!executed)
        {
            //Debug.Log("UPDATE");
            //int kernel = compute.FindKernel("CSMain");
            //SetProperties enables point cloud to move when game object moves, but is laggier due to redrawing. Just comment it out for performance improvement;
            //transform.LookAt(target);
            //SetProperties();
            //SetGOPosition();
            //compute.SetFloat("t", t);
            //compute.SetBuffer(kernel, "pp", meshPropertiesBuffer);

            //compute.SetBuffer(kernel, "_Depth", depthBuffer);

            //update the color image
            //counter += 1;
            //UpdateTexture();
            //Debug.Log("UPDATE");
            //DateTime localTime = DateTime.Now;
            //float deltaTime = Time.deltaTime;
            //long microseconds = localTime.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            //Debug.Log("updates per second: " + (counter/Time.realtimeSinceStartup).ToString() + " updates: " + counter.ToString() + " deltaTime: " + Time.realtimeSinceStartup.ToString());

            // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
            // This is probably for the best, but we have to do some more calculation.  Divide population by numthreads.x (declared in compute shader).
            //compute.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);

            ////ChangeProperties(); // update point clouds data
            //meshPropertiesBuffer.GetData(globalProps);

            //using (var writer = new StreamWriter("./pointcloud.csv"))
            //{
            //    // Assuming points is a 2D array where each row is [x, y, z]
            //    for (int i = 0; i < globalProps.Length; i++)
            //    {
            //        // Format the point data as a comma-separated string
            //        string line = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", globalProps[i].pos.x, globalProps[i].pos.y, globalProps[i].pos.z);
            //        writer.WriteLine(line);
            //    }
            //}
            //material.SetBuffer("_Properties", meshPropertiesBuffer);

            //uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
            //// Arguments for drawing mesh.
            //// 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
            //_args[0] = mesh.GetIndexCount(0);
            //_args[1] = (uint)globalProps.Length;
            //_args[2] = mesh.GetIndexStart(0);
            //_args[3] = mesh.GetBaseVertex(0);
            //argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            //argsBuffer.SetData(_args);

            //Debug.Log(_args[1]);




            tsdfVolume = new ComputeBuffer(num_voxel, sizeof(float));
            float[] initialValues = new float[num_voxel];
            for (int i = 0; i < initialValues.Length; i++)
            {
                initialValues[i] = -1.0f;
            }
            tsdfVolume.SetData(initialValues);

            resBuffer = new ComputeBuffer(num_voxel, MeshProperties.Size());
            coorBuffer = new ComputeBuffer(num_voxel, MeshProperties.Size());

            //tempx = new ComputeBuffer((int)population, sizeof(float));
            //tempy = new ComputeBuffer((int)population, sizeof(float));
            //tempz = new ComputeBuffer((int)population, sizeof(float));

            int tsdf_kernel = tsdf_shader.FindKernel("CSMain");
            //tsdf_shader.SetBuffer(tsdf_kernel, "pointCloud", meshPropertiesBuffer);
            tsdf_shader.SetBuffer(tsdf_kernel, "tsdfVolume", tsdfVolume);
            tsdf_shader.SetBuffer(tsdf_kernel, "uv_coord", coorBuffer);
            tsdf_shader.SetBuffer(tsdf_kernel, "_Depth", depthBuffer);
            //tsdf_shader.SetBuffer(tsdf_kernel, "res", resBuffer);
            tsdf_shader.SetVector("intrinsics", new Vector4((float)CX, (float)CY, FX, FY));

            Matrix4x4 gPose = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));

            tsdf_shader.SetMatrix("_GOPose", gPose);
            tsdf_shader.SetMatrix("_GOPoseInverse", Matrix4x4.Inverse(gPose));
            tsdf_shader.SetVector("screenData", new Vector4((float)width, (float)height, 1 / (float)width, FY));

            tsdf_shader.SetInt("dimy", voxel_y);
            tsdf_shader.SetInt("dimz", voxel_z);
            tsdf_shader.SetFloat("voxel_size", voxel_size);
            tsdf_shader.SetInt("truncation_scale", truncation_scale);

            //tsdf_shader.SetBuffer(tsdf_kernel, "test_x", tempx);
            //tsdf_shader.SetBuffer(tsdf_kernel, "test_y", tempy);
            //tsdf_shader.SetBuffer(tsdf_kernel, "test_z", tempz);

            tsdf_shader.Dispatch(tsdf_kernel, 1, voxel_y, 1);

            float[] tt = new float[num_voxel];
            tsdfVolume.GetData(tt);

            using (BinaryWriter writer = new BinaryWriter(File.Open("./temp.txt", FileMode.Create)))
            {
                // Optionally, write dimensions to the file if needed.
                writer.Write(320);
                writer.Write(160);
                writer.Write(240);

                // Write each float to the binary file.
                foreach (float value in tt)
                {
                    writer.Write(value);
                }
            }

                //float[] ttx = new float[population];
                //float[] tty = new float[population];
                //float[] ttz = new float[population];
                //float[] ttw = new float[population];

                //meshPropertiesBufferX.GetData(ttx);
                //meshPropertiesBufferY.GetData(tty);
                //meshPropertiesBufferZ.GetData(ttz);
                //meshPropertiesBufferW.GetData(ttw);

                //List<float> ttxx = ttx.ToList();
                //ttxx.RemoveAll(item => item > 100.0f || item < 0.0f);

                //List<float> ttxy = tty.ToList();
                //ttxy.RemoveAll(item => item > 100.0f || item < 0.0f);

                //List<float> ttxz = ttz.ToList();
                //ttxz.RemoveAll(item => item > 100.0f || item < 0.0f);

                //List<float> ttxw = ttw.ToList();
                //ttxw.RemoveAll(item => item > 100.0f || item < 0.0f);

                //resBuffer = new ComputeBuffer((int)population, MeshProperties.Size());

                int postprocess_kernel = postprocess_shader.FindKernel("CSMain");
            postprocess_shader.SetBuffer(postprocess_kernel, "tsdfVolume", tsdfVolume);
            //tempx = new ComputeBuffer((int)population, sizeof(float));
            //tempy = new ComputeBuffer((int)population, sizeof(float));
            //tempz = new ComputeBuffer((int)population, sizeof(float));
            //tempw = new ComputeBuffer((int)population, sizeof(float));

            //tempx.SetData(ttx);
            //tempy.SetData(ttx);
            //tempz.SetData(ttx);
            //tempw.SetData(ttw);

            //postprocess_shader.SetBuffer(postprocess_kernel, "test_x", tempx);
            //postprocess_shader.SetBuffer(postprocess_kernel, "test_y", tempy);
            //postprocess_shader.SetBuffer(postprocess_kernel, "test_z", tempz);
            //postprocess_shader.SetBuffer(postprocess_kernel, "test_w", tempw);
            postprocess_shader.SetBuffer(postprocess_kernel, "_Properties", resBuffer);
            postprocess_shader.SetFloat("voxel_size", voxel_size);
            postprocess_shader.SetFloat("threshold_dis", threshold_distance);
            postprocess_shader.SetInt("dimy", voxel_y);
            postprocess_shader.SetInt("dimz", voxel_z);

            postprocess_shader.Dispatch(postprocess_kernel, 1, voxel_y, voxel_z);
            
            //postprocess_shader.Dispatch(postprocess_kernel, Mathf.CeilToInt(population / 128.0f), 1, 1);


            //float[] temp_tsdf = new float[num_voxel];
            //MeshProperties[] results = new MeshProperties[num_voxel];
            //int count = 0;

            //tsdfVolume.GetData(temp_tsdf);


            //for (int i = 0; i < results.Length; i++)
            //{
            //    int px = i % 128;
            //    int py = (i / 128) % 84;
            //    int pz = i / (128 * 84);

            //    float voxelSize = 0.025f;

            //    if (temp_tsdf[i] < 0.4f)
            //    {
            //        results[i].pos.x = (px + 0.5f) * voxelSize + 0.1f;
            //        results[i].pos.y = (py + 0.5f) * voxelSize + 0.5f;
            //        results[i].pos.z = (pz + 0.5f) * voxelSize + 1.8f;
            //        results[i].pos.w = 0.0f;
            //        count++;
            //    }
            //    else
            //    {
            //        results[i].pos.x = 0.0f;
            //        results[i].pos.y = 0.0f;
            //        results[i].pos.z = 0.0f;
            //        results[i].pos.w = 0.0f;
            //    }
            //}

            //resBuffer.GetData(results);

            executed = true;
        }

        //material
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        material.SetFloat("a", get_target_rota());
        material.SetFloat("pS", pS);
        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        material.SetVector("intrinsics", intr);
        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        material.SetVector("screenData", screenData);
        material.SetFloat("samplingSize", downsample);
        material.SetBuffer("_Properties", resBuffer);
        material.SetBuffer("uv_coord", coorBuffer);
        //material.SetBuffer("_Properties", meshPropertiesBuffer);
        material.SetTexture("_colorMap", color_image);

        //argsBuffer
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = mesh.GetIndexCount(0);
        //args[1] = population;
        args[1] = (uint)num_voxel;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        //material.SetBuffer("pointCloud", meshPropertiesBuffer);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        //numUpdates += 1;
    }

    private Vector4 pixel_to_vision_frame(uint i, uint j, float depth)
    {
        //int CX = 320;
        //int CY = 240;

        //float FX = (float)552.029101;
        //float FY = (float)552.029101;

        float x = (j - CX) * depth / FX;
        float y = (i - CY) * depth / FY;

        Vector4 ret = new Vector4(x, y, depth, 1f);
        return (ret);

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

    private Mesh CreateCube(float width = 1f, float height = 1f, float depth = 1f)
    {
        Mesh mesh = new Mesh();

        float w = width * 0.5f;
        float h = height * 0.5f;
        float d = depth * 0.5f;

        // Define the vertices of the cube
        Vector3[] vertices = new Vector3[]
        {
        // Front face
        new Vector3(-w, -h, d), // Bottom Left - 0
        new Vector3(w, -h, d), // Bottom Right - 1
        new Vector3(w, h, d), // Top Right - 2
        new Vector3(-w, h, d), // Top Left - 3

        // Back face
        new Vector3(-w, -h, -d), // Bottom Left - 4
        new Vector3(w, -h, -d), // Bottom Right - 5
        new Vector3(w, h, -d), // Top Right - 6
        new Vector3(-w, h, -d), // Top Left - 7

        // Left face
        new Vector3(-w, -h, -d), // Bottom Left - 8
        new Vector3(-w, -h, d), // Bottom Right - 9
        new Vector3(-w, h, d), // Top Right - 10
        new Vector3(-w, h, -d), // Top Left - 11

        // Right face
        new Vector3(w, -h, d), // Bottom Left - 12
        new Vector3(w, -h, -d), // Bottom Right - 13
        new Vector3(w, h, -d), // Top Right - 14
        new Vector3(w, h, d), // Top Left - 15

        // Top face
        new Vector3(-w, h, d), // Bottom Left - 16
        new Vector3(w, h, d), // Bottom Right - 17
        new Vector3(w, h, -d), // Top Right - 18
        new Vector3(-w, h, -d), // Top Left - 19

        // Bottom face
        new Vector3(-w, -h, -d), // Bottom Left - 20
        new Vector3(w, -h, -d), // Bottom Right - 21
        new Vector3(w, -h, d), // Top Right - 22
        new Vector3(-w, -h, d), // Top Left - 23
        };

        // Define each triangle of the cube
        int[] triangles = new int[]
        {
        // Front face
        2, 3, 0, 0, 1, 2,

        // Back face
        4, 6, 5, 4, 7, 6,

        // Left face
        8, 9, 10, 8, 10, 11,

        // Right face
        12, 13, 15, 15, 13, 14,

        // Top face
        16, 17, 18, 16, 18, 19,

        // Bottom face
        22, 23, 20, 21, 22, 20
        };

        // Automatically calculate normals for the cube
        Vector3[] normals = new Vector3[]
        {
       new Vector3(0, 0, 1),
       new Vector3(0, 0, 1),
       new Vector3(0, 0, 1),
       new Vector3(0, 0, 1),

       new Vector3(0, 0, -1),
       new Vector3(0, 0, -1),
       new Vector3(0, 0, -1),
       new Vector3(0, 0, -1),

       new Vector3(-1, 0, 0),
       new Vector3(-1, 0, 0),
       new Vector3(-1, 0, 0),
       new Vector3(-1, 0, 0),

       new Vector3(1, 0, 0),
       new Vector3(1, 0, 0),
       new Vector3(1, 0, 0),
       new Vector3(1, 0, 0),

       new Vector3(0, 1, 0),
       new Vector3(0, 1, 0),
       new Vector3(0, 1, 0),
       new Vector3(0, 1, 0),

       new Vector3(0, -1, 0),
       new Vector3(0, -1, 0),
       new Vector3(0, -1, 0),
       new Vector3(0, -1, 0),
        };

        // Define UVs for the cube
        Vector2[] uv = new Vector2[vertices.Length];

        // Assign vertices, triangles, normals, and UVs to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
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

    public void toggleFreezeCloud()
    {
        freezeCloud = !freezeCloud;
    }

    private void Start()
    {
        // See OnEnable
    }


    private void OnDisable()
    {
        // Release gracefully.
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        meshPropertiesBufferX.Release();
        meshPropertiesBufferY.Release();
        meshPropertiesBufferZ.Release();
        meshPropertiesBufferW.Release();
        tsdfVolume.Release();

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void OnEnable()
    {
        Setup();
    }

    void OnDestroy()
    {
        meshPropertiesBuffer.Release();
        meshPropertiesBufferX.Release();
        meshPropertiesBufferY.Release();
        meshPropertiesBufferZ.Release();
        meshPropertiesBufferW.Release();
        tsdfVolume.Release();
    }
}