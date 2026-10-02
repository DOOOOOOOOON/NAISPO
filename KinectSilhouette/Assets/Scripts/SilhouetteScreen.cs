using System;
using System.IO;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;
using UnityEngine;

public class SilhouetteScreen : MonoBehaviour
{
    public bool mirror = true;
    public float scale = 1f;
    public Vector2 offset;
    public Color silhouetteColor = Color.black;
    [Range(0,1)] public float backgroundLighten = .2f;
    public int gpuDeviceId = 0;
    public bool showControls = true;
    Texture2D background, mask;
    Thread worker;
    volatile bool stopping;
    volatile string status = "Starting Kinect...";
    readonly object sync = new object();
    byte[] latest;
    int width, height, stride;
    Color32[] pixels;
    static int activeSession;
    bool verify;
    int receivedFrames;

    void Start()
    {
        Application.runInBackground = true;
        verify = Array.IndexOf(Environment.GetCommandLineArgs(), "--verify-sdk") >= 0;
        Application.targetFrameRate = 60;
        background = Resources.Load<Texture2D>("forest");
        if (Interlocked.CompareExchange(ref activeSession, 1, 0) != 0)
        { status = "Previous Kinect session is still stopping. Restart Play after it finishes."; return; }
        string model = Path.Combine(Application.streamingAssetsPath, "dnn_model_2_0_op11.onnx");
        int gpu = gpuDeviceId;
        worker = new Thread(() => CaptureLoop(model, gpu)) { IsBackground = true };
        worker.Start();
    }

    void CaptureLoop(string model, int gpu)
    {
        try
        {
            if (!File.Exists(model)) throw new FileNotFoundException("Body tracking model missing", model);
            using (var device = Device.Open(0))
            {
                device.StartCameras(new DeviceConfiguration {
                    ColorResolution = ColorResolution.Off, DepthMode = DepthMode.NFOV_Unbinned,
                    CameraFPS = FPS.FPS30, WiredSyncMode = WiredSyncMode.Standalone });
                try
                {
                    status = "Loading body tracking model...";
                    using (var tracker = Tracker.Create(device.GetCalibration(), new TrackerConfiguration {
                        ProcessingMode = TrackerProcessingMode.DirectML,
                        SensorOrientation = SensorOrientation.Default, GpuDeviceId = gpu, ModelPath = model }))
                    {
                        while (!stopping)
                        {
                            try
                            {
                                using (var capture = device.GetCapture(TimeSpan.FromMilliseconds(200)))
                                    tracker.EnqueueCapture(capture, TimeSpan.FromMilliseconds(200));
                            }
                            catch (TimeoutException) { }
                            using (var frame = tracker.PopResult(TimeSpan.FromMilliseconds(200), false))
                            {
                                if (frame == null) continue;
                                using (var map = frame.BodyIndexMap)
                                {
                                    byte[] data = map.Memory.ToArray();
                                    lock (sync) { latest = data; width = map.WidthPixels; height = map.HeightPixels; stride = map.StrideBytes; }
                                }
                                status = "People: " + frame.NumberOfBodies + " | Live";
                            }
                        }
                        tracker.Shutdown();
                    }
                }
                finally { device.StopCameras(); }
            }
        }
        catch (Exception ex) { status = "Kinect error: " + ex.Message; Debug.LogException(ex); }
        finally { Interlocked.Exchange(ref activeSession, 0); }
    }

    void Update()
    {
        if (verify && Time.realtimeSinceStartup > 25) { Debug.Log("Verification: frames=" + receivedFrames + "; " + status); Application.Quit(); }
        if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
        if (Input.GetKeyDown(KeyCode.M)) mirror = !mirror;
        if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;
        if (Input.GetKeyDown(KeyCode.Escape)) Screen.fullScreen = false;
        float step = Time.deltaTime * 0.3f;
        if (Input.GetKey(KeyCode.LeftArrow)) offset.x -= step;
        if (Input.GetKey(KeyCode.RightArrow)) offset.x += step;
        if (Input.GetKey(KeyCode.UpArrow)) offset.y -= step;
        if (Input.GetKey(KeyCode.DownArrow)) offset.y += step;
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) scale += step;
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) scale = Mathf.Max(.2f, scale - step);
        if (Input.GetKeyDown(KeyCode.R)) { scale = 1; offset = Vector2.zero; mirror = true; }
        byte[] data; int w,h,s;
        lock (sync) { data = latest; latest = null; w = width; h = height; s = stride; }
        if (data == null) return;
        receivedFrames++;
        if (mask == null || mask.width != w || mask.height != h)
        {
            if (mask != null) Destroy(mask);
            mask = new Texture2D(w,h,TextureFormat.RGBA32,false);
            mask.wrapMode = TextureWrapMode.Clamp;
            pixels = new Color32[w*h];
        }
        Color32 ink = silhouetteColor;
        for (int y=0;y<h;y++) for (int x=0;x<w;x++)
            pixels[(h-1-y)*w+x] = data[y*s+x] == 255 ? new Color32(0,0,0,0) : ink;
        mask.SetPixels32(pixels); mask.Apply(false);
    }

    void OnGUI()
    {
        if (background != null) GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), background, ScaleMode.ScaleAndCrop);
        GUI.color = new Color(1,1,1,backgroundLighten);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (mask != null)
        {
            float h = Screen.height * scale, w = h * mask.width / mask.height;
            Rect rect = new Rect((Screen.width-w)/2 + offset.x*Screen.width, (Screen.height-h)/2 + offset.y*Screen.height,w,h);
            GUI.DrawTextureWithTexCoords(rect,mask,mirror ? new Rect(1,0,-1,1) : new Rect(0,0,1,1),true);
        }
        if (!showControls) return;
        GUI.Box(new Rect(12,12,640,90), "");
        GUI.Label(new Rect(24,20,615,45), status);
        GUI.Label(new Rect(24,64,615,30), "M: mirror | Arrows: move | +/-: size | R: reset | H: hide | F: fullscreen");
    }

    void OnDestroy()
    {
        stopping = true;
        if (worker != null && !worker.Join(5000)) Debug.LogWarning("Kinect shutdown pending; wait before starting another session.");
        if (mask != null) Destroy(mask);
    }
}
