/*
 Copyright (c) 2016 Gerry Iles (Padishar)

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

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
        readonly Texture2D graphTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        private frameState[] frameHistory = new frameState[width];

        int frameIndex = 0;
        int lastRendered = 0;

        long lastTime;
        long lastCount;

        long maxFrameTime = 0;
        const String maxFSPattern = "Max: {0}ms";
        String maxFrameTimeStr;

        long minFrameTime = 1000000000;
        const String minFSPattern = "Min: {0}ms";
        String minFrameTimeStr;

        long ticksPerMilliSec;
        long ticksPerSec;

        long lastFpsTime;
        const String avgFpsPattern = "Fps: {0}";
        String avgFpsStr;
        long lastAvgFpsx10 = 0;

        bool fullUpdate = true;

        double timeScale;
        double countScale;

        Color[] blackLine;
        Color[] line;

        private GUIStyle labelStyle;
		private GUIStyle graphStyle;
		private GUILayoutOption wndWidth;
        private GUILayoutOption wndHeight;

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
            ticksPerSec = Stopwatch.Frequency;
            ticksPerMilliSec = ticksPerSec / 1000;
            lastCount = GC.CollectionCount(GC.MaxGeneration);
        }
        
        internal void OnDestroy()
        {
        }

        public void Update()
        {
            // First thing is to record the time and the GCCount delta for this frame
            long time = Stopwatch.GetTimestamp();
            long timedelta = time - lastTime;
            frameHistory[frameIndex].time = timedelta;
            if (timedelta > maxFrameTime)
            {
                maxFrameTime = timedelta;
                maxFrameTimeStr = String.Format(maxFSPattern, (maxFrameTime / ticksPerMilliSec));
            }
            if (timedelta < minFrameTime)
            {
                minFrameTime = timedelta;
                minFrameTimeStr = String.Format(minFSPattern, (minFrameTime / ticksPerMilliSec));
            }
            lastTime = time;
            int count = GC.CollectionCount(GC.MaxGeneration);
            frameHistory[frameIndex].gc = count - lastCount;
            lastCount = count;

            if ((time - lastFpsTime) > ticksPerSec)
            {
                long totalTicks = 0;
                long numFrames = 0;
                int f = frameIndex;
                while (numFrames < width && totalTicks < (5 * ticksPerSec))
                {
                    totalTicks += frameHistory[f].time;
                    f = (f == 0) ? width - 1 : f - 1;
                    numFrames++;
                }
                long avgFpsx10 = (numFrames * 10000 * ticksPerMilliSec) / totalTicks;
                if (avgFpsx10 != lastAvgFpsx10)
                {
                    lastAvgFpsx10 = avgFpsx10;
                    avgFpsStr = String.Format(avgFpsPattern, ((double)lastAvgFpsx10 / 10d));
                }
                lastFpsTime = time;
            }

            frameIndex = (frameIndex + 1) % width;

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.Equals))
            {
                showUI = !showUI;
            }

            if (!showUI)
                return;

            // Work out if the scale needs to change
            timeScale = (double)height * 2 / (double)Stopwatch.Frequency;
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
			if (graphStyle == null)
				graphStyle = new GUIStyle();
            if (wndWidth == null)
                wndWidth = GUILayout.Width(width);
            if (wndHeight == null)
                wndHeight = GUILayout.Height(height);

            if (showUI)
            {
                windowPos = GUILayout.Window(5421235, windowPos, WindowGUI, "FrameGraph 1.0.0.3", wndWidth, wndHeight);
            }
        }

        public void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
			GUILayout.Box(graphTexture, graphStyle, wndWidth, wndHeight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(minFrameTimeStr, labelStyle);
            GUILayout.Label(maxFrameTimeStr, labelStyle);
            GUILayout.Label(avgFpsStr, labelStyle);
            if (GUILayout.Button("Reset"))
            {
                maxFrameTime = 0;
                maxFrameTimeStr = String.Format(maxFSPattern, (maxFrameTime / ticksPerMilliSec));
                minFrameTime = 1000000000;
                minFrameTimeStr = String.Format(minFSPattern, (minFrameTime / ticksPerMilliSec));
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
