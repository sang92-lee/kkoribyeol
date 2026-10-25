using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 서버 호출 + 저장 + 웹 브라우저 기능. 웹(dev/index.html)의 api()와 같은 규칙: x-code 헤더 + JSON.
// Unity 웹은 같은 주소(/play)에서 열려서 웹 버전과 로그인·저장소를 같이 씀
public static class Net
{
    const string Remote = "https://kkoribyeol.dltkdrndl92.workers.dev";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void KkVibrate(int ms);
    [DllImport("__Internal")] static extern string KkPrompt(string title, string def);
    [DllImport("__Internal")] static extern string KkLocalGet(string k);
    [DllImport("__Internal")] static extern void KkLocalSet(string k, string v);
    static string Base => new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority);
#else
    static void KkVibrate(int ms) { }
    static string KkPrompt(string title, string def) => def;
    static string KkLocalGet(string k) => PlayerPrefs.GetString("kk_" + k, "");
    static void KkLocalSet(string k, string v) { PlayerPrefs.SetString("kk_" + k, v); PlayerPrefs.Save(); }
    static string Base => Remote;
#endif

    public static string Get(string k) => KkLocalGet(k);
    public static void Set(string k, string v) => KkLocalSet(k, v);
    public static void Buzz(int ms) => KkVibrate(ms);
    public static string Ask(string title, string def = "") { var v = KkPrompt(title, def); return v == "\0" ? null : v; }

    public static string Code { get => Get("code"); set => Set("code", value); }
    public static string Nick { get => Get("nick"); set => Set("nick", value); }
    public static string Id
    {
        get
        {
            var id = Get("id");
            if (string.IsNullOrEmpty(id)) { id = Guid.NewGuid().ToString("N").Substring(0, 24); Set("id", id); }
            return id;
        }
        set => Set("id", value);
    }

    public static IEnumerator Call(string path, Dictionary<string, object> body, Action<Dictionary<string, object>> ok, Action<string, long> fail)
    {
        using var req = new UnityWebRequest(Base + "/api/" + path, body == null ? "GET" : "POST");
        if (body != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Write(body)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("content-type", "application/json");
        req.SetRequestHeader("x-code", Code);
        req.timeout = 20;
        yield return req.SendWebRequest();
        Dictionary<string, object> j = null;
        try { j = Json.Parse(req.downloadHandler.text) as Dictionary<string, object>; } catch { }
        if (req.result == UnityWebRequest.Result.Success && j != null) ok?.Invoke(j);
        else fail?.Invoke(j.Str("message", req.responseCode == 0 ? "인터넷 연결을 확인해 주세요" : "잠시 뒤 다시 해 주세요"), req.responseCode);
    }

    public static IEnumerator Image(string url, Action<Texture2D> done)
    {
        using var req = UnityWebRequest.Get(Base + url);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { done(null); yield break; }
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        done(t.LoadImage(req.downloadHandler.data) ? t : null);
    }

    public static string PetImg(string id, double v) => $"/api/img/{UnityWebRequest.EscapeURL(id)}?v={v.ToString(CultureInfo.InvariantCulture)}&c={UnityWebRequest.EscapeURL(Code)}";
}

// 서버 응답 JSON 읽기·쓰기 (state는 펫 id가 키인 사전이라 JsonUtility로는 못 읽음)
public static class Json
{
    public static object Parse(string s) { int i = 0; return Val(s, ref i); }
    static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
    static object Val(string s, ref int i)
    {
        Ws(s, ref i); if (i >= s.Length) return null;
        char c = s[i];
        if (c == '{')
        {
            var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true) { Ws(s, ref i); var k = Txt(s, ref i); Ws(s, ref i); i++; d[k] = Val(s, ref i); Ws(s, ref i); if (s[i++] == '}') return d; }
        }
        if (c == '[')
        {
            var l = new List<object>(); i++; Ws(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i++] == ']') return l; }
        }
        if (c == '"') return Txt(s, ref i);
        if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
        int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        double.TryParse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out var n); return n;
    }
    static string Txt(string s, ref int i)
    {
        var sb = new StringBuilder(); i++;
        while (s[i] != '"')
        {
            char c = s[i++]; if (c != '\\') { sb.Append(c); continue; }
            char e = s[i++];
            if (e == 'u') { sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber)); i += 4; }
            else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e);
        }
        i++; return sb.ToString();
    }
    public static string Write(object o)
    {
        switch (o)
        {
            case null: return "null";
            case string s:
                var sb = new StringBuilder("\"");
                foreach (var c in s) { if (c == '"' || c == '\\') sb.Append('\\').Append(c); else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); }
                return sb.Append('"').ToString();
            case bool b: return b ? "true" : "false";
            case IDictionary<string, object> d: { var parts = new List<string>(); foreach (var kv in d) parts.Add(Write(kv.Key) + ":" + Write(kv.Value)); return "{" + string.Join(",", parts) + "}"; }
            default: return Convert.ToString(o, CultureInfo.InvariantCulture);
        }
    }
    public static Dictionary<string, object> Obj(this object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v as Dictionary<string, object> : null;
    public static List<object> Arr(this object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v as List<object> : null;
    public static string Str(this object o, string k, string def = "") => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : def;
    public static double Num(this object o, string k, double def = 0) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) && v is double n ? n : def;
}
