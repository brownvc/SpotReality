using System.Collections;
using System.Collections.Generic;
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

    private DepthManager depth_manager;

    public DrawMeshInstanced Left_Depth_Renderer;
    public DrawMeshInstanced Right_Depth_Renderer;

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
        if (received_right && received_left)
        {
            is_estimating = true;
            Debug.Log("here");
            bool not_moving = Left_Depth_Renderer.get_ready_to_freeze();
            (output_left, output_right) = depth_manager.process_depth(depth_left, rgb_left, depth_right, rgb_right, not_moving);
            is_estimating = false;
        }
    }

    void Start()
    {
        depth_manager = GetComponent<DepthManager>();
    }
}
