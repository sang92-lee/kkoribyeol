# index.html·style.jpg·server.js → 배포용 worker.js 하나로 묶기
import base64, json, pathlib
d = pathlib.Path(__file__).parent
src = (d / 'server.js').read_text()
html = (d / 'index.html').read_text()
# Unity로 구운 효과 그림(dev/fx/*.png)은 data URL로 넣어서 worker 하나로 서빙
for f in sorted((d / 'fx').glob('*.png')):
    html = html.replace(f'fx/{f.name}', 'data:image/png;base64,' + base64.b64encode(f.read_bytes()).decode())
html = html.replace('`fx/${k}.png`', 'FXD[k]').replace('url(fx/${name}.png)', 'url(${FXD[name]})')
gs = {f.stem: 'data:image/png;base64,' + base64.b64encode(f.read_bytes()).decode() for f in sorted((d / 'gear_side').glob('*.png'))}
html = html.replace('const GSD = null;', 'const GSD = ' + json.dumps(gs) + ';', 1)
html = html.replace('const FXI = {};', 'const FXD = ' + json.dumps({f.stem: 'data:image/png;base64,' + base64.b64encode(f.read_bytes()).decode() for f in sorted((d / 'fx').glob('*.png'))}) + ';\nconst FXI = {};', 1)
b64 = base64.b64encode((d / 'style.jpg').read_bytes()).decode()
head = 'import { DurableObject } from "cloudflare:workers";\n'
assert src.startswith(head)
out = head + '\n/* 이 파일은 dev/build.py로 만들어요. 고칠 땐 dev 폴더를 고친 뒤 다시 묶기 */\n' + \
      'const INDEX_HTML = ' + json.dumps(html, ensure_ascii=False) + ';\n' + \
      'const STYLE_B64 = "' + b64 + '";\n' + src[len(head):]
import sys
(pathlib.Path(sys.argv[1]) / 'worker.js').write_text(out)
print('ok', len(out))
