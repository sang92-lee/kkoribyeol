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
        status = Label(cv.transform, "", 34, Ink, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 60), new Vector2(-40, 300), TextAnchor.UpperLeft);

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

    static void Button(Transform p, string t, Vector2 a0, Vector2 a1, Color bg, UnityEngine.Events.UnityAction on)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(p, false);
        var r = (RectTransform)go.transform; r.anchorMin = a0; r.anchorMax = a1; r.offsetMin = r.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => { Net.Buzz(10); on(); });
        Label(go.transform, t, 36, Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
    }
}
