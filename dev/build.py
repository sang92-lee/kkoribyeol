# index.html·style.jpg·server.js → 배포용 worker.js 하나로 묶기
import base64, json, pathlib
d = pathlib.Path(__file__).parent
src = (d / 'server.js').read_text()
html = (d / 'index.html').read_text()
b64 = base64.b64encode((d / 'style.jpg').read_bytes()).decode()
head = 'import { DurableObject } from "cloudflare:workers";\n'
assert src.startswith(head)
out = head + '\n/* 이 파일은 dev/build.py로 만들어요. 고칠 땐 dev 폴더를 고친 뒤 다시 묶기 */\n' + \
      'const INDEX_HTML = ' + json.dumps(html, ensure_ascii=False) + ';\n' + \
      'const STYLE_B64 = "' + b64 + '";\n' + src[len(head):]
import sys
(pathlib.Path(sys.argv[1]) / 'worker.js').write_text(out)
print('ok', len(out))
