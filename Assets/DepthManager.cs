using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DepthManager : MonoBehaviour
{
    public bool activate_depth_estimation;
    public bool mean_averaging;
    public bool median_averaging;
    public bool edge_detection;

    public float edge_threshold;



    private float[] spot1_depth_left;
    private Texture2D spot1_rgb_left;

    private float[] spot1_depth_right;
    private Texture2D spot1_rgb_right;

    private float[] spot2_depth_left;
    private Texture2D spot2_rgb_left;

    private float[] spot2_depth_right;
    private Texture2D spot2_rgb_right;



    private float[] spot1_output_left = new float[480 * 640];
    private float[] spot1_output_right = new float[480 * 640];
    private float[] spot2_output_left = new float[480 * 640];
    private float[] spot2_output_right = new float[480 * 640];

    private bool spot1_received_left = false;
    private bool spot1_received_right = false;
    private bool spot2_received_left = false;
    private bool spot2_received_right = false;

    private bool depth_process_lock = false;

    public DepthAveraging Spot1AveragerLeft;
    public DepthAveraging Spot1AveragerRight;
    public DepthAveraging Spot2AveragerLeft;
    public DepthAveraging Spot2AveragerRight;

    public DrawMeshInstanced Spot1_Left_Depth_Renderer;
    public DrawMeshInstanced Spot1_Right_Depth_Renderer;
    public DrawMeshInstanced Spot2_Left_Depth_Renderer;
    public DrawMeshInstanced Spot2_Right_Depth_Renderer;

    private float deltaTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        if (activate_depth_estimation)
        {
            StartCoroutine(ResetActivateDepthEstimation());
        }
        
    }

    private IEnumerator ResetActivateDepthEstimation()
    {
        activate_depth_estimation = false;
        yield return new WaitForSeconds(0.5f);
        activate_depth_estimation = true;
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
    }

    public float[] update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index)
    {
        if (camera_index == 0 && !spot1_received_left)
        {
            spot1_depth_left = (float[])depth.Clone();

            if (spot1_rgb_left != null)
            {
                Destroy(spot1_rgb_left);
            }
            spot1_rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, spot1_rgb_left);
            spot1_received_left = true;
        }
        else if (camera_index == 1 && !spot1_received_right)
        {
            spot1_depth_right = (float[])depth.Clone();

            if (spot1_rgb_right != null)
            {
                Destroy(spot1_rgb_right);
            }
            spot1_rgb_right = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, spot1_rgb_right);
            spot1_received_right = true;
        }
        else if (camera_index == 2 && !spot2_received_left)
        {
            spot2_depth_left = (float[])depth.Clone();

            if (spot2_rgb_left != null)
            {
                Destroy(spot2_rgb_left);
            }
            spot2_rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, spot2_rgb_left);
            spot2_received_left = true;
        }
        else if (camera_index == 3 && !spot2_received_right)
        {
            spot2_depth_right = (float[])depth.Clone();

            if (spot2_rgb_right != null)
            {
                Destroy(spot2_rgb_right);
            }
            spot2_rgb_right = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, spot2_rgb_right);
            spot2_received_right = true;
        }


        if (spot1_received_left && spot1_received_right && spot2_received_left && spot2_received_right && !depth_process_lock)
        {
            depth_process_lock = true;

            bool not_moving = Spot1_Left_Depth_Renderer.get_ready_to_freeze() && Spot1_Right_Depth_Renderer.get_ready_to_freeze()
                && Spot2_Left_Depth_Renderer.get_ready_to_freeze() && Spot2_Right_Depth_Renderer.get_ready_to_freeze();

            //not_moving = true;
            (spot1_output_left, spot1_output_right, spot2_output_left, spot2_output_right) = process_depth(spot1_depth_left, spot1_rgb_left, spot1_depth_right, spot1_rgb_right,
                spot2_depth_left, spot2_rgb_left, spot2_depth_right, spot2_rgb_right, not_moving);

            spot1_received_left = false;
            spot1_received_right = false;
            spot2_received_left = false;
            spot2_received_right = false;

            depth_process_lock = false;
        }

        if (camera_index == 0)
        {
            return spot1_output_left;
        }
        else if (camera_index == 1)
        {
            return spot1_output_right;
        }
        else if (camera_index == 2)
        {
            return spot2_output_left;
        }
        else if (camera_index == 3)
        {
            return spot2_output_right;
        }

        return spot1_output_left;
    }

    private (float[], float[], float[], float[]) process_depth(float[] depthL1, Texture2D rgbL1, 
                                            float[] depthR1, Texture2D rgbR1,
                                            float[] depthL2, Texture2D rgbL2,
                                            float[] depthR2, Texture2D rgbR2, 
                                            bool is_not_moving)
    {
        if (median_averaging && mean_averaging)
        {
            mean_averaging = false;
        }

        float[] spot1_temp_output_left = depthL1, spot1_temp_output_right = depthR1;
        float[] spot2_temp_output_left = depthL2, spot2_temp_output_right = depthR2;

        // depth completion
        if (activate_depth_estimation && is_not_moving)
        {
            (spot1_temp_output_left, spot1_temp_output_right, spot2_temp_output_left, spot2_temp_output_right) = 
                GetComponent<DepthCompletion>().complete(depthL1, rgbL1, depthR1, rgbR1,
                                                        depthL2, rgbL2, depthR2, rgbR2);
        }

        spot1_temp_output_left = Spot1AveragerLeft.averaging(spot1_temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        spot1_temp_output_right = Spot1AveragerRight.averaging(spot1_temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        spot2_temp_output_left = Spot2AveragerLeft.averaging(spot2_temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        spot2_temp_output_right = Spot2AveragerRight.averaging(spot2_temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);

        return (spot1_temp_output_left, spot1_temp_output_right, spot2_temp_output_left, spot2_temp_output_right);
    }
}
