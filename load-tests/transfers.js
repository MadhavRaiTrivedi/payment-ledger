import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const walletCount = Number(__ENV.WALLETS || 200);
const ratePerSecond = Number(__ENV.RATE || 500);
const duration = __ENV.DURATION || '60s';
const startingBalanceInPaise = 1_000_000_000;

export const options = {
  setupTimeout: '120s',
  scenarios: {
    transfers: {
      executor: 'constant-arrival-rate',
      rate: ratePerSecond,
      timeUnit: '1s',
      duration,
      preAllocatedVUs: 100,
      maxVUs: 400,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:transfer}': ['p(95)<50'],
  },
};

function json(body) {
  return JSON.stringify(body);
}

function headers(token, idempotencyKey) {
  const result = { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
  if (idempotencyKey) {
    result['Idempotency-Key'] = idempotencyKey;
  }
  return result;
}

export function setup() {
  const tokenResponse = http.post(`${baseUrl}/api/auth/dev-token`, json({ role: 'Admin' }), {
    headers: { 'Content-Type': 'application/json' },
  });
  const token = tokenResponse.json('accessToken');

  const wallets = [];
  for (let i = 0; i < walletCount; i++) {
    const wallet = http.post(`${baseUrl}/api/accounts`, json({ ownerId: crypto.randomUUID() }), {
      headers: headers(token),
    });
    const walletId = wallet.json('id');
    http.post(`${baseUrl}/api/deposits`, json({ accountId: walletId, amountInPaise: startingBalanceInPaise }), {
      headers: headers(token, crypto.randomUUID()),
    });
    wallets.push(walletId);
  }

  return { token, wallets };
}

export default function ({ token, wallets }) {
  const source = wallets[Math.floor(Math.random() * wallets.length)];
  let destination = source;
  while (destination === source) {
    destination = wallets[Math.floor(Math.random() * wallets.length)];
  }

  const response = http.post(
    `${baseUrl}/api/transfers`,
    json({ sourceAccountId: source, destinationAccountId: destination, amountInPaise: 100 }),
    { headers: headers(token, crypto.randomUUID()), tags: { name: 'transfer' } },
  );

  check(response, { 'transfer posted': (r) => r.status === 201 });
}
