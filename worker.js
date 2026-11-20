// 꼬리별은 쉬는 중. 친구들 기록(Hub 저장소)은 지우지 않고 남겨 둔다.
export class Hub {
  constructor(state, env) { this.state = state; }
  async fetch() { return new Response('쉬는 중', {status: 503}); }
}
export default {
  async fetch() {
    return new Response('꼬리별은 지금 쉬는 중이에요.', {headers: {'content-type': 'text/plain; charset=utf-8'}});
  },
};
