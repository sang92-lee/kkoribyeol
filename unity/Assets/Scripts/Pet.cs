using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 우리 아이: 서버 그림 한 장을 격자 메시에 입혀서 말랑하게(눌림·출렁·기울기) + 살짝 3D로(둥근 몸통에 감아 돌기) 움직임.
// 수식은 웹 jelly()와 같고, 웹은 가로 띠만 휘지만 여기선 꼭짓점마다 휘어서 더 부드러움
public class Pet : MonoBehaviour
{
    public Room Room;
    public bool Touching;
    const int C = 18, Rr = 28;
    const float WPct = 29;                        // 방 화면 너비의 29% (웹과 같음)
    Mesh mesh; MeshRenderer mr; Vector3[] verts; Color[] cols; Vector2[] uvs;
    Texture2D tex; float aspect = 1, vb;           // vb: 그림 아래 여백(발바닥 위치)
    double ver = -1;
    readonly List<(SpriteRenderer sr, float u, float v, float w, string slot)> gears = new();
    string gearKey = "";

    // 위치·상태
    float x = 50, target = 50, speed, dir = 1, stateT, nextT;
    string act = "idle";
    // 말랑 스프링 (웹 J와 같은 이름)
    float sq = 1, sv, lean, lv, wob, wv, dent, dv, pull, pv, ty = .5f, look, lookTo, sniffT, standT, stand, habitT, hopY;
    float yaw, yawStart, turnT = -1, turnFrom = 1, stepT; bool landed = true; int stepN;

    float Wd => WPct / 100f * Room.VW;            // 몸 너비(유닛)
    float Ht => Wd * aspect;

    void Awake()
    {
        mesh = new Mesh(); mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        mr.sortingOrder = 30;
        int n = (C + 1) * (Rr + 1);
        verts = new Vector3[n]; cols = new Color[n]; uvs = new Vector2[n];
        for (int r = 0; r <= Rr; r++) for (int c = 0; c <= C; c++) uvs[r * (C + 1) + c] = new Vector2((float)c / C, (float)r / Rr);
        var tris = new List<int>();
        for (int r = 0; r < Rr; r++) for (int c = 0; c < C; c++) { int a = r * (C + 1) + c, b = a + 1, d = a + C + 1, e = d + 1; tris.AddRange(new[] { a, d, b, b, d, e }); }
        mesh.vertices = verts; mesh.uv = uvs; mesh.colors = cols; mesh.SetTriangles(tris, 0);
        mr.enabled = false;
        // 발밑 그림자 (뛰어오르면 작고 흐려짐)
        var t = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)) / 32; t.SetPixel(x, y, new Color(.29f, .2f, .16f, Mathf.Clamp01((1 - d) * 2) * .22f)); }
        t.Apply();
        shadow = new GameObject("shadow").AddComponent<SpriteRenderer>(); shadow.sprite = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64); shadow.sortingOrder = 29;
    }
    SpriteRenderer shadow;

    public void Apply(Dictionary<string, object> st, Dictionary<string, object> pet)
    {
        double v = pet.Num("v");
        if (v != ver && pet.TryGetValue("img", out var im) && im is bool b && b) { ver = v; StartCoroutine(Net.Image(Net.PetImg(Net.Id, v), SetTex)); }
        var gl = pet.Arr("gears"); var key = gl == null ? "" : string.Join(",", gl);
        if (key != gearKey) { gearKey = key; BuildGears(gl, pet.Obj("gpos")); }
    }

    void SetTex(Texture2D t)
    {
        if (!t) return;
        tex = Cutout(t, out float bottom);
        vb = bottom; aspect = (float)tex.height / tex.width;
        mr.sharedMaterial.mainTexture = tex; mr.enabled = true;
        Game.I.Say(Game.I.MyPet.Str("name") + "가 방에 왔어요. 콕 찌르거나 쓱쓱 문질러 보세요");
    }

    // 웹 cutout(): 네 모서리 색과 비슷한 배경을 가장자리부터 지움 → 투명
    static Texture2D Cutout(Texture2D src, out float bottom)
    {
        int W = src.width, H = src.height; var px = src.GetPixels32();
        Color32 c0 = px[0], c1 = px[W - 1], c2 = px[(H - 1) * W], c3 = px[H * W - 1];
        int br = (c0.r + c1.r + c2.r + c3.r) / 4, bg = (c0.g + c1.g + c2.g + c3.g) / 4, bb = (c0.b + c1.b + c2.b + c3.b) / 4;
        bool Near(Color32 p) => Mathf.Abs(p.r - br) + Mathf.Abs(p.g - bg) + Mathf.Abs(p.b - bb) < 54;
        var seen = new bool[W * H]; var stack = new Stack<int>();
        for (int x = 0; x < W; x++) { stack.Push(x); stack.Push((H - 1) * W + x); }
        for (int y = 0; y < H; y++) { stack.Push(y * W); stack.Push(y * W + W - 1); }
        while (stack.Count > 0)
        {
            int n = stack.Pop(); if (seen[n]) continue; seen[n] = true;
            if (!Near(px[n])) continue;
            px[n].a = 0;
            int x = n % W, y = n / W;
            if (x > 0) stack.Push(n - 1); if (x < W - 1) stack.Push(n + 1); if (y > 0) stack.Push(n - W); if (y < H - 1) stack.Push(n + W);
        }
        int minY = H;
        for (int y = 0; y < H && minY == H; y += 2) for (int x = 0; x < W; x += 2) if (px[y * W + x].a > 0) { minY = y; break; }
        bottom = minY / (float)H;   // 텍스처는 아래부터 위로
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        t.SetPixels32(px); t.Apply();
        return t;
    }

    void BuildGears(List<object> gl, Dictionary<string, object> gpos)
    {
        foreach (var g in gears) Destroy(g.sr.gameObject);
        gears.Clear();
        if (gl == null) return;
        foreach (var o in gl)
        {
            var id = o as string; if (id == null || !GameData.GearPos.TryGetValue(id, out var gp)) continue;
            var t = Resources.Load<Texture2D>("Art/gear_" + id); if (!t) continue;
            float l = gp[0], tp = gp[1], w = gp[2];
            var cp = gpos.Obj(id); if (cp != null) { l = (float)cp.Num("l", l); tp = (float)cp.Num("t", tp); w = (float)cp.Num("w", w); }
            float ga = (float)t.height / t.width;
            var go = new GameObject("gear_" + id); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), t.width);
            var slot = GameData.Slot.TryGetValue(id, out var s) ? s : "head";
            sr.sortingOrder = slot == "body" ? 31 : slot == "face" ? 32 : 33;
            // 웹: left/top/width는 그림 상자 기준 %. 가운데 점을 그림 좌표(u: 왼→오, v: 아래→위)로
            float u = (l + w / 2) / 100f, v = 1 - (tp / 100f + w / 100f * ga / aspect / 2);
            gears.Add((sr, u, v, w / 100f, slot));
        }
    }

    // ---------- 움직임 ----------
    // 어디로 가서 무엇을 함 (밥 먹기·물 마시기 등). 도착하면 act를 secs초 동안
    string pendAct, pendLine; float pendSecs, actUntil;
    public void DoAt(float xp, float spd, string a, float secs, string line)
    {
        pendAct = a; pendSecs = secs; pendLine = line;
        if (Mathf.Abs(xp - x) < .5f) { Arrive(); return; }
        Go(Mathf.Clamp(xp, 4, 96), spd);
    }
    void Arrive()
    {
        if (pendAct == null) return;
        act = pendAct; actUntil = Time.time + pendSecs; if (pendLine != null) Say(pendLine);
        pendAct = null;
    }
    public void ComeTo(float xp) { if (act == "sleep") return; Say(Random.value < .5f ? "왔어!" : "…여기?"); Go(Mathf.Clamp(xp, 6, 94), 14); }
    void Go(float to, float spd) { target = to; speed = spd; act = "walk"; }
    void Say(string t) => Game.I.Say(Game.I.MyPet.Str("name") + ": " + t);

    void Update()
    {
        if (!mr.enabled) return;
        float dt = Mathf.Min(Time.deltaTime, .05f), now = Time.time;
        Decide(now);
        bool moving = act == "walk", run = moving && speed > 20;
        if (moving)
        {
            float d = target - x, step = speed * dt * Room.WF / 1.3f;
            if (Mathf.Abs(d) <= step) { x = target; act = "idle"; nextT = now + Random.Range(1.5f, 4f); sv -= 1.4f; Arrive(); }
            else { x += Mathf.Sign(d) * step; SetDir(Mathf.Sign(d), now); }
        }
        // 먹기: 오물오물 눌림 + 부스러기 / 마시기: 꼴깍꼴깍 / 신남: 통통
        if (act == "eat" || act == "drink" || act == "happy")
        {
            float per = act == "eat" ? .26f : act == "drink" ? .42f : .3f; int f = Mathf.FloorToInt(now / per);
            if (f != chewN) { chewN = f; sv -= act == "happy" ? 2.2f : act == "eat" ? .8f : .5f; if (act == "eat" && f % 3 == 0) FxLib.Play("crumbs", transform.position + new Vector3(dir * Wd * .1f, Ht * .25f, 0), .4f, 35); }
            if (now > actUntil) { act = "idle"; nextT = now + 2; if (Random.value < .5f) Say("배불러~"); }
        }
        Springs(dt, now, moving, run);
        transform.position = Room.W(x, Room.Floor, 0) + Vector3.up * hopY;
        shadow.enabled = mr.enabled; shadow.transform.position = Room.W(x, Room.Floor, 0) + Vector3.up * .05f;
        float ss = 1 - Mathf.Clamp01(hopY / (Ht * .3f)) * .4f; shadow.transform.localScale = new Vector3(Wd * .7f * ss, Wd * .13f * ss, 1);
        transform.localScale = new Vector3(dirShown, 1, 1);
        Deform();
        Touch(now);
    }

    void Decide(float now)
    {
        if (act != "idle" || now < nextT || pendAct != null) return;
        float r = Random.value;
        if (r < .55f) Go(Random.Range(8f, 92f), 10);
        else if (r < .75f) Go(Random.Range(8f, 92f), 26);
        else nextT = now + Random.Range(2f, 4f);
        if (act == "walk" && Random.value < .3f) Say(new[] { "킁킁", "어디 가볼까", "우다다!", "냠냠 생각" }[Random.Range(0, 4)]);
    }

    // 방향 전환: 폴짝 뛰며 새 쪽으로 32도 돌고, 그 순간 좌우를 바꿔 반대편 32도에서 이어 돎 (두 모습이 같아서 안 보임)
    void SetDir(float d, float now)
    {
        if (d == 0 || d == dir || now - turnT < .34f) return;
        turnFrom = dir; dir = d; turnT = now; yawStart = yaw; sv += 1.2f; landed = false;
    }

    void Springs(float dt, float now, bool moving, bool run)
    {
        // 걸음: 크기 대신 포물선으로 통통
        float per = run ? .095f : .15f;
        if (moving) { int f = Mathf.FloorToInt(now / per); if (f != stepN) { stepN = f; sv -= run ? 1.05f : .8f; } float ph = (now % per) / per; hopY = (run ? .09f : .03f) * Ht * Mathf.Sin(Mathf.PI * ph); }
        else hopY *= .8f;
        float tk = (now - turnT) / .34f;
        if (tk < 1) hopY = Mathf.Max(hopY, .06f * Ht * Mathf.Sin(Mathf.PI * tk));
        // 3D 돌기
        float yawBase = moving ? (run ? 24 : 18) : 0;
        if (tk < 1)
        {
            float e(float t) => t * t * (3 - 2 * t);
            if (tk < .5f) yaw = yawStart + (-32 - yawStart) * e(tk * 2);
            else yaw = 32 + (yawBase - 32) * e((tk - .5f) * 2);
            if (tk < .5f) { dirShown = turnFrom; } else { if (dirShown != dir) wv -= dir * 3; dirShown = dir; }
        }
        else { dirShown = dir; yaw += (yawBase - yaw) * Mathf.Min(1, dt * 8); if (!landed) { landed = true; sv -= 1.5f; FxLib.Play("dust", transform.position - Vector3.right * dir * Wd * .3f, .5f, 35); } }
        // 버릇 (가만히 있을 때)
        bool idle = act == "idle";
        if (idle && now > habitT)
        {
            float r = Random.value; habitT = now + Random.Range(1.4f, 3.2f);
            if (r < .38f) sniffT = now + Random.Range(.5f, .9f);
            else if (r < .6f) { standT = now + Random.Range(.9f, 1.6f); sv += 1.6f; }
            else lookTo = (Random.value < .5f ? -1 : 1) * Random.Range(.5f, 1f);
        }
        if (!idle) { sniffT = 0; standT = 0; lookTo = 0; }
        bool standing = now < standT;
        bool was = stand > .5f;
        stand += ((standing ? 1 : 0) - stand) * Mathf.Min(1, dt * (standing ? 9 : 14));
        if (was && stand < .5f) sv -= 1.4f;
        look += (lookTo - look) * Mathf.Min(1, dt * 10); if (Random.value < dt * .4f) lookTo = 0;
        float br = .014f * Mathf.Sin(now / .42f);
        float tLean = moving ? Mathf.Min(1, speed / 30) * .7f : 0;
        lv += (120 * (tLean - lean) - 13 * lv) * dt; lean += lv * dt;
        sv += (300 * (1 + br + stand * .14f - sq) - 9 * sv) * dt; sq += sv * dt; sq = Mathf.Clamp(sq, .7f, 1.35f);
        wv += (190 * (0 - wob) - 5.5f * wv) * dt; wob += wv * dt;
        dv += (160 * (0 - dent) - 11 * dv) * dt; dent += dv * dt;
        pv += (90 * (0 - pull) - 9 * pv) * dt; pull += pv * dt;
    }
    float dirShown = 1; int chewN;

    // 그림 좌표(u,v) → 지금 모양에서의 위치. 장비도 같은 함수로 붙임
    Vector3 Map(float u, float v, out float shade, out float wscale)
    {
        float yN = 1 - v, w = Wd, h = Ht;
        float up = Mathf.Pow(1 - yN, 1.5f), near = Mathf.Exp(-Mathf.Pow((yN - ty) / .16f, 2));
        float ws = 1 + (1 - sq) * .6f;
        float bulge = (1 + (ws - 1) * (.6f + .4f * Mathf.Sin(Mathf.PI * yN)) - dent * .16f * near + dent * .05f * (1 - near)) * (1 - stand * .07f);
        float dx = (lean * .22f + wob * .06f) * w * up + pull * w * .12f * near + look * w * .05f * up * up;
        float phi = yaw * Mathf.Deg2Rad, th = Mathf.Asin(Mathf.Clamp((u - .5f) * 2, -1, 1)), a = Mathf.Clamp(th + phi, -Mathf.PI / 2, Mathf.PI / 2);
        shade = Mathf.Lerp(1, .78f, Mathf.Clamp01(-Mathf.Sin(a) * Mathf.Sign(phi)) * Mathf.Abs(Mathf.Sin(phi)) * 2);
        wscale = bulge * Mathf.Max(.3f, Mathf.Cos(a)) / Mathf.Max(.3f, Mathf.Cos(th));
        float y = (v - vb) * h * sq - Mathf.Sin(sniffT > Time.time ? Time.time / .028f : 0) * h * .012f * Mathf.Max(0, 1 - yN * 2.2f);
        return new Vector3(Mathf.Sin(a) * w / 2 * bulge + dx, y, 0);
    }

    void Deform()
    {
        for (int r = 0; r <= Rr; r++)
            for (int c = 0; c <= C; c++)
            {
                int i = r * (C + 1) + c;
                verts[i] = Map(uvs[i].x, uvs[i].y, out float s, out _);
                cols[i] = new Color(s, s, s, 1);
            }
        mesh.vertices = verts; mesh.colors = cols; mesh.RecalculateBounds();
        foreach (var g in gears)
        {
            var p = Map(g.u, g.v, out _, out float ws);
            g.sr.transform.localPosition = p + Vector3.back * .01f;
            float sz = g.w * Wd;
            g.sr.transform.localScale = new Vector3(sz * ws, sz * sq, 1);
            g.sr.transform.localRotation = Quaternion.Euler(0, 0, -(lean * 12 + wob * 4 + look * 6));
        }
    }

    // ---------- 손가락: 콕 찌르기 / 쓱쓱 문지르기 ----------
    Vector3 last; float strokeDist;
    void Touch(float now)
    {
        var cam = Room.Cam;
        var wp = cam.ScreenToWorldPoint(Input.mousePosition); wp.z = 0;
        var lp = wp - transform.position;
        bool over = Mathf.Abs(lp.x) < Wd * .42f && lp.y > 0 && lp.y < Ht * (1 - vb) * .95f;
        if (Input.GetMouseButtonDown(0) && over && cam.pixelRect.Contains(Input.mousePosition)) { Touching = true; last = wp; strokeDist = 0; }
        if (Touching && Input.GetMouseButton(0))
        {
            strokeDist += Vector3.Distance(wp, last);
            if (strokeDist > Wd * .35f) { strokeDist = 0; Stroke(wp, wp.x - last.x); }
            last = wp;
        }
        if (Touching && Input.GetMouseButtonUp(0)) { if (!stroked) Poke(wp); stroked = false; Touching = false; }
    }
    bool stroked;

    void TouchAt(Vector3 wp, float kick) { ty = Mathf.Clamp01(1 - (wp.y - transform.position.y) / Ht); dv += kick; }

    void Poke(Vector3 wp)
    {
        sv -= 2.6f; wv += (Random.value < .5f ? -1 : 1) * 4; TouchAt(wp, 9);
        Net.Buzz(22);
        FxLib.Play("heart", wp + Vector3.up * .3f, .55f, 60); FxLib.Play("sparkle", wp, .5f, 61);
        Say(new[] { "냠냠…", "해씨 줘!", "볼 빵빵", "쳇바퀴 가자", "킁킁" }[Random.Range(0, 5)]);
        if (act == "idle") nextT = Time.time + 2;
    }

    void Stroke(Vector3 wp, float dx)
    {
        stroked = true;
        sv -= 2.2f; pv += Mathf.Sign(dx == 0 ? 1 : dx) * 6 * dir; TouchAt(wp, 4);
        Net.Buzz(12);
        FxLib.Play("heart", wp + Vector3.up * .3f, .45f, 60);
        if (Random.value < .3f) Say(new[] { "좋아…", "쪼끔 더", "눈 사르르" }[Random.Range(0, 3)]);
        if (act == "walk") act = "idle";
        nextT = Time.time + 2.5f;
    }
}
