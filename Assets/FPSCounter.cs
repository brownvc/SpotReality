using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    struct named_timer
    {
        public string name;
        public double start_time;
        public double running_delta;
        public int n_updates;

        public named_timer(string name)
        {
            this.name          = name;
            this.start_time    = 0;
            this.running_delta = 0.0;
            this.n_updates = 0;
        }
    }

    // A reference to the UI element
    public TMPro.TextMeshProUGUI fpsText;

    // A mapping between and ID and the timer associated with it.
    private Dictionary<int, named_timer> timer_map = new();
    private int timer_count = 0;

    private int overall_fps_id;

    // Display update rate
    private float deltaTime = 0.0f;
    private float updateInterval = 1.0f/4.0f;  // How often should we update
    private int n_frames = 0;

    // Start is called before the first frame update
    void Start()
    {

        this.overall_fps_id = registerTimer("Overall");

        start(this.overall_fps_id);
    }

    public void start(int timer_id)
    {
        // TODO: modify in one shot
        named_timer nt = timer_map[timer_id];
        nt.start_time = Time.realtimeSinceStartupAsDouble;
        timer_map[timer_id] = nt;

    }

    public void end(int timer_id)
    {
        // TODO: modify in one shot
        named_timer nt = timer_map[timer_id];
        double delta = Time.realtimeSinceStartupAsDouble - nt.start_time;
        nt.running_delta += delta;
        nt.n_updates++;
        timer_map[timer_id] = nt;
    }

    // To be called at init time
    public int registerTimer(string name)
    {
        timer_map.Add(timer_count, new named_timer(name));

        return timer_count++;
    }

    // Update is called once per frame
    // Will update the FPS UI display with all the registered timers
    void Update()
    {
        //string str = "We have " + timer_count + " timers\n";
        end(this.overall_fps_id);

        deltaTime += Time.deltaTime;
        n_frames += 1;


        //// TODO: Figure out how to access values by reference from a Dictionary...
        if (deltaTime > updateInterval) {
            string str = "";

            for (int i = 0; i < timer_count; i++)
            {

                named_timer nt = timer_map[i];
                str += nt.name + ": ";
                double diff = nt.running_delta / n_frames;
                if (n_frames != nt.n_updates)
                {
                    Debug.LogWarning(
                        "n_updates " + nt.n_updates.ToString() + " != n_frames "
                        + n_frames.ToString() + " for timer " + nt.name
                   );
                }
                double ms = diff * 1000.0;
                double fps = 1.0 / diff;
                str += string.Format("{0:0.00000} ms - ", ms);
                str += string.Format("{0:0.00} fps\n", fps);

                nt.n_updates     = 0;
                nt.running_delta = 0;


                timer_map[i] = nt;
            }
            n_frames = 0;
            deltaTime = 0.0f;

            fpsText.text = str;
        }

        start(this.overall_fps_id);
    }
}
