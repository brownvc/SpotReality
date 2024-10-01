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

    private float deltaTime = 0.0f;

    public GameObject FPSDisplayObject;

    private FPSCounter fps_timer;
    private int depth_completion_timer_id;
    private int averaging_timer_id;
    private int left_eye_data_timer_id;
    private int right_eye_data_timer_id;

    private DepthCompletion depth_completion;
    private ComputeBuffer temp_output_left;
    private ComputeBuffer temp_output_right;

    // Start is called before the first frame update
    void Start()
    {
        depth_completion = GetComponent<DepthCompletion>();
        temp_output_left = new ComputeBuffer(480 * 640, sizeof(float));
        temp_output_right = new ComputeBuffer(480 * 640, sizeof(float));

        fps_timer = FPSDisplayObject.GetComponent<FPSCounter>();

        depth_completion_timer_id = fps_timer.registerTimer("Depth completion");
        averaging_timer_id = fps_timer.registerTimer("Mean Averaging");
        left_eye_data_timer_id = fps_timer.registerTimer("Left eye data");
        right_eye_data_timer_id = fps_timer.registerTimer("Right eye data");


        if (activate_depth_estimation)
        {
            StartCoroutine(ResetActivateDepthEstimation());
        }
        
    }


    private IEnumerator ResetActivateDepthEstimation()
    {
        activate_depth_estimation = false;
        yield return new WaitForSeconds(0.1f);
        activate_depth_estimation = true;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public ComputeBuffer update_depth_from_renderer(Texture2D rgb, float[] depth, int camera_index)
    {
        if (camera_index == 0 && !received_left)
        {
            fps_timer.start(left_eye_data_timer_id);

            depth_left = (float[])depth.Clone();

            if (rgb_left != null)
            {
                Destroy(rgb_left);
            }
            rgb_left = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_left);
            received_left = true;

            fps_timer.end(left_eye_data_timer_id);
        }
        else if (camera_index == 1 && !received_right)
        {
            fps_timer.start(right_eye_data_timer_id);

            depth_right = (float[])depth.Clone();

            if (rgb_right != null)
            {
                Destroy(rgb_right);
            }
            rgb_right = new Texture2D(rgb.width, rgb.height, rgb.format, rgb.mipmapCount > 1);
            Graphics.CopyTexture(rgb, rgb_right);
            received_right = true;

            fps_timer.end(right_eye_data_timer_id);
        }

        if (received_left && received_right && !depth_process_lock)
        {
            depth_process_lock = true;

            //bool not_moving = Left_Depth_Renderer.get_ready_to_freeze() && Right_Depth_Renderer.get_ready_to_freeze();
            bool not_moving = Left_Depth_Renderer.get_ready_to_freeze();
            //not_moving = true;
            (temp_output_left, temp_output_right) = process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving);

            received_left = false;
            received_right = false;

            depth_process_lock = false;
            first_run = true;
        }

        if (camera_index == 0)
        {
            return temp_output_left;
        }
        else if (camera_index == 1)
        {
            return temp_output_right;
        }

        return temp_output_right;
    }

    private (ComputeBuffer, ComputeBuffer) process_depth(float[] depthL, Texture2D rgbL, float[] depthR, Texture2D rgbR, bool is_not_moving)
    {
        if (median_averaging && mean_averaging)
        {
            mean_averaging = false;
        }

        //float[] temp_output_left = depthL, temp_output_right = depthR;

        // depth completion
        //Debug.Log("depth completion");
        if (activate_depth_estimation && is_not_moving)
        {
            fps_timer.start(depth_completion_timer_id);
            (temp_output_left, temp_output_right) = depth_completion.complete(depthL, rgbL, depthR, rgbR);
            fps_timer.end(depth_completion_timer_id);
        }

        fps_timer.start(averaging_timer_id);
        temp_output_left = AveragerLeft.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        temp_output_right = AveragerRight.averaging(temp_output_right, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        fps_timer.end(averaging_timer_id);

        //float[] ol = new float[480 * 640], or = new float[480 * 640];

        //temp_output_left.GetData(ol);
        //temp_output_right.GetData(or);

        return (temp_output_left, temp_output_right);
    }
}
