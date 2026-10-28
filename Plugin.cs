using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;

[BepInPlugin("foxyjumpscare", "Foxy Jumpscare", "1.0.1")]
public class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource L;
    internal static ConfigEntry<int> Denominator;
    internal static ConfigEntry<int> Fps;

    public override void Load()
    {
        L = Log;
        Denominator = Config.Bind("General", "ChanceDenominator", 1000,
            "Each second there is a 1 in N chance of the jumpscare. Set to 5 to test.");
        Fps = Config.Bind("General", "Fps", 20, "Must match the fps used in ffmpeg.");

        ClassInjector.RegisterTypeInIl2Cpp<Jumpscare>();
        var go = new GameObject("FoxyJumpscare");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<Jumpscare>();
        Log.LogInfo("Foxy Jumpscare loaded");
    }
}

public class Jumpscare : MonoBehaviour
{
    public Jumpscare(IntPtr ptr) : base(ptr) { }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    static readonly List<Texture2D> frames = new List<Texture2D>();
    static bool loaded, playing;
    static float timer, playTime;
    static int current;
    static int frameW, frameH;
    static string dir, wavPath;
    static GameObject canvasGo;
    static RawImage img;

    static void LoadAll()
    {
        loaded = true;
        dir = Path.Combine(Paths.PluginPath, "FoxyJumpscare");
        wavPath = Path.Combine(dir, "foxy.wav");
        var files = Directory.GetFiles(Path.Combine(dir, "frames"), "*.rgba").OrderBy(f => f).ToArray();
        const int W = 640; // must match scale=640 in ffmpeg
        foreach (var f in files)
        {
            byte[] bytes = File.ReadAllBytes(f);
            int h = bytes.Length / 4 / W;
            frameW = W; frameH = h;

            var tex = new Texture2D(W, h, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave; // survives UnloadUnusedAssets
            tex.wrapMode = TextureWrapMode.Clamp;

            IntPtr p = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, p, bytes.Length);
                tex.LoadRawTextureData(p, bytes.Length);
                tex.Apply();
            }
            finally { Marshal.FreeHGlobal(p); }
            frames.Add(tex);
        }
        Plugin.L.LogInfo($"Loaded {frames.Count} jumpscare frames ({frameW}x{frameH})");
    }

    static void EnsureUI()
    {
        if (canvasGo != null) return;
        canvasGo = new GameObject("FoxyCanvas");
        UnityEngine.Object.DontDestroyOnLoad(canvasGo);
        canvasGo.hideFlags = HideFlags.HideAndDontSave;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var imgGo = new GameObject("FoxyImage");
        imgGo.transform.SetParent(canvasGo.transform, false);
        img = imgGo.AddComponent<RawImage>();
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // fit the frame to the screen, keeping its aspect ratio
        float fw = frameW, fh = frameH;
        float s = Math.Min(Screen.width / fw, Screen.height / fh);
        rt.sizeDelta = new Vector2(fw * s, fh * s);
        canvasGo.SetActive(false);
    }

    static void Play()
    {
        if (frames.Count == 0) return;
        if (frames[0] == null) { Plugin.L.LogError("Frames were destroyed by Unity"); return; }
        try
        {
            EnsureUI();
            img.texture = frames[0];
            canvasGo.SetActive(true);
        }
        catch (Exception e) { Plugin.L.LogError(e.ToString()); return; }

        playing = true; playTime = 0f; current = 0;
        try { PlaySound(wavPath, IntPtr.Zero, 0x00020001); } // SND_FILENAME | SND_ASYNC
        catch (Exception e) { Plugin.L.LogWarning("Audio failed: " + e.Message); }
    }

    void Update()
    {
        if (!loaded)
        {
            try { LoadAll(); } catch (Exception e) { Plugin.L.LogError(e.ToString()); }
            return;
        }

        if (playing)
        {
            playTime += Time.unscaledDeltaTime;
            int f = (int)(playTime * Plugin.Fps.Value);
            if (f >= frames.Count)
            {
                playing = false;
                canvasGo.SetActive(false);
            }
            else if (f != current)
            {
                current = f;
                img.texture = frames[f];
            }
            return;
        }

        timer += Time.unscaledDeltaTime;
        if (timer >= 1f)
        {
            timer -= 1f;
            if (UnityEngine.Random.Range(0, Plugin.Denominator.Value) == 0) Play();
        }
    }
}