using System.IO;
using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// Records the main camera frame by frame on a fixed clock (Time.captureFramerate), so a
    /// clip is smooth however slowly the Editor renders, and ends play when the pilot finishes.
    /// Frames go to outDir as 00000.png and up; tools/frames-to-mp4.sh joins them.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class FrameRecorder : MonoBehaviour
    {
        public string outDir = "Screenshots/frames";
        public int fps = 30;
        public int every = 1;
        public int width = 1280, height = 720;
        public StretchPilot pilot;
        public float tailSeconds = 1.5f;

        Camera cam;
        RenderTexture rt;
        Texture2D readback;
        int frame, written;
        float finishedAt = -1f;

        void Start()
        {
            cam = GetComponent<Camera>();
            Time.captureFramerate = fps;
            if (Directory.Exists(outDir))
                foreach (var f in Directory.GetFiles(outDir, "*.png"))
                    File.Delete(f);
            Directory.CreateDirectory(outDir);
            rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            readback = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        void LateUpdate()
        {
            if (frame++ % every == 0)
            {
                var old = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = old;
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"{written++:00000}.png"), readback.EncodeToPNG());
            }
            if (pilot != null && pilot.Finished)
            {
                if (finishedAt < 0f)
                    finishedAt = Time.time;
                else if (Time.time - finishedAt > tailSeconds)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            Time.captureFramerate = 0;
            if (rt != null)
                rt.Release();
        }
    }
}
