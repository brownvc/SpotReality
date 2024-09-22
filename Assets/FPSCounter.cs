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
        //for (int i = 0; i < timer_count; i++)
        //{
        //    named_timer nt = timer_map[i];

        //    double diff = nt.end_time - nt.start_time;
        //    nt.running_delta += diff;
        //    nt.n_frames++;

        //    timer_map[i] = nt;
        //}
        
        if (deltaTime > updateInterval) {
            string str = "";

            foreach (KeyValuePair<int, named_timer> kvp in timer_map)
            {
                named_timer nt = kvp.Value;
                str += nt.name + ": ";
                double diff = nt.running_delta / n_frames;
                double ms = diff * 1000.0;
                double fps = 1.0 / diff;
                str += string.Format("{0:0.00000} ms - ", ms);
                str += string.Format("{0:0.00} fps\n", fps);

                n_frames = 0;
                deltaTime = 0.0f;
            }

            fpsText.text = str;
        }

        start(this.overall_fps_id);
    }
}
