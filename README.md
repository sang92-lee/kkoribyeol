# 꼬리별

우리 애 사진을 꼬리별 그림체로 바꾸고, 10분마다 열리는 갓챠 줄에 친구들이랑 같이 서는 데모

- 사진 → 그림: Gemini 이미지 모델 (키는 Cloudflare 비밀 값에만 있음)
- 줄·순위·뽑기·랭킹: Cloudflare Worker + Durable Object(SQLite)
- 화면: `worker.js` 안에 같이 들어 있음 (원본은 `dev/`)

## 배포 (Cloudflare)
1. Workers & Pages → Create → Import a repository → 이 저장소 선택 → Deploy
2. 만들어진 Worker → Settings → Variables and Secrets
   - `GEMINI_API_KEY` (Secret): 제미나이 키
   - `ACCESS_CODE` (Secret): 친구들한테 알려줄 입장 코드
3. `https://kkoribyeol.<내 계정>.workers.dev` 를 친구들한테 공유

## 고칠 때
`dev/` 안의 파일을 고친 뒤 `python3 dev/build.py .` 로 `worker.js`를 다시 묶어요

## 안전장치
- AI 그림: 한 사람 하루 6번, 전체 하루 30번 (실패하면 횟수 안 깎임)
- 입장 코드 없으면 아무것도 못 씀
