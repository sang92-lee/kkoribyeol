using System.IO;
using UnityEditor;
using UnityEngine;

// 장비 정면 그림(Assets/Gear/*.png)을 머리·몸통 둘레에 감아서 옆(오른쪽을 보는) 모습으로 굽는다.
// 웹에서 옆모습 포즈일 때 정면 그림 대신 이 그림을 같은 자리에 쓴다. → ../dev/gear_side/*.png
// 실행: Unity -batchmode -quit -projectPath unity -executeMethod GearSide.Run
public static class GearSide
{
    const float Radius = .85f;   // 살짝 둥근 판: 너무 감으면 옆에서 거의 안 보여서 3/4 정도로만 돌아간 느낌을 냄
    const float Yaw = 42f;       // 몸을 돌린 각도 (3/4 옆모습)
    const int Cols = 48, Px = 256;

    [MenuItem("Tools/장비 옆모습 굽기")]
    public static void Run()
    {
        var src = Path.Combine(Application.dataPath, "Gear");
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../dev/gear_side"));
        Directory.CreateDirectory(outDir);
        int n = 0;
        foreach (var f in Directory.GetFiles(src, "*.png"))
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.LoadImage(File.ReadAllBytes(f));
            File.WriteAllBytes(Path.Combine(outDir, Path.GetFileName(f)), Bake(tex, Path.GetFileNameWithoutExtension(f)));
            n++;
        }
        Debug.Log("[GearSide] " + n + " → " + outDir);
    }

    static byte[] Bake(Texture2D tex, string id)
    {
        float h = (float)tex.height / tex.width;
        var root = new GameObject("gear_" + id);

        // 둘레에 감긴 띠: 가로 u는 각도로, 세로 v는 높이로. 모자류는 위로 갈수록 살짝 좁아져 둥근 머리에 얹힌 느낌
        bool hat = id is "santa" or "witch" or "froghat" or "strawberryhat" or "bearhood" or "beret" or "strawhat" or "catears" or "bunny" or "crown" or "tiara" or "halo" or "unicorn" or "sprout" or "chick" or "flower" or "ribbon";
        float span = 2 * Mathf.Asin(.5f / Radius);
        var verts = new Vector3[(Cols + 1) * 2 * 6];
        var uvs = new Vector2[verts.Length];
        int rows = 6;
        verts = new Vector3[(Cols + 1) * (rows + 1)]; uvs = new Vector2[verts.Length];
        for (int r = 0; r <= rows; r++)
            for (int c = 0; c <= Cols; c++)
            {
                float u = (float)c / Cols, v = (float)r / rows;
                float th = (u - .5f) * span;
                float rad = Radius * (hat ? Mathf.Lerp(1f, .8f, v * v) : 1f);
                // 앞(-z)이 카메라 쪽
                verts[r * (Cols + 1) + c] = new Vector3(rad * Mathf.Sin(th), (v - .5f) * h, -rad * Mathf.Cos(th) + Radius); // 앞 중앙을 원점에 두고 돌려서 그림이 제자리에서 3/4로 돌아감
                uvs[r * (Cols + 1) + c] = new Vector2(u, v);
            }
        var tris = new int[Cols * rows * 6]; int t = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                int a = r * (Cols + 1) + c, b = a + 1, d = a + Cols + 1, e = d + 1;
                tris[t++] = a; tris[t++] = b; tris[t++] = d; tris[t++] = b; tris[t++] = e; tris[t++] = d;
            }
        var mesh = new Mesh { vertices = verts, uv = uvs, triangles = tris };
        var mr = root.AddComponent<MeshRenderer>(); root.AddComponent<MeshFilter>().sharedMesh = mesh;
        mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        // 옆모습은 오른쪽을 봄: 앞면이 화면 오른쪽으로 가게 돌림
        root.transform.rotation = Quaternion.Euler(0, -Yaw, 0);

        var camGo = new GameObject("cam"); camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = h / 2; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); cam.enabled = false; cam.aspect = 1 / h;

        int W = Px * 2, H = Mathf.RoundToInt(Px * 2 * h);
        var rt = new RenderTexture(W, H, 16, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var big = new Texture2D(W, H, TextureFormat.RGBA32, false); big.ReadPixels(new Rect(0, 0, W, H), 0, 0); big.Apply();
        RenderTexture.active = null;

        // 2배로 그린 걸 절반으로 (Sprites/Default는 미리 곱한 색이 쌓이므로 알파로 나눔)
        var outT = new Texture2D(Px, H / 2, TextureFormat.RGBA32, false);
        for (int y = 0; y < H / 2; y++)
            for (int x = 0; x < Px; x++)
            {
                Color s = Color.clear; float a = 0;
                for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++) { var c = big.GetPixel(x * 2 + dx, y * 2 + dy); s += c; a += c.a; }
                var o = a > 0 ? s / a : Color.clear; o.a = a / 4; outT.SetPixel(x, y, o);
            }
        outT.Apply();
        var png = outT.EncodeToPNG();
        Object.DestroyImmediate(root); Object.DestroyImmediate(camGo); rt.Release();
        return png;
    }
}
