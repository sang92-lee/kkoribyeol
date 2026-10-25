using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 꼬리별 Unity 버전 1단계: 입장(코드+닉네임 / 계정) → 상태 받기(6초) → 우리 아이 방.
// 서버는 웹과 같은 Cloudflare 그대로. 화면만 Unity. 웹(dev/index.html)이 기능·수치의 기준
public class Game : MonoBehaviour
{
    public static Game I;
    public Dictionary<string, object> State;
    public Room Room;
    Text title, status, gateMsg;
    GameObject gate;
    float pollT;
    bool polling;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I) return;
        new GameObject("Game").AddComponent<Game>();
    }

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        BuildUi();
        Room = new GameObject("Room").AddComponent<Room>();
        if (string.IsNullOrEmpty(Net.Code)) ShowGate(""); else StartCoroutine(Hello());
    }

    public Dictionary<string, object> MyPet => State.Obj("pets").Obj(Net.Id);

    void Update()
    {
        if (!polling) return;
        pollT -= Time.unscaledDeltaTime;
        if (pollT <= 0) { pollT = 6; StartCoroutine(Refresh()); }   // 서버 무료 한도 때문에 웹과 같은 6초
    }

    IEnumerator Hello()
    {
        string err = null;
        yield return Net.Call("hello", new Dictionary<string, object> { ["id"] = Net.Id, ["nick"] = Net.Nick }, _ => { }, (m, c) => err = c == 401 ? "입장 코드가 달라요" : m);
        if (err != null) { ShowGate(err); yield break; }
        gate.SetActive(false); polling = true; pollT = 0;
    }

    IEnumerator Refresh()
    {
        yield return Net.Call("state?id=" + Net.Id, null, j =>
        {
            State = j;
            var p = MyPet;
            title.text = p != null ? p.Str("name") : "우리 아이";
            if (p == null || !p.ContainsKey("img") || !(p["img"] is bool b && b)) status.text = "웹에서 먼저 우리 아이를 그려 주세요 (사진 → 그림)";
            Room.Apply(j, p);
        }, (m, c) => { if (c == 401) { polling = false; Net.Code = ""; ShowGate("입장 코드가 달라요"); } });
    }

    public void Say(string t) => status.text = t;

    // ---------- 입장 ----------
    void ShowGate(string msg) { gate.SetActive(true); gateMsg.text = msg; }

    void NewStart()
    {
        var code = Net.Ask("입장 코드 (친구한테 받은 코드)", Net.Code); if (string.IsNullOrEmpty(code)) return;
        var nick = Net.Ask("닉네임", Net.Nick); if (string.IsNullOrEmpty(nick)) return;
        Net.Code = code.Trim(); Net.Nick = nick.Trim();
        StartCoroutine(Hello());
    }

    void Login()
    {
        var code = Net.Ask("입장 코드", Net.Code); if (string.IsNullOrEmpty(code)) return;
        var uid = Net.Ask("아이디"); if (string.IsNullOrEmpty(uid)) return;
        var pw = Net.Ask("비밀번호"); if (string.IsNullOrEmpty(pw)) return;
        Net.Code = code.Trim();
        StartCoroutine(Net.Call("login", new Dictionary<string, object> { ["uid"] = uid.Trim(), ["pw"] = pw }, j =>
        {
            Net.Id = j.Str("id"); Net.Nick = j.Str("nick");
            StartCoroutine(Hello());
        }, (m, c) => gateMsg.text = m));
    }

    // ---------- 화면 (코드로 만듦) ----------
    public static readonly Color Ink = new Color32(0x4A, 0x34, 0x28, 255), Cream = new Color32(0xFF, 0xF8, 0xEE, 255), Pink = new Color32(0xF2, 0xA0, 0xB3, 255);
    public static Font Font;

    void BuildUi()
    {
        Font = Resources.Load<Font>("Fonts/NotoSansKR");
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var cv = new GameObject("Ui", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
        var sc = cv.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(750, 1334); sc.matchWidthOrHeight = 0;

        // 위: 이름 / 아래: 지금 하는 일
        title = Label(cv.transform, "꼬리별", 54, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -40), TextAnchor.MiddleLeft);
        status = Label(cv.transform, "", 32, Ink, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 330), new Vector2(-40, 440), TextAnchor.UpperLeft);
        // 돌봄 버튼 (웹 우리 아이 화면과 같은 다섯 개)
        string[] names = { "밥", "물 갈기", "간식", "청소", "이름 부르기" };
        System.Action[] acts = { Feed, Water, Snack, Clean, Call };
        for (int i = 0; i < 5; i++)
        {
            int k = i; float x0 = .03f + i * .19f;
            careSub[i] = Button(cv.transform, names[i], new Vector2(x0, 0), new Vector2(x0 + .18f, 0), Color.white, () => acts[k](), new Vector2(0, 150), new Vector2(0, 300));
        }

        gate = new GameObject("Gate", typeof(RectTransform), typeof(Image));
        gate.transform.SetParent(cv.transform, false);
        var gr = (RectTransform)gate.transform; gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one; gr.offsetMin = gr.offsetMax = Vector2.zero;
        gate.GetComponent<Image>().color = Cream;
        Label(gate.transform, "꼬리별", 96, Ink, new Vector2(0, .62f), new Vector2(1, .8f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Label(gate.transform, "우리 애가 별이 되어 사는 곳 · Unity 버전 (시험)", 30, Ink, new Vector2(0, .56f), new Vector2(1, .62f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Button(gate.transform, "처음이에요 (코드 + 닉네임)", new Vector2(.12f, .42f), new Vector2(.88f, .5f), Pink, NewStart);
        Button(gate.transform, "계정이 있어요 (아이디·비밀번호)", new Vector2(.12f, .32f), new Vector2(.88f, .4f), Color.white, Login);
        gateMsg = Label(gate.transform, "", 30, new Color32(0xD2, 0x47, 0x5B, 255), new Vector2(0, .24f), new Vector2(1, .3f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        gate.SetActive(false);
    }

    static Text Label(Transform p, string t, int size, Color c, Vector2 a0, Vector2 a1, Vector2 o0, Vector2 o1, TextAnchor al)
    {
        var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(p, false);
        var r = (RectTransform)go.transform; r.anchorMin = a0; r.anchorMax = a1; r.offsetMin = o0; r.offsetMax = o1;
        var x = go.AddComponent<Text>(); x.font = Font; x.text = t; x.fontSize = size; x.color = c; x.alignment = al;
        x.horizontalOverflow = HorizontalWrapMode.Wrap; x.verticalOverflow = VerticalWrapMode.Overflow; x.raycastTarget = false;
        return x;
    }

    static Text Button(Transform p, string t, Vector2 a0, Vector2 a1, Color bg, UnityEngine.Events.UnityAction on, Vector2 o0 = default, Vector2 o1 = default)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(p, false);
        var r = (RectTransform)go.transform; r.anchorMin = a0; r.anchorMax = a1; r.offsetMin = o0; r.offsetMax = o1;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => { Net.Buzz(10); on(); });
        Label(go.transform, t, o1.y > 0 ? 30 : 36, Ink, new Vector2(0, o1.y > 0 ? .45f : 0), Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        return Label(go.transform, "", 24, new Color32(0x8C, 0x74, 0x66, 255), Vector2.zero, new Vector2(1, .45f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
    }

    // ---------- 돌봄 (웹 feed/water/toilet 과 같은 규칙, 기록은 폰 안에) ----------
    readonly Text[] careSub = new Text[5];
    float X(string k, float def) => Room.SpotX.TryGetValue(k, out var v) ? v : def;
    string Nm => MyPet.Str("name", "우리 아이");
    void Feed() { var c = Care.Get(); c.food = 4; c.fat = Mathf.Min(1, c.fat + .02f); c.Save(); Room.Pet.DoAt(X("bowl", 41), 30, "eat", 4.5f, "밥이다!"); Say(Nm + "가 밥 먹으러 달려가요"); }
    void Water() { var c = Care.Get(); c.water = 1; c.Save(); Room.Pet.DoAt(X("bottle", 46) - 3, 24, "drink", 3, "꼴깍꼴깍"); Say(Nm + "가 물 마시러 가요"); }
    void Snack() { var c = Care.Get(); c.trust = Mathf.Min(100, c.trust + 1); c.fat = Mathf.Min(1, c.fat + .03f); c.Save(); Room.Pet.DoAt(50, 30, "eat", 4, "해바라기씨다!"); FxLib.Play("sparkle", Room.W(50, 30), .6f, 60); Say(Nm + "가 해바라기씨를 오독오독 먹어요"); }
    void Clean() { var c = Care.Get(); bool dirty = c.poop > 0; c.poop = 0; c.Save(); FxLib.Play("sparkle", Room.W(X("toilet", 65), 16), .7f, 60); Say(dirty ? "화장실을 깨끗하게 치웠어요" : "이미 깨끗해요"); }
    void Call() { Room.Pet.DoAt(Random.Range(35f, 65f), 26, "happy", 1.4f, "불렀어?"); FxLib.Play("heart", Room.W(50, 40), .6f, 60); Say("\"" + Nm + "~\""); }

    void LateUpdate()
    {
        if (State == null) return;
        var c = Care.Get(); int f = Mathf.RoundToInt(c.food);
        careSub[0].text = new string('●', Mathf.Clamp(f, 0, 4)) + new string('○', 4 - Mathf.Clamp(f, 0, 4));
        careSub[1].text = Mathf.RoundToInt(c.water * 100) + "%";
        careSub[2].text = "해바라기씨";
        careSub[3].text = c.poop > 0 ? "똥 " + Mathf.RoundToInt(c.poop) : "깨끗";
        careSub[4].text = "\"" + Nm + "~\"";
    }
}
