using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DepthManagerWrapper : MonoBehaviour
{
    private float[] depth_left;
    private Texture2D rgb_left;

    private float[] depth_right;
    private Texture2D rgb_right;

    private float[] output_left = new float[480 * 640];
    private float[] output_right = new float[480 * 640];

    private bool is_estimating = false;
    private bool received_left = false;
    private bool received_right = false;

    public DrawMeshInstanced Left_Depth_Renderer;
    public DrawMeshInstanced Right_Depth_Renderer;

    private DepthCompletion depth_completion;

    // ============================================== Depth Manager =========================================================
    public bool activate_depth_estimation;
    public bool mean_averaging;
    public bool median_averaging;
    public bool edge_detection;

    public DepthAveraging AveragerLeft;
    public DepthAveraging AveragerRight;

    public float edge_threshold;

    public float[] get_depth(int camera_index)
    {
        if (camera_index == 0)
        {
            return output_left;
        }
        return output_right;
    }

    public void set_data(int camera_index, Texture2D rgb, float[] depth)
    {
        if (camera_index == 0 && !is_estimating)
        {
            depth_left = (float[])depth.Clone();

            if (rgb_left != null)
            {
                Destroy(rgb_left);
            }
            rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_left);

            received_left = true;
        }
        else if (camera_index == 1 && !is_estimating)
        {
            depth_right = (float[])depth.Clone();

            if (rgb_right != null)
            {
                Destroy(rgb_right);
            }
            rgb_right = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_right);

            received_right = true;
        }
    }

    void Update()
    {
        if (!is_estimating)
        {
            bool is_not_moving = Left_Depth_Renderer.get_ready_to_freeze();
            output_left = AveragerLeft.averaging(output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
            output_right = AveragerRight.averaging(output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        }
        if (received_left && received_right && !is_estimating)
        {
            is_estimating = true;
            Task.Run(() => ProcessDepthAsync()).ContinueWith(task =>
            {
                if (task.Exception != null)
                    Debug.LogError("Depth processing failed: " + task.Exception);
                is_estimating = false;
            });
        }
    }

    async Task ProcessDepthAsync()
    {
        bool not_moving = Left_Depth_Renderer.get_ready_to_freeze();
        (output_left, output_right) = await Task.Run(() => process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving));
    }

    void Start()
    {
        depth_completion = GetComponent<DepthCompletion>();
        if (activate_depth_estimation)
        {
            StartCoroutine(ResetActivateDepthEstimation());
        }
    }


    // ============================================== Depth Manager =========================================================

    private IEnumerator ResetActivateDepthEstimation()
    {
        activate_depth_estimation = false;
        yield return new WaitForSeconds(0.1f);
        activate_depth_estimation = true;
    }

    public (float[], float[]) process_depth(float[] depthL, Texture2D rgbL, float[] depthR, Texture2D rgbR, bool is_not_moving)
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
            //fps_timer.start(depth_completion_timer_id);
            (temp_output_left, temp_output_right) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
            //fps_timer.end(depth_completion_timer_id);
        }

        //fps_timer.start(averaging_timer_id);
        //temp_output_left = AveragerLeft.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        //temp_output_right = AveragerRight.averaging(temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        //fps_timer.end(averaging_timer_id);

        return (temp_output_left, temp_output_right);
    }
}
