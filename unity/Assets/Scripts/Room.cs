using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 우리 아이 방. 좌표는 웹과 같은 비율: 가로 % (방 전체 = 화면 WF배), 세로 % (방 높이 기준, 바닥 12%)
// 화면에 보이는 방 너비 = 10 유닛, 높이 = 7.5 유닛 (웹 방 4:3)
public class Room : MonoBehaviour
{
    public const float VW = 10, VH = 7.5f, Floor = 12;
    public float WF = 1.3f;
    public Pet Pet;
    public Camera Cam;
    readonly Dictionary<string, SpriteRenderer> props = new();
    public readonly Dictionary<string, float> SpotX = new();
    string built = "";
    float cam, camTo, userCamT;
    SpriteRenderer sky, floor;
    readonly List<SpriteRenderer> stars = new();

    public Vector3 W(float xPct, float bottomPct, float z = 0) => new Vector3(xPct / 100f * VW * WF, bottomPct / 100f * VH, z);
    public float XPct(float worldX) => worldX / (VW * WF) * 100f;

    void Awake()
    {
        // 방 밖(위 이름·아래 글자 자리) 크림색 바탕
        var bg = new GameObject("BgCam").AddComponent<Camera>(); bg.depth = -10; bg.cullingMask = 0; bg.clearFlags = CameraClearFlags.SolidColor; bg.backgroundColor = Game.Cream;
        Cam = new GameObject("Cam").AddComponent<Camera>();
        Cam.orthographic = true; Cam.orthographicSize = VH / 2; Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = Game.Cream;
        Cam.transform.position = new Vector3(VW / 2, VH / 2, -10);
        sky = Quad("sky", Grad(new Color32(0xD9, 0xE8, 0xFA, 255), new Color32(0xF7, 0xE8, 0xEF, 255)), -50);
        floor = Quad("floor", Grad(new Color32(0xF3, 0xDD, 0xC0, 255), new Color32(0xEB, 0xCC, 0xA6, 255)), -49);
        Pet = new GameObject("Pet").AddComponent<Pet>(); Pet.Room = this;
    }

    // 방이 폰 화면 위쪽에 4:3으로 보이게 (아래는 글자 자리)
    void LateUpdate()
    {
        // 화면 비율이 달라도 UI(기준 너비 750)와 같은 자리: 위 130, 좌우 24
        float sw = Screen.width, sh = Screen.height, k = sw / 750f, mx = 24 * k / sw, top = 130 * k / sh, h = Mathf.Min(1, (sw - 48 * k) * .75f / sh);
        Cam.rect = new Rect(mx, 1 - h - top, 1 - mx * 2, h);
        Cam.orthographicSize = VH / 2;
        // 카메라: 손으로 끌면 그쪽, 아니면 펫을 부드럽게 따라감
        float maxCam = (WF - 1) * VW;
        if (Time.time > userCamT) camTo = Mathf.Clamp(Pet.transform.position.x - VW / 2, 0, maxCam);
        cam = Mathf.Lerp(cam, Mathf.Clamp(camTo, 0, maxCam), 1 - Mathf.Exp(-Time.deltaTime * 4));
        Cam.transform.position = new Vector3(cam + VW / 2, VH / 2, -10);
        Pan();
        Night();
    }

    Vector2 downPos; float downCam; bool dragging;
    void Pan()
    {
        if (Input.GetMouseButtonDown(0)) { downPos = Input.mousePosition; downCam = camTo; dragging = false; }
        if (Input.GetMouseButton(0))
        {
            float dx = Input.mousePosition.x - downPos.x;
            if (Mathf.Abs(dx) > 18 && !Pet.Touching) dragging = true;
            if (dragging) { camTo = downCam - dx / Screen.width * VW; userCamT = Time.time + 4; }
        }
        if (Input.GetMouseButtonUp(0) && !dragging && !Pet.Touching)
        {
            // 바닥을 톡 하면 그쪽으로 옴
            var w = Cam.ScreenToWorldPoint(Input.mousePosition);
            if (Cam.pixelRect.Contains(Input.mousePosition) && w.y < VH * .32f) Pet.ComeTo(XPct(w.x));
        }
    }

    void Night()
    {
        float h = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;
        bool night = h >= 20 || h < 6;
        float t = night ? 1 : 0;
        sky.color = Color.Lerp(Color.white, new Color(.33f, .32f, .55f), t);
        floor.color = Color.Lerp(Color.white, new Color(.55f, .5f, .6f), t * .8f);
        foreach (var s in stars) s.color = new Color(1, 1, 1, t * (.6f + .4f * Mathf.Sin(Time.time * 2 + s.transform.position.x)));
        if (night && Time.time > fireT) { fireT = Time.time + 1.4f; FxLib.Play("firefly", W(Random.Range(5f, 95f), Random.Range(45f, 80f)), .9f, -5); }
    }
    float fireT;

    // ---------- 서버 상태 → 방 ----------
    public void Apply(Dictionary<string, object> st, Dictionary<string, object> pet)
    {
        if (pet == null) return;
        int lv = (int)st.Num("roomLv");
        WF = GameData.RoomWF[Mathf.Clamp(lv, 0, 3)];
        var owned = Owned(st, pet, lv);
        var lay = pet.Obj("layout");
        var key = WF + "|" + string.Join(",", owned) + "|" + Json.Write(lay) + "|" + pet.Num("v");
        Pet.Apply(st, pet);
        if (key == built) return;
        built = key;
        foreach (var p in props.Values) Destroy(p.gameObject);
        props.Clear();
        sky.transform.position = new Vector3(VW * WF / 2, VH * .65f, 5); sky.transform.localScale = new Vector3(VW * WF, VH * .7f, 1);
        floor.transform.position = new Vector3(VW * WF / 2, VH * .15f, 4); floor.transform.localScale = new Vector3(VW * WF, VH * .3f, 1);
        foreach (var s in stars) Destroy(s.gameObject);
        stars.Clear();
        // 바닥 윗선(웹: 잉크색 2.5px) + 벽 물방울 무늬 + 톱밥 막대
        var line = Quad("line", Solid(Game.Ink), -45); line.transform.position = W(50, 30, 3.5f); line.transform.localScale = new Vector3(VW * WF, .045f, 1); props["_line"] = line;
        var wall = Quad("wall", WallTex(WF), -48); wall.transform.localScale = new Vector3(VW * WF, VH * .7f, 1);
        wall.transform.position = new Vector3(VW * WF / 2, VH * .65f, 4.5f); wall.color = new Color(1, 1, 1, .35f); props["_wall"] = wall;
        for (int i = 0; i < Mathf.RoundToInt(WF * 2); i++) { var st2 = Sprite("sticks", 14); st2.transform.position = W(8 + i * 45 / WF % 90, 4, 2); st2.sortingOrder = 5; props["_st" + i] = st2; }
        for (int i = 0; i < Mathf.RoundToInt(10 * WF); i++)
        {
            var s = Quad("star", Dot(), -40); s.transform.position = W((i * 37 + 7) % 96, 100 - (i * 23 + 5) % 52 * .7f, 3); s.transform.localScale = Vector3.one * .07f; stars.Add(s);
        }

        float Spot(string k, out float z)
        {
            z = 0;
            if (lay != null && lay.TryGetValue(k, out var v))
            {
                if (v is double d) return (float)d;
                if (v is Dictionary<string, object> o) { z = (float)o.Num("y"); return (float)o.Num("x", 50); }
            }
            return GameData.Spot.TryGetValue(k, out var b) ? b : 50;
        }
        void Prop(string k, string art, float w, float bottom = Floor)
        {
            float x = Spot(k, out var z);
            bool hang = k is "hammock" or "lamp" or "cloud";
            if (hang) z = 0;
            var sr = Sprite(art, w * (1 - z * .25f));
            sr.transform.position = W(x, bottom + z * 13, 1 + z);
            sr.sortingOrder = 20 - Mathf.RoundToInt(z * 16);
            if (z > 0) sr.color = Color.white * (1 - z * .06f) + Color.black;
            props[k] = sr; SpotX[k] = x;
            if (!hang && bottom <= Floor + 1) { var sh = Quad("shadow", Blob(), sr.sortingOrder - 1); sh.transform.position = W(x, bottom + z * 13 + .3f, 1.5f + z); sh.transform.localScale = new Vector3(w / 100f * VW * .85f, .22f, 1); sh.color = new Color(.29f, .2f, .16f, .16f); props[k + "_sh"] = sh; }
        }
        Prop("platform", "platform", 30, 13); Prop("pot", "pot", 15);
        Prop("house", "house", 22);
        Prop("bowl", "bowl", 10); Prop("bottle", "bottle", 6); Prop("toilet", "toilet", 12); Prop("wheel", "wheel", 24);
        foreach (var k in owned)
        {
            var b = k.Split('#')[0];
            float w = GameData.ItemW.TryGetValue(b, out var ww) ? ww : 18;
            Prop(k, "item_" + b, w, b == "cloud" ? 44 : b == "pool" ? 9 : Floor);
        }
    }

    // 웹 ownedItems(): 배치(layout)가 있으면 그 안에서, 없으면 순서대로 방 칸 수만큼
    static List<string> Owned(Dictionary<string, object> st, Dictionary<string, object> pet, int lv)
    {
        var all = new HashSet<string>(); var cnt = new Dictionary<string, int>();
        foreach (var o in st.Arr("mine") ?? new List<object>())
            if (o.Str("kind") == "item") { var a = o.Str("a"); all.Add(a); cnt[a] = Mathf.Min(3, (cnt.TryGetValue(a, out var c) ? c : 0) + 1); }
        var slots = st.Arr("roomSlots"); int n = slots != null && lv < slots.Count ? System.Convert.ToInt32(slots[lv]) : 3;
        var res = new List<string>();
        var lay = pet.Obj("layout");
        if (lay != null)
        {
            foreach (var k in lay.Keys)
            {
                var b = k.Split('#')[0];
                if (!all.Contains(b)) continue;
                if (k.Contains("#") && int.Parse(k.Split('#')[1]) > (cnt.TryGetValue(b, out var c) ? c : 0)) continue;
                if (res.Count < n) res.Add(k);
            }
            return res;
        }
        foreach (var k in GameData.ItemOrder) if (all.Contains(k) && res.Count < n) res.Add(k);
        return res;
    }

    // ---------- 그림 도우미 ----------
    static readonly Dictionary<string, Sprite> cache = new();
    SpriteRenderer Sprite(string art, float widthUnits)
    {
        if (!cache.TryGetValue(art, out var sp))
        {
            var t = Resources.Load<Texture2D>("Art/" + art);
            sp = t ? UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), t.width) : null;
            cache[art] = sp;
        }
        var go = new GameObject(art); go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp;
        go.transform.localScale = Vector3.one * (widthUnits / 100f * VW);
        return sr;
    }

    SpriteRenderer Quad(string name, Texture2D t, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width);
        sr.drawMode = SpriteDrawMode.Simple; sr.sortingOrder = order;
        if (t.height != t.width) { var c = go.AddComponent<FitQuad>(); c.ratio = (float)t.width / t.height; }
        return sr;
    }

    static Texture2D Grad(Color top, Color bottom)
    {
        var t = new Texture2D(32, 32) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < 32; i++) for (int x = 0; x < 32; x++) t.SetPixel(x, i, Color.Lerp(bottom, top, i / 31f));
        t.Apply(); return t;
    }

    static Texture2D Solid(Color c) { var t = new Texture2D(4, 4); var a = new Color[16]; for (int i = 0; i < 16; i++) a[i] = c; t.SetPixels(a); t.Apply(); return t; }
    static Texture2D Blob()
    {
        var t = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)) / 32; t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 2))); }
        t.Apply(); return t;
    }
    // 벽 물방울 무늬 (웹 테마 1번): 0.55유닛마다 분홍 점, 벽 크기만큼 한 장으로
    static Texture2D WallTex(float wf)
    {
        int cols = Mathf.RoundToInt(VW * wf / .55f), rows = Mathf.RoundToInt(VH * .7f / .55f), P = 16;
        var t = new Texture2D(cols * P, rows * P);
        var px = new Color[cols * P * rows * P];
        for (int y = 0; y < rows * P; y++) for (int x = 0; x < cols * P; x++) { float d = Vector2.Distance(new Vector2(x % P + .5f, y % P + .5f), new Vector2(P / 2f, P / 2f)); px[y * cols * P + x] = new Color(.95f, .63f, .7f, Mathf.Clamp01(2.6f - d)); }
        t.SetPixels(px); t.Apply(); return t;
    }

    static Texture2D Dot()
    {
        var t = new Texture2D(16, 16);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(8, 8)); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(7 - d))); }
        t.Apply(); return t;
    }
}

// 가로세로 다른 텍스처도 localScale(가로, 세로)가 실제 크기가 되게
public class FitQuad : MonoBehaviour
{
    public float ratio = 1; Vector3 last;
    void LateUpdate() { var s = transform.localScale; if (s == last) return; s.y = s.y * ratio; transform.localScale = last = s; }
}
