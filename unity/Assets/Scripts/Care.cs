using UnityEngine;

// 돌봄 기록 (밥·물·화장실·친해짐·통통함). 웹과 같은 저장 칸(localStorage care_<id>)·같은 형식이라 웹에서 준 밥이 여기서도 보임
public class Care
{
    public float food = 3, water = .8f, poop, trust = 25, fat = .35f; public double at;
    static Care cur;

    public static Care Get()
    {
        if (cur != null) { Decay(cur); return cur; }
        Care c = null;
        try { var j = Json.Parse(Net.Get("care_" + Net.Id)) as System.Collections.Generic.Dictionary<string, object>; if (j != null) c = new Care { food = (float)j.Num("food", 3), water = (float)j.Num("water", .8), poop = (float)j.Num("poop"), trust = (float)j.Num("trust", 25), fat = (float)j.Num("fat", .35), at = j.Num("at", Now) }; } catch { }
        cur = c ?? new Care { at = Now };
        Decay(cur); return cur;
    }

    static double Now => (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds;

    // 웹 care(): 시간이 지나면 배고프고 목마름
    static void Decay(Care c)
    {
        float h = (float)((Now - c.at) / 3.6e6);
        c.food = Mathf.Max(0, c.food - h * .3f); c.fat = Mathf.Max(0, c.fat - h * .01f); c.water = Mathf.Max(0, c.water - h * .07f); c.at = Now;
    }

    public void Save()
    {
        at = Now;
        Net.Set("care_" + Net.Id, Json.Write(new System.Collections.Generic.Dictionary<string, object> { ["food"] = food, ["water"] = water, ["poop"] = poop, ["trust"] = trust, ["fat"] = fat, ["at"] = at }));
    }
}
