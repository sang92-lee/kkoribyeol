using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 웹(dev/index.html)에 넣을 효과 그림을 Unity 파티클로 만들어 가로 한 줄 스프라이트 시트(PNG)로 굽는다.
// 실행: Unity -batchmode -quit -projectPath unity -executeMethod FxBake.Run  → ../dev/fx/*.png
public static class FxBake
{
    const int Size = 160;              // 한 칸 크기(px)
    [MenuItem("Tools/효과 굽기")]
    public static void Run()
    {
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../dev/fx"));
        Directory.CreateDirectory(outDir);
        var log = new List<string>();

        Bake(outDir, "heart", 18, 24, FxLib.Heart, log);
        Bake(outDir, "sparkle", 14, 24, FxLib.Sparkle, log);
        Bake(outDir, "dust", 12, 24, FxLib.Dust, log);
        Bake(outDir, "zzz", 30, 15, FxLib.Zzz, log);
        Bake(outDir, "crumbs", 16, 24, FxLib.Crumbs, log);
        Bake(outDir, "splash", 14, 24, FxLib.Splash, log);
        Bake(outDir, "confetti", 24, 24, FxLib.Confetti, log);
        Bake(outDir, "ring", 12, 24, FxLib.Ring, log);
        Bake(outDir, "firefly", 48, 16, FxLib.Firefly, log);
        Bake(outDir, "notes", 20, 20, FxLib.Notes, log);

        File.WriteAllText(Path.Combine(outDir, "fx.txt"), string.Join("\n", log));
        Debug.Log("[FxBake] " + string.Join(" | ", log));
    }

    // ---------- 굽기 ----------

    static void Bake(string outDir, string name, int frames, int fps, System.Action<ParticleSystem> setup, List<string> log)
    {
        var root = new GameObject("fx_" + name);
        var camGo = new GameObject("cam"); camGo.transform.SetParent(root.transform);
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 1.5f; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); cam.enabled = false; cam.allowHDR = false; cam.allowMSAA = false;

        var psGo = new GameObject("ps"); psGo.transform.SetParent(root.transform);
        var ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local; main.maxParticles = 200;
        ps.useAutoRandomSeed = false; ps.randomSeed = (uint)name.GetHashCode();
        var em = ps.emission; em.rateOverTime = 0;
        setup(ps);

        // 2배로 그려서 줄이면 테두리가 매끈함
        int S = Size * 2;
        var rt = new RenderTexture(S, S, 16, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        cam.targetTexture = rt;
        var sheet = new Texture2D(Size * frames, Size, TextureFormat.RGBA32, false);
        var frame = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int f = 0; f < frames; f++)
        {
            if (f == 0) ps.Simulate(.5f / fps, true, true, true); else ps.Simulate(1f / fps, true, false, true);
            cam.Render();
            RenderTexture.active = rt;
            frame.ReadPixels(new Rect(0, 0, S, S), 0, 0); frame.Apply();
            RenderTexture.active = null;
            // 2×2 평균으로 축소 (투명 가장자리 색 번짐 막으려고 알파 가중)
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Color sum = Color.clear; float a = 0;
                    for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++) { var c = frame.GetPixel(x * 2 + dx, y * 2 + dy); sum += c; a += c.a; }
                    // 화면에는 미리 곱한 색(premultiplied)이 쌓이므로 알파로 나눠서 원래 색을 되돌림
                    var o = a > 0 ? sum / a : Color.clear; o.a = Mathf.Clamp01(a / 4);
                    sheet.SetPixel(f * Size + x, y, o);
                }
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(outDir, name + ".png"), sheet.EncodeToPNG());
        log.Add($"{name}: {frames}f {fps}fps {Size}px");
        Object.DestroyImmediate(root); rt.Release();
    }

}
