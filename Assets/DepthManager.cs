using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthManager : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool mean_averaging;
    public bool median_averaging;
    public bool edge_detection;

    public float edge_threshold;

    private float[] depth_left;
    private Texture2D rgb_left;

    private float[] depth_right;
    private Texture2D rgb_right;

    private float[] output_left = new float[480 * 640];
    private float[] output_right = new float[480 * 640];

    private bool received_left = false;
    private bool received_right = false;

    private bool depth_process_lock = false;

    public DepthAveraging AveragerLeft;
    public DepthAveraging AveragerRight;

    public DrawMeshInstanced Left_Depth_Renderer;
    public DrawMeshInstanced Right_Depth_Renderer;

    bool first_run = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //public void ReceiveDataFromRenderer(Texture2D rgb, float[] depth, int camera_index)
    //{
    //    //if (camera_index == 0 && !depth_process_lock && !received_left)
    //    //{
    //    //    depth_left = depth;
    //    //    rgb_left = rgb;
    //    //    received_left = true;
    //    //    Debug.Log("receive left");
    //    //}
    //    //else if (camera_index == 1 && !depth_process_lock && !received_right)
    //    //{
    //    //    depth_right = depth;
    //    //    rgb_right = rgb;
    //    //    received_right = true;
    //    //    Debug.Log("receive right");
    //    //}

    //    if (camera_index == 0)
    //    {
    //        depth_left = depth;
    //        rgb_left = rgb;
    //    }
    //    else if (camera_index == 1)
    //    {
    //        depth_right = depth;
    //        rgb_right = rgb;
    //    }

    //    TryProcessDepths();
    //}

    public float[] update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index)
    {
        if (camera_index == 0 && !received_left)
        {
            depth_left = (float[])depth.Clone();
            rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_left);
            received_left = true;
        }
        else if (camera_index == 1 && !received_right)
        {
            depth_right = (float[])depth.Clone();
            rgb_right = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_right);
            received_right = true;
        }

        if (received_left && received_right && !depth_process_lock)
        {
            depth_process_lock = true;

            bool not_moving = Left_Depth_Renderer.get_ready_to_freeze() && Right_Depth_Renderer.get_ready_to_freeze();
            not_moving = true;
            (output_left, output_right) = process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving);

            received_left = false;
            received_right = false;

            depth_process_lock = false;
            first_run = true;
        }

        if (camera_index == 0)
        {
            return output_left;
        }
        else if(camera_index == 1)
        {
            return output_right;
        }

        return output_right;
    }

    //private void TryProcessDepths()
    //{
    //    //if (received_left && received_right && !depth_process_lock && !first_run)
    //    //{
    //    //    Debug.Log("TryProcessDepths");
    //    //    depth_process_lock = true;

    //    //    bool not_moving = Depth_rederer.get_ready_to_freeze();
    //    //    not_moving = true;
    //    //    (output_left, output_right) = process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving);

    //    //    received_left = false;
    //    //    received_right = false;

    //    //    depth_process_lock = false;
    //    //    first_run = true;
    //    //}

    //    if (received_left && received_right && !depth_process_lock)
    //    {
    //        Debug.Log("TryProcessDepths");
    //        depth_process_lock = true;

    //        bool not_moving = Left_Depth_Renderer.get_ready_to_freeze();
    //        not_moving = true;
    //        (output_left, output_right) = process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving);

    //        received_left = false;
    //        received_right = false;

    //        depth_process_lock = false;
    //        first_run = true;
    //    }
    //}

    //public float[] get_processed_depth(int camera_index)
    //{
    //    if (camera_index == 0)
    //    {
    //        return output_left;
    //    }
    //    else if (camera_index == 1)
    //    {
    //        return output_right;
    //    }

    //    return output_right;
    //}

    //public bool ready_to_get_res()
    //{
    //    return !received_right && first_run && !depth_process_lock;
    //}

    private (float[], float[]) process_depth(float[] depthL, Texture2D rgbL, float[] depthR, Texture2D rgbR, bool is_not_moving)
    {
        if (median_averaging && mean_averaging)
        {
            mean_averaging = false;
        }

        float[] temp_output_left = depthL, temp_output_right = depthR;

        // depth completion
        //Debug.Log("depth completion");
        if (activate_depth_estimation && is_not_moving)
        {
            (temp_output_left, temp_output_right) = GetComponent<DepthCompletion>().complete(depthL, rgbL, depthR, rgbR);
        }

        temp_output_left = AveragerLeft.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        temp_output_right = AveragerRight.averaging(temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);

        return (temp_output_left, temp_output_right);
    }
}
