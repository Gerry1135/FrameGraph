using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace FrameGraph
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class FrameGraph : MonoBehaviour
    {
        private const int width = 512;
        private const int height = 512;

        private Rect windowPos = new Rect(80, 80, 400, 200);
        private bool showUI = false;
        readonly Texture2D graphTexture = new Texture2D(width, height);

        private frameState[] frameHistory = new frameState[width];

        int frameIndex = 0;
        int lastRendered = 0;

        long lastTime = -1;
        long lastCount;

        bool fullUpdate = true;

        double timeScale;
        double countScale;

        Color[] blackSquare;
        Color[] blackLine;
        Color[] line;

        private GUIStyle labelStyle;
        private GUILayoutOption noExpandWidth;
        private GUILayoutOption wndWidth;
        private GUILayoutOption wndHeight;
        private GUILayoutOption boxHeight;

        Color color = new Color();

        struct frameState
        {
            public long time;
            public long gc;
        }

        internal void Awake()
        {
            DontDestroyOnLoad(gameObject);

            line = new Color[height];
            blackLine = new Color[height * 2];
            for (int i = 0; i < blackLine.Length; i++)
                blackLine[i] = Color.black;

            for (int i = 0; i < width; i++)
            {
                frameHistory[i].time = 0;
                frameHistory[i].gc = 0;
            }

            lastTime = Stopwatch.GetTimestamp();
            lastCount = GC.CollectionCount(GC.MaxGeneration);
        }
        
        internal void OnDestroy()
        {
        }

        public void Update()
        {
            // First thing is to record the time and the GCCount delta for this frame
            long time = Stopwatch.GetTimestamp();
            frameHistory[frameIndex].time = time - lastTime;
            lastTime = time;
            int count = GC.CollectionCount(GC.MaxGeneration);
            frameHistory[frameIndex].gc = count - lastCount;
            lastCount = count;
            frameIndex = (frameIndex + 1) % width;

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.Equals))
            {
                showUI = !showUI;
            }

            if (!showUI)
                return;

            // Work out if the scale needs to change
            timeScale = (double)height * 4 / (double)Stopwatch.Frequency;
            countScale = 16;

            // fullUpdate = true;

            if (fullUpdate)
            {
                fullUpdate = false;
                for (int x = 0; x < graphTexture.width; x++)
                    DrawColumn(x);

                graphTexture.SetPixels(frameIndex, 0, 1, height, blackLine);
                graphTexture.SetPixels(frameIndex + 1, 0, 1, height, blackLine);

                graphTexture.Apply();

                lastRendered = frameIndex;
            }
            else
            {
                // If we want to update this time
                if (true)
                {
                    // Update the columns from lastRendered to frameIndex
                    if (lastRendered >= frameIndex)
                    {
                        for (int x = lastRendered; x < width; x++)
                            DrawColumn(x);

                        lastRendered = 0;
                    }
                    
                    for (int x = lastRendered; x < frameIndex; x++)
                        DrawColumn(x);

                    lastRendered = frameIndex;

                    graphTexture.SetPixels(frameIndex, 0, 1, height, blackLine);
                    graphTexture.SetPixels(frameIndex + 1, 0, 1, height, blackLine);
                    graphTexture.Apply();
                }
            }
        }

        private void DrawColumn(int x)
        {
            int timeY = (int)(frameHistory[x].time * timeScale);
            int countY = (int)(frameHistory[x].gc * countScale);
            //print("" + timeY);
            for (int y = 0; y < graphTexture.height; y++)
            {
                if (y <= countY)
                    color = Color.red;
                else if (y <= timeY)
                    color = Color.green;
                else
                    color = Color.black;
                    
                line[y] = color;
            }

            graphTexture.SetPixels(x, 0, 1, height, line);
        }

        public void OnGUI()
        {
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label);

            if (wndWidth == null)
                wndWidth = GUILayout.Width(width);
            if (wndHeight == null)
                wndHeight = GUILayout.Height(height);

            if (showUI)
            {
                windowPos = GUILayout.Window(5421235, windowPos, WindowGUI, "FrameGraph", wndWidth, wndHeight);
            }
        }

        public void WindowGUI(int windowID)
        {
            GUILayout.Box(graphTexture, wndWidth, wndHeight);

            GUI.DragWindow();
        }
    }
}
