using UnityEngine;

// 효과 파티클 설정 모음. 게임 안에서 실시간으로 쓰고(FxLib.Play), 웹용 그림 굽기(Editor/FxBake)도 같은 설정을 씀
public static class FxLib
{
    static readonly Color Ink = new Color32(0x4A, 0x34, 0x28, 255);
    static readonly Color Pink = new Color32(0xF2, 0xA0, 0xB3, 255), PinkDeep = new Color32(0xE0, 0x7C, 0x95, 255),
        Butter = new Color32(0xFF, 0xE0, 0x8A, 255), Sky = new Color32(0xA9, 0xD4, 0xF2, 255), Cream = new Color32(0xF6, 0xE7, 0xD2, 255),
        Seed = new Color32(0xB9, 0x8A, 0x5A, 255), Mint = new Color32(0xBF, 0xE3, 0xD0, 255), Lilac = new Color32(0xCD, 0xB4, 0xF0, 255);

    // ---------- 효과마다 파티클 설정 ----------

    // 하트: 아래에서 퐁 솟아 살짝 커졌다가 흔들리며 사라짐
    public static void Heart(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Heart01, Pink, true));
        var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.55f, .75f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
        m.startSize = new ParticleSystem.MinMaxCurve(.42f, .62f); m.gravityModifier = -.15f; m.startRotation = new ParticleSystem.MinMaxCurve(-.35f, .35f);
        m.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1, .86f, .9f));
        Burst(ps, 4);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 28; sh.radius = .15f; sh.rotation = new Vector3(-90, 0, 0);
        ps.transform.position = new Vector3(0, -1.2f, 0);
        SizePop(ps, 1.25f);
        var noise = ps.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = 1.4f;
        FadeOut(ps, .6f);
    }

    // 반짝이: 네 갈래 별이 사방으로 튀어나가며 돌고 작아짐
    public static void Sparkle(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Star01, Color.white, true));
        var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .58f); m.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
        m.startSize = new ParticleSystem.MinMaxCurve(.42f, .66f); m.startRotation = new ParticleSystem.MinMaxCurve(0, 1.5f);
        var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Pink, 0), new GradientColorKey(Butter, .5f), new GradientColorKey(Sky, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        Burst(ps, 9);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .05f;
        var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0; lv.dampen = .16f;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(4, 7);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .4f), new Keyframe(.15f, 1.15f), new Keyframe(1, 0)));
    }

    // 먼지: 발 뒤로 크림색 몽글 구름이 뒤로 밀리며 부풀고 흐려짐
    public static void Dust(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Puff01, Cream, true));
        var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .5f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        m.startSize = new ParticleSystem.MinMaxCurve(.35f, .55f); m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
        Burst(ps, 5);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25; sh.radius = .1f; sh.rotation = new Vector3(-15, -90, 0);
        ps.transform.position = new Vector3(.9f, -1.3f, 0);
        var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0; lv.dampen = .12f;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .5f), new Keyframe(1, 1.6f)));
        FadeOut(ps, .4f);
    }

    // 쿨쿨: Z 세 개가 차례로 떠올라 좌우로 살랑이며 커지고 사라짐 (반복용)
    public static void Zzz(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Z01, Lilac, true));
        var m = ps.main; m.startLifetime = 1.9f; m.startSpeed = 0; m.startSize = .5f; m.loop = true; m.duration = 2f; m.prewarm = true;
        m.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        var em = ps.emission; em.rateOverTime = 1.5f;
        var sh = ps.shape; sh.enabled = false;
        ps.transform.position = new Vector3(-.7f, -1.1f, 0);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(1.1f, new AnimationCurve(new Keyframe(0, .2f), new Keyframe(.25f, 1), new Keyframe(.5f, -.2f), new Keyframe(.75f, 1), new Keyframe(1, .2f)));
        vel.y = new ParticleSystem.MinMaxCurve(1.3f, AnimationCurve.Constant(0, 1, 1)); vel.z = new ParticleSystem.MinMaxCurve(0, AnimationCurve.Constant(0, 1, 0));
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .3f), new Keyframe(.3f, .9f), new Keyframe(1, 1.4f)));
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(.25f, new AnimationCurve(new Keyframe(0, -1), new Keyframe(.5f, 1), new Keyframe(1, -1)));
        FadeOut(ps, .7f, .12f);
    }

    // 부스러기: 씨앗 조각이 튀어 바닥에 톡톡 튀고 멈춤
    public static void Crumbs(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Seed01, Seed, true));
        var m = ps.main; m.startLifetime = .66f; m.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        m.startSize = new ParticleSystem.MinMaxCurve(.16f, .26f); m.gravityModifier = 2.2f; m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
        Burst(ps, 6);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 40; sh.radius = .1f; sh.rotation = new Vector3(-90, 0, 0);
        ps.transform.position = new Vector3(0, -.2f, 0);
        var col = ps.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes; col.mode = ParticleSystemCollisionMode.Collision2D;
        var floor = new GameObject("floor").transform; floor.position = new Vector3(0, -1.35f, 0); floor.rotation = Quaternion.identity;
        floor.SetParent(ps.transform.parent, true);
        col.SetPlane(0, floor); col.bounce = .45f; col.dampen = .35f;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-8, 8);
        FadeOut(ps, .8f);
    }

    // 물방울: 튜브 풀장·물 마실 때 퐁 튀는 물방울
    public static void Splash(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Drop01, Sky, true));
        var m = ps.main; m.startLifetime = .55f; m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 4.4f);
        m.startSize = new ParticleSystem.MinMaxCurve(.18f, .3f); m.gravityModifier = 1.8f;
        Burst(ps, 7);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35; sh.radius = .2f; sh.rotation = new Vector3(-90, 0, 0);
        ps.transform.position = new Vector3(0, -1.1f, 0);
        var r = (ParticleSystemRenderer)ps.GetComponent<Renderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .05f; r.lengthScale = 1.2f;
        FadeOut(ps, .7f);
    }

    // 꽃가루: 갓챠·피버 때 위로 팡 터져서 팔랑팔랑 떨어짐
    public static void Confetti(ParticleSystem ps)
    {
        Tex(ps, ShapeTex((x, y) => Box(x, y, .45f, .8f) - .08f, Color.white, true));
        var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.9f, 1.1f); m.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
        m.startSize = new ParticleSystem.MinMaxCurve(.16f, .26f); m.gravityModifier = .7f; m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
        var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Pink, 0), new GradientColorKey(Butter, .33f), new GradientColorKey(Sky, .66f), new GradientColorKey(Mint, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        Burst(ps, 26);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 32; sh.radius = .2f; sh.rotation = new Vector3(-90, 0, 0);
        ps.transform.position = new Vector3(0, -1.3f, 0);
        var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0; lv.dampen = .05f;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-9, 9);
        // 팔랑임: 가로로 납작해졌다 펴짐
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.separateAxes = true;
        sz.x = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 1), new Keyframe(.2f, .2f), new Keyframe(.4f, 1), new Keyframe(.6f, .2f), new Keyframe(.8f, 1), new Keyframe(1, .3f)));
        sz.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 1, 1)); sz.z = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 1, 1));
        FadeOut(ps, .75f);
    }

    // 파동: 콤보·정확히 맞힘 때 퍼지는 동그라미 링 두 겹
    public static void Ring(ParticleSystem ps)
    {
        Tex(ps, ShapeTex((x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - .74f) - .16f, Pink, false));
        var m = ps.main; m.startLifetime = .5f; m.startSpeed = 0; m.startSize = 2.6f;
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 1), new ParticleSystem.Burst(.12f, 1) });
        var sh = ps.shape; sh.enabled = false;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .15f, 0, 3), new Keyframe(1, 1, 0, 0)));
        FadeOut(ps, .35f);
    }

    // 반딧불이: 밤하늘에 둥둥 떠다니며 깜빡임 (반복용)
    public static void Firefly(ParticleSystem ps)
    {
        Tex(ps, Glow());
        var m = ps.main; m.startLifetime = 3f; m.startSpeed = .12f; m.startSize = new ParticleSystem.MinMaxCurve(.22f, .34f); m.loop = true; m.duration = 3f; m.prewarm = true;
        var em = ps.emission; em.rateOverTime = 3.4f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.8f, 2.6f, .1f);
        var noise = ps.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .8f; noise.scrollSpeed = .3f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(.3f, .45f), new GradientAlphaKey(1, .65f), new GradientAlphaKey(.2f, .85f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .6f), new Keyframe(.3f, 1.2f), new Keyframe(.6f, .8f), new Keyframe(1, 1.1f)));
    }

    // 음표: 피아노·냠트박스 때 통통 튀며 올라가는 음표 두 개
    public static void Notes(ParticleSystem ps)
    {
        Tex(ps, ShapeTex(Note01, Lilac, true));
        var m = ps.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.75f, .95f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 1.9f);
        m.startSize = new ParticleSystem.MinMaxCurve(.5f, .7f); m.gravityModifier = -.05f;
        var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Lilac, 0), new GradientColorKey(Pink, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        Burst(ps, 3);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25; sh.radius = .3f; sh.rotation = new Vector3(-90, 0, 0);
        ps.transform.position = new Vector3(0, -1f, 0);
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(.4f, new AnimationCurve(new Keyframe(0, -1), new Keyframe(.25f, 1), new Keyframe(.5f, -1), new Keyframe(.75f, 1), new Keyframe(1, -1)));
        SizePop(ps, 1.2f);
        FadeOut(ps, .65f);
    }

    // 반딧불이 빛: 가운데 노란 점 + 퍼지는 빛무리
    public static Texture2D Glow()
    {
        const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
            {
                float d = Vector2.Distance(new Vector2(i + .5f, j + .5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                float core = Mathf.Clamp01((.22f - d) * 12), halo = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f) * .55f;
                t.SetPixel(i, j, new Color(1, Mathf.Lerp(.93f, 1, core), Mathf.Lerp(.6f, .9f, core), Mathf.Max(core, halo)));
            }
        t.Apply(); return t;
    }

    public static float Note01(float x, float y)
    {
        // 8분음표: 동그란 머리 + 기둥 + 깃발
        float head = Mathf.Sqrt((x + .25f) * (x + .25f) / (.36f * .36f) + (y + .5f) * (y + .5f) / (.28f * .28f)) - 1;
        head *= .28f;
        float stem = Box(x - .08f, y + .05f, .07f, .55f);
        float flag = Box((x - .3f) * .9f + (y - .35f) * .4f, y - .38f, .22f, .1f);
        return Mathf.Min(head, Mathf.Min(stem, flag));
    }

    public static void Burst(ParticleSystem ps, int n)
    {
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, (short)n) });
    }

    public static void SizePop(ParticleSystem ps, float peak)
    {
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .2f), new Keyframe(.18f, peak), new Keyframe(.35f, .95f), new Keyframe(1, .8f)));
    }

    public static void FadeOut(ParticleSystem ps, float from, float fadeIn = 0)
    {
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            fadeIn > 0 ? new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, fadeIn), new GradientAlphaKey(1, from), new GradientAlphaKey(0, 1) }
                       : new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, from), new GradientAlphaKey(0, 1) });
        col.color = g;
    }

    public static void Tex(ParticleSystem ps, Texture2D t)
    {
        var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = t };
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.View;
    }

    // ---------- 모양 텍스처: 웹 그림체처럼 잉크색 테두리 + 안쪽 하이라이트 ----------

    public delegate float Sdf(float x, float y); // 음수 = 안쪽, 좌표 -1..1

    public static Texture2D ShapeTex(Sdf sdf, Color fill, bool outline)
    {
        const int N = 128; float px = 2f / N, line = outline ? .1f : 0;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1;
                float d = sdf(x * 1.1f, y * 1.1f);
                float a = Mathf.Clamp01(.5f - d / px);                 // 바깥선까지
                float inner = Mathf.Clamp01(.5f - (d + line) / px);    // 테두리 안쪽
                var c = Color.Lerp(Ink, fill, inner);
                // 왼쪽 위 하이라이트
                float hl = Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(-.3f, .3f)) / .28f) * inner;
                c = Color.Lerp(c, Color.white, hl * .55f);
                c.a = a; t.SetPixel(i, j, c);
            }
        t.Apply();
        return t;
    }

    public static float Circle(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    public static float Heart01(float x, float y)
    {
        // 동그라미 두 개 + 45도 돌린 네모 = 통통한 하트
        float c = Mathf.Min(Circle(x, y, -.38f, .22f, .44f), Circle(x, y, .38f, .22f, .44f));
        float u = (x + y + .3f) * .7071f, v = (y - x + .3f) * .7071f;
        return Mathf.Min(c, Box(u, v, .5f, .5f) - .04f);
    }

    public static float Star01(float x, float y)
    {
        // 네 갈래 별: |x|^p + |y|^p 의 별 모양(p<1)
        float p = .55f; float v = Mathf.Pow(Mathf.Abs(x), p) + Mathf.Pow(Mathf.Abs(y), p);
        return (v - .92f) * .55f;
    }

    public static float Puff01(float x, float y) =>
        Mathf.Min(Circle(x, y, -.35f, -.15f, .45f), Mathf.Min(Circle(x, y, .3f, -.2f, .5f), Circle(x, y, 0, .25f, .5f)));

    public static float Seed01(float x, float y)
    {
        // 해바라기씨: 한쪽이 뾰족한 타원
        float k = 1 + y * .35f; return Mathf.Sqrt(x * x / (.42f * .42f * k * k) + y * y / (.8f * .8f)) - 1;
    }

    public static float Drop01(float x, float y)
    {
        float c = Circle(x, y, 0, -.25f, .55f);
        float tip = Mathf.Abs(x) * 1.6f + (y - .85f) * .9f; // 위로 뾰족
        return y > -.25f ? Mathf.Max(c, -(.0f) + Mathf.Max(tip, -1)) * .6f + Mathf.Min(0, c) * .4f : c;
    }

    public static float Z01(float x, float y)
    {
        // Z: 위 막대 + 대각선 + 아래 막대
        float top = Box(x, y - .62f, .7f, .16f), bot = Box(x, y + .62f, .7f, .16f);
        float u = (x + y) * .7071f, v = (y - x) * .7071f;
        float diag = Box(v, u, .17f, .82f);
        return Mathf.Min(top, Mathf.Min(bot, diag));
    }

    public static float Box(float x, float y, float hw, float hh)
    {
        float dx = Mathf.Abs(x) - hw, dy = Mathf.Abs(y) - hh;
        return Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) + Mathf.Min(Mathf.Max(dx, dy), 0);
    }

    public static readonly System.Collections.Generic.Dictionary<string, System.Action<ParticleSystem>> All = new()
    {
        ["heart"] = Heart, ["sparkle"] = Sparkle, ["dust"] = Dust, ["zzz"] = Zzz, ["crumbs"] = Crumbs, ["splash"] = Splash,
        ["confetti"] = Confetti, ["ring"] = Ring, ["firefly"] = Firefly, ["notes"] = Notes,
    };

    // 게임 안에서 한 번 터뜨리기: 효과 크기는 Bake 화면(높이 3) 기준이라 scale로 맞춤
    public static ParticleSystem Play(string name, Vector3 pos, float scale, int order = 50)
    {
        var go = new GameObject("fx_" + name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.maxParticles = 200; main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission; em.rateOverTime = 0;
        All[name](ps);
        main.loop = false; main.prewarm = false; main.duration = Mathf.Max(main.duration, 1); // 반복 효과도 한 번만 (계속 쌓이지 않게)
        var child = ps.transform.position; // 설정이 정한 시작 위치(효과 화면 기준)
        go.transform.position = pos + child * scale; go.transform.localScale = Vector3.one * scale;
        ps.GetComponent<ParticleSystemRenderer>().sortingOrder = order;
        ps.Play();
        return ps;
    }
}
