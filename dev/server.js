import { DurableObject } from "cloudflare:workers";

/* ---------- 게임 규칙 (데모용 숫자) ---------- */
const ROUND_MS = 10 * 60e3;   // 10분마다 캡슐 입고
const OPEN_DELAY = 30e3;      // 판 시작 30초 뒤 첫 손님 뽑기
const SERV = 15e3;            // 한 명당 15초
const MINWAIT = 20e3;         // 줄 서면 최소 20초는 기다림
const WINDOW = 120e3;         // 내 차례 오면 2분 안에 뽑기
const JOIN_CLOSE = 60e3;      // 판 끝나기 1분 전엔 줄 마감
const DRAW_PER_PLAYER = 4;    // AI 그림: 한 사람 하루 4번
const DRAW_PER_DAY = 25;      // AI 그림: 전체 하루 25번 (요금 안전장치)

const NPC_PETS = [
  { id: "npc-kongtteok", name: "콩떡", kind: "hamster", fur: "#F3D9B1" },
  { id: "npc-hodu", name: "호두", kind: "hamster", fur: "#C08A5B" },
  { id: "npc-mochi", name: "모찌", kind: "dog", fur: "#FFF1DE" },
  { id: "npc-nyang", name: "냥냥", kind: "cat", fur: "#9A949E" },
];
const ITEMS = [
  { id: "slide", w: 30 }, { id: "hammock", w: 30 }, { id: "lamp", w: 30 }, { id: "cloud", w: 10 },
];
const GEAR = [
  { id: "ribbon", w: 24 }, { id: "strawhat", w: 20 }, { id: "scarf", w: 20 }, { id: "glasses", w: 16 }, { id: "flower", w: 14 }, { id: "crown", w: 6 },
];
const GOODS = [
  { id: "sticker", w: 40 }, { id: "keyring", w: 35 }, { id: "plush", w: 18 }, { id: "figure", w: 7 },
];

const json = (o, status = 200) => new Response(JSON.stringify(o), { status, headers: { "content-type": "application/json; charset=utf-8", "cache-control": "no-store" } });
function rng(seed) { let s = (seed >>> 0) || 1; return () => { s ^= s << 13; s >>>= 0; s ^= s >>> 17; s ^= s << 5; s >>>= 0; return s / 4294967296; }; }
function hash(str) { let h = 2166136261; for (let i = 0; i < str.length; i++) { h ^= str.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; }
function pickW(list, r) { const sum = list.reduce((a, b) => a + b.w, 0); let x = r * sum; for (const it of list) { if ((x -= it.w) <= 0) return it; } return list[list.length - 1]; }
function toB64(buf) { const u = new Uint8Array(buf); let s = ""; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); }
const validId = id => typeof id === "string" && /^[a-z0-9-]{8,40}$/.test(id);
const kst = t => new Date(t + 9 * 3600e3).toISOString().slice(0, 10);

export default {
  async fetch(req, env) {
    const url = new URL(req.url);
    if (!url.pathname.startsWith("/api/")) {
      if (url.pathname === "/" || url.pathname === "/index.html") return new Response(INDEX_HTML, { headers: { "content-type": "text/html; charset=utf-8", "cache-control": "no-cache" } });
      if (url.pathname === "/style.jpg") return new Response(Uint8Array.from(atob(STYLE_B64), c => c.charCodeAt(0)), { headers: { "content-type": "image/jpeg", "cache-control": "public, max-age=86400" } });
      return new Response("", { status: 404 });
    }
    if (!env.ACCESS_CODE) return json({ error: "setup", message: "ACCESS_CODE가 아직 설정되지 않았어요" }, 503);
    const code = req.headers.get("x-code") || url.searchParams.get("c");
    if (code !== env.ACCESS_CODE) return json({ error: "code", message: "입장 코드가 달라요" }, 401);
    const hub = env.HUB.get(env.HUB.idFromName("main"));
    if (url.pathname === "/api/draw" && req.method === "POST") return draw(req, env, hub);
    return hub.fetch(req);
  },
};

/* ---------- 사진 → 꼬리별 그림체 (Gemini) ---------- */
const KIND_KO = { hamster: "hamster", dog: "puppy", cat: "cat" };
async function draw(req, env, hub) {
  if (!env.GEMINI_API_KEY) return json({ error: "setup", message: "GEMINI_API_KEY가 아직 설정되지 않았어요" }, 503);
  let body; try { body = await req.json(); } catch { return json({ error: "bad", message: "요청이 이상해요" }, 400); }
  const { id, kind, photo } = body || {};
  if (!validId(id) || typeof photo !== "string" || photo.length < 1000 || photo.length > 3_000_000) return json({ error: "bad", message: "사진을 다시 골라 주세요" }, 400);
  const q = await hub.fetch(new Request(new URL("/api/quota", req.url), { method: "POST", body: JSON.stringify({ id }) })).then(r => r.json());
  if (!q.ok) return json({ error: "quota", message: q.message, left: 0 }, 429);

  const style = STYLE_B64;
  const animal = KIND_KO[kind] || "pet";
  const prompt =
    `Redraw the ${animal} in the FIRST image as a cute chibi character illustration, in exactly the art style of the SECOND image. ` +
    `Style: round mochi-like chubby body, soft dark-brown hand-drawn outline, fuzzy fur strokes along the edge, big glossy black dot eyes, pink blush cheeks, tiny pink paws, flat warm pastel colors. ` +
    `Keep this pet's real fur colors, markings, ear shape and hair length from the photo so the owner recognizes it. ` +
    `One character only, full body, centered, facing the viewer, plain cream background (#FDF6EC), no text, no frame, no extra objects.`;
  const imgs = [{ mime: "image/jpeg", data: photo }, { mime: "image/jpeg", data: style }];

  const tries = [
    () => callInteractions(env, prompt, imgs, true),
    () => callInteractions(env, prompt, imgs, false),
    () => callGenerate(env, prompt, imgs, "gemini-3.1-flash-image"),
    () => callGenerate(env, prompt, imgs, "gemini-2.5-flash-image"),
  ];
  const errs = [];
  for (const t of tries) {
    try { const out = await t(); if (out) return json({ image: out.data, mime: out.mime, left: q.left }); errs.push("그림 없는 응답"); }
    catch (e) { errs.push(String(e.message || e).slice(0, 220)); }
  }
  const lastErr = errs.join(" | ");
  await hub.fetch(new Request(new URL("/api/quota", req.url), { method: "POST", body: JSON.stringify({ id, refund: true }) }));
  return json({ error: "ai", message: "그림을 못 받아왔어요. 잠시 뒤 다시 해 주세요 (횟수는 안 깎였어요)", detail: lastErr }, 502);
}
async function callInteractions(env, prompt, imgs, withFormat) {
  const body = {
    model: "gemini-nano-banana-2.1",
    input: [{ type: "text", text: prompt }, ...imgs.map(i => ({ type: "image", mime_type: i.mime, data: i.data }))],
  };
  if (withFormat) body.response_format = { type: "image", mime_type: "image/jpeg", aspect_ratio: "1:1", image_size: "1K" };
  const r = await fetch("https://generativelanguage.googleapis.com/v1beta/interactions", {
    method: "POST", headers: { "content-type": "application/json", "x-goog-api-key": env.GEMINI_API_KEY }, body: JSON.stringify(body),
  });
  if (!r.ok) throw new Error(`interactions ${r.status}: ${(await r.text()).slice(0, 200)}`);
  return findImage(await r.json());
}
async function callGenerate(env, prompt, imgs, model) {
  const body = { contents: [{ parts: [{ text: prompt }, ...imgs.map(i => ({ inline_data: { mime_type: i.mime, data: i.data } }))] }], generationConfig: { responseModalities: ["IMAGE"] } };
  const r = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent`, {
    method: "POST", headers: { "content-type": "application/json", "x-goog-api-key": env.GEMINI_API_KEY }, body: JSON.stringify(body),
  });
  if (!r.ok) throw new Error(`${model} ${r.status}: ${(await r.text()).slice(0, 200)}`);
  return findImage(await r.json());
}
/* 응답 모양이 바뀌어도 그림 데이터를 찾아냄 (마지막 그림이 결과) */
function findImage(o) {
  let found = null;
  const walk = v => {
    if (!v || typeof v !== "object") return;
    if (Array.isArray(v)) return v.forEach(walk);
    const d = v.inlineData || v.inline_data;
    if (d && typeof d.data === "string" && d.data.length > 1000) found = { data: d.data, mime: d.mimeType || d.mime_type || "image/png" };
    else if (v.type === "image" && typeof v.data === "string" && v.data.length > 1000) found = { data: v.data, mime: v.mime_type || v.mimeType || "image/png" };
    for (const k in v) walk(v[k]);
  };
  walk(o);
  return found;
}

/* ---------- 공용 저장소: 플레이어·펫·줄·뽑기 ---------- */
export class Hub extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.sql = ctx.storage.sql;
    this.sql.exec("CREATE TABLE IF NOT EXISTS players(id TEXT PRIMARY KEY, nick TEXT, at INTEGER)");
    this.sql.exec("CREATE TABLE IF NOT EXISTS pets(id TEXT PRIMARY KEY, name TEXT, kind TEXT, img TEXT, at INTEGER)");
    this.sql.exec("CREATE TABLE IF NOT EXISTS queue(r INTEGER, id TEXT, at INTEGER, PRIMARY KEY(r, id))");
    this.sql.exec("CREATE TABLE IF NOT EXISTS pulls(uniq TEXT PRIMARY KEY, who TEXT, r INTEGER, t INTEGER, kind TEXT, a TEXT, b TEXT)");
    this.sql.exec("CREATE TABLE IF NOT EXISTS equip(id TEXT PRIMARY KEY, gear TEXT)");
    this.sql.exec("CREATE TABLE IF NOT EXISTS quota(day TEXT, id TEXT, n INTEGER, PRIMARY KEY(day, id))");
  }
  rows(q, ...a) { return this.sql.exec(q, ...a).toArray(); }

  async fetch(req) {
    const url = new URL(req.url), p = url.pathname, now = Date.now();
    let b = {};
    if (req.method === "POST") { try { b = await req.json(); } catch { return json({ error: "bad", message: "요청이 이상해요" }, 400); } }

    if (p.startsWith("/api/img/")) {
      const r = this.rows("SELECT img FROM pets WHERE id = ?", p.slice(9))[0];
      if (!r || !r.img) return new Response("", { status: 404 });
      const m = /^data:(image\/[a-z]+);base64,(.+)$/.exec(r.img);
      if (!m) return new Response("", { status: 404 });
      const bin = Uint8Array.from(atob(m[2]), c => c.charCodeAt(0));
      return new Response(bin, { headers: { "content-type": m[1], "cache-control": "public, max-age=86400" } });
    }
    if (p === "/api/quota") {
      const day = kst(now);
      if (b.refund) { this.sql.exec("UPDATE quota SET n = MAX(0, n - 1) WHERE day = ? AND id = ?", day, b.id); return json({ ok: true }); }
      const total = this.rows("SELECT COALESCE(SUM(n),0) AS n FROM quota WHERE day = ?", day)[0].n;
      const mine = (this.rows("SELECT n FROM quota WHERE day = ? AND id = ?", day, b.id)[0] || { n: 0 }).n;
      if (mine >= DRAW_PER_PLAYER) return json({ ok: false, message: `오늘 그리기 ${DRAW_PER_PLAYER}번을 다 썼어요. 내일 다시 해 주세요` });
      if (total >= DRAW_PER_DAY) return json({ ok: false, message: "오늘 전체 그리기 횟수가 다 찼어요. 내일 다시 해 주세요" });
      this.sql.exec("INSERT INTO quota(day, id, n) VALUES(?, ?, 1) ON CONFLICT(day, id) DO UPDATE SET n = n + 1", day, b.id);
      return json({ ok: true, left: DRAW_PER_PLAYER - mine - 1 });
    }
    if (p === "/api/hello") {
      if (!validId(b.id)) return json({ error: "bad", message: "다시 들어와 주세요" }, 400);
      const nick = String(b.nick || "").trim().slice(0, 12) || "손님";
      this.sql.exec("INSERT INTO players(id, nick, at) VALUES(?, ?, ?) ON CONFLICT(id) DO UPDATE SET nick = excluded.nick", b.id, nick, now);
      const day = kst(now), used = (this.rows("SELECT n FROM quota WHERE day = ? AND id = ?", day, b.id)[0] || { n: 0 }).n;
      return json({ ok: true, drawsLeft: Math.max(0, DRAW_PER_PLAYER - used) });
    }
    if (p === "/api/pet") {
      if (!this.player(b.id)) return json({ error: "who", message: "다시 들어와 주세요" }, 400);
      const name = String(b.name || "").trim().slice(0, 12);
      if (!name) return json({ error: "bad", message: "이름을 적어 주세요" }, 400);
      if (typeof b.img !== "string" || !/^data:image\/(jpeg|png|webp);base64,/.test(b.img) || b.img.length > 400_000) return json({ error: "bad", message: "그림이 너무 커요" }, 400);
      const kind = ["hamster", "dog", "cat"].includes(b.kind) ? b.kind : "hamster";
      this.sql.exec("INSERT INTO pets(id, name, kind, img, at) VALUES(?, ?, ?, ?, ?) ON CONFLICT(id) DO UPDATE SET name = excluded.name, kind = excluded.kind, img = excluded.img, at = excluded.at", b.id, name, kind, b.img, now);
      return json({ ok: true });
    }
    if (p === "/api/equip") {
      if (!this.player(b.id)) return json({ error: "who", message: "다시 들어와 주세요" }, 400);
      const gear = String(b.gear || "");
      if (gear && !this.rows("SELECT 1 FROM pulls WHERE who = ? AND kind = 'gear' AND a = ?", b.id, gear)[0]) return json({ error: "bad", message: "아직 없는 장비예요" }, 400);
      this.sql.exec("INSERT INTO equip(id, gear) VALUES(?, ?) ON CONFLICT(id) DO UPDATE SET gear = excluded.gear", b.id, gear);
      return json({ ok: true });
    }
    if (p === "/api/join") {
      if (!this.player(b.id)) return json({ error: "who", message: "다시 들어와 주세요" }, 400);
      const R = this.round(now);
      if (R.entries.some(e => e.id === b.id)) return json({ ok: true });
      if (!R.joinable) return json({ error: "closed", message: R.arrived >= R.stock ? "이번 판 캡슐이 이미 다 예약됐어요" : "이번 판 줄은 마감됐어요" }, 409);
      this.sql.exec("INSERT OR IGNORE INTO queue(r, id, at) VALUES(?, ?, ?)", R.r, b.id, now);
      return json({ ok: true });
    }
    if (p === "/api/pull") {
      const R = this.round(now), me = R.entries.find(e => e.id === b.id);
      if (!me) return json({ error: "noline", message: "먼저 줄을 서 주세요" }, 409);
      if (!me.gets) return json({ error: "soldout", message: "앞에서 캡슐이 다 떨어졌어요" }, 409);
      if (now < me.t) return json({ error: "wait", message: "아직 차례가 아니에요" }, 409);
      if (now > me.t + WINDOW) return json({ error: "missed", message: "차례 시간이 지나 버렸어요" }, 409);
      const uniq = `${R.r}:${b.id}`;
      const had = this.rows("SELECT kind, a, b FROM pulls WHERE uniq = ?", uniq)[0];
      if (had) return json({ ok: true, result: had });
      const out = this.roll(Math.random, b.id);
      this.sql.exec("INSERT INTO pulls(uniq, who, r, t, kind, a, b) VALUES(?, ?, ?, ?, ?, ?, ?)", uniq, b.id, R.r, now, out.kind, out.a, out.b);
      return json({ ok: true, result: out });
    }
    if (p === "/api/state") return json(this.state(url.searchParams.get("id") || "", now));
    return json({ error: "none", message: "없는 주소예요" }, 404);
  }

  player(id) { return validId(id) && this.rows("SELECT id FROM players WHERE id = ?", id)[0]; }
  pool() { return [...this.rows("SELECT id FROM pets WHERE img IS NOT NULL").map(r => r.id), ...NPC_PETS.map(n => n.id)]; }
  roll(R, who) {
    const pool = this.pool(), x = R();
    if (x < .38) return { kind: "item", a: pickW(ITEMS, R()).id, b: "" };
    if (x < .62) return { kind: "gear", a: pickW(GEAR, R()).id, b: "" };
    return { kind: "pet", a: pool[Math.floor(R() * pool.length)], b: pickW(GOODS, R()).id };
  }

  /* 한 판의 줄: 손님 햄스터(NPC) + 진짜 친구들, 들어온 순서대로 15초씩 */
  round(now) {
    const r = Math.floor(now / ROUND_MS), rs = r * ROUND_MS, R = rng(Math.imul(r, 2654435761) ^ 0x5bd1e995);
    const stock = 4 + Math.floor(R() * 4), nNpc = 2 + Math.floor(R() * 3);
    const entries = [];
    for (let i = 0; i < nNpc; i++) { const pet = NPC_PETS[Math.floor(R() * NPC_PETS.length)]; entries.push({ id: `npc:${r}:${i}`, pet: pet.id, at: rs + Math.floor(R() * 150e3), npc: true }); }
    for (const q of this.rows("SELECT id, at FROM queue WHERE r = ?", r)) entries.push({ id: q.id, pet: q.id, at: q.at, npc: false });
    entries.sort((a, b) => a.at - b.at || (a.id < b.id ? -1 : 1));
    let prev = rs + OPEN_DELAY - SERV;
    entries.forEach((e, i) => { e.t = Math.max(prev + SERV, e.at + MINWAIT, rs + OPEN_DELAY); prev = e.t; e.gets = i < stock; });
    /* 손님 햄스터는 제 차례에 알아서 뽑아감 (랭킹이 살아있게) */
    for (const e of entries) {
      if (e.npc && e.gets && e.t <= now) {
        const out = this.roll(rng(hash(e.id)), e.pet);
        this.sql.exec("INSERT OR IGNORE INTO pulls(uniq, who, r, t, kind, a, b) VALUES(?, ?, ?, ?, ?, ?, ?)", e.id, e.pet, r, e.t, out.kind, out.a, out.b);
      }
    }
    /* 지금 줄 서면 몇 시에 차례가 오나: 이미 온 사람들 뒤 */
    const arrivedList = entries.filter(e => e.at <= now), re = rs + ROUND_MS;
    const lastT = arrivedList.length ? arrivedList[arrivedList.length - 1].t : rs + OPEN_DELAY - SERV;
    const tNew = Math.max(lastT + SERV, now + MINWAIT, rs + OPEN_DELAY);
    const joinable = arrivedList.length < stock && tNew + WINDOW <= re && now <= re - JOIN_CLOSE;
    return { r, rs, re, stock, entries, arrived: arrivedList.length, joinable };
  }

  state(id, now) {
    const R = this.round(now);
    const pulled = new Set(this.rows("SELECT uniq FROM pulls WHERE r = ?", R.r).map(x => x.uniq));
    const key = e => e.npc ? e.id : `${R.r}:${e.id}`;
    const players = Object.fromEntries(this.rows("SELECT id, nick FROM players").map(x => [x.id, x.nick]));
    const pets = {};
    for (const n of NPC_PETS) pets[n.id] = { name: n.name, kind: n.kind, fur: n.fur, npc: true };
    const gear = Object.fromEntries(this.rows("SELECT id, gear FROM equip").map(x => [x.id, x.gear]));
    for (const x of this.rows("SELECT id, name, kind, at, img IS NOT NULL AS has FROM pets")) pets[x.id] = { name: x.name, kind: x.kind, v: x.at, img: !!x.has, owner: players[x.id] || "", gear: gear[x.id] || "" };

    const line = [];
    R.entries.forEach((e, i) => {
      if (e.at > now) return;
      const done = pulled.has(key(e));
      const inWindow = now >= e.t && now <= e.t + WINDOW;
      let st = "wait";
      if (!e.gets) st = "soldout";
      else if (e.npc) st = e.t <= now ? "done" : "wait";
      else if (done) st = "done";
      else if (inWindow) st = "turn";
      else if (now > e.t + WINDOW) st = "missed";
      if (st === "done" || st === "missed" || (st === "soldout" && e.id !== id)) return;
      line.push({ no: i + 1, id: e.id, pet: e.pet, npc: e.npc, nick: e.npc ? "" : (players[e.id] || "손님"), st, t: e.t });
    });
    const used = R.entries.filter(e => e.gets && e.t <= now).length;
    const meE = R.entries.find(e => e.id === id);
    let me = { st: "none" };
    if (meE) {
      const i = R.entries.indexOf(meE), ahead = line.filter(x => x.no < i + 1).length;
      const done = pulled.has(key(meE));
      me = { no: i + 1, ahead, t: meE.t, until: meE.t + WINDOW, gets: meE.gets,
        st: done ? "done" : !meE.gets ? "soldout" : now < meE.t ? "wait" : now <= meE.t + WINDOW ? "turn" : "missed" };
    }
    const week = now - 7 * 864e5;
    const rank = this.rows("SELECT a AS pet, COUNT(*) AS n FROM pulls WHERE kind = 'pet' AND t > ? GROUP BY a ORDER BY n DESC LIMIT 5", week);
    const mine = id ? this.rows("SELECT kind, a, b, t FROM pulls WHERE who = ? ORDER BY t DESC LIMIT 300", id) : [];
    const recent = this.rows("SELECT who, kind, a, b, t FROM pulls WHERE t <= ? ORDER BY t DESC LIMIT 6", now)
      .map(x => ({ ...x, by: pets[x.who]?.npc ? pets[x.who].name : (players[x.who] || "누군가") }));
    const pulledMe = id ? this.rows("SELECT COUNT(*) AS n FROM pulls WHERE kind = 'pet' AND a = ? AND who != ?", id, id)[0].n : 0;
    const joinable = !meE && R.joinable;
    return {
      now, round: { r: R.r, rs: R.rs, re: R.re, stock: R.stock, left: Math.max(0, R.stock - used), booked: R.arrived, joinable, window: WINDOW },
      line, me, pets, rank, mine, pulledMe, recent,
    };
  }
}
