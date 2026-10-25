mergeInto(LibraryManager.library, {
  // 진동 (안드로이드 브라우저만, 아이폰 사파리는 지원 안 함)
  KkVibrate: function (ms) { try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) {} },
  // 한글 입력: Unity 웹 입력칸은 한글 조합이 잘 안 돼서 브라우저 입력창을 씀
  KkPrompt: function (title, def) {
    var v = window.prompt(UTF8ToString(title), UTF8ToString(def));
    if (v == null) v = '\u0000';
    var n = lengthBytesUTF8(v) + 1, p = _malloc(n); stringToUTF8(v, p, n); return p;
  },
  // 웹 버전(같은 주소)과 같은 로그인 정보를 씀: localStorage kk_code / kk_id / kk_nick
  KkLocalGet: function (k) {
    var v = ''; try { v = localStorage.getItem('kk_' + UTF8ToString(k)) || ''; } catch (e) {}
    var n = lengthBytesUTF8(v) + 1, p = _malloc(n); stringToUTF8(v, p, n); return p;
  },
  KkLocalSet: function (k, v) { try { localStorage.setItem('kk_' + UTF8ToString(k), UTF8ToString(v)); } catch (e) {} },
});
