#!/usr/bin/env node
/**
 * Manual stress: 100 concurrent POST /api/bookings for one slot (capacity 10).
 * Uses 100 distinct user_id values (idempotency is per user+slot).
 *
 * Prerequisites: API + Postgres with booking schema; slot row must exist with matching capacity.
 *
 *   BASE_URL=http://localhost:5001 SLOT_ID=<uuid> node scripts/stress-booking.mjs
 *
 * Optional: USERNAME PASSWORD CONCURRENCY CAPACITY
 */

import { randomBytes } from 'node:crypto';

const baseUrl = (process.env.BASE_URL ?? 'http://localhost:5001').replace(/\/$/, '');
const slotId = process.env.SLOT_ID;
const username = process.env.USERNAME ?? 'rootadmin';
const password = process.env.PASSWORD ?? 'guardianOP';
const concurrency = Number(process.env.CONCURRENCY ?? 100);
const expectedCapacity = Number(process.env.CAPACITY ?? 10);

if (!slotId) {
  console.error('Set SLOT_ID to an existing time_slots.id (UUID).');
  process.exit(1);
}

function randomUuid() {
  if (globalThis.crypto?.randomUUID) {
    return crypto.randomUUID();
  }
  const b = randomBytes(16);
  b[6] = (b[6] & 0x0f) | 0x40;
  b[8] = (b[8] & 0x3f) | 0x80;
  const h = b.toString('hex');
  return `${h.slice(0, 8)}-${h.slice(8, 12)}-${h.slice(12, 16)}-${h.slice(16, 20)}-${h.slice(20)}`;
}

async function login() {
  const res = await fetch(`${baseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({ username, password }),
  });
  if (!res.ok) {
    const t = await res.text();
    throw new Error(`Login failed ${res.status}: ${t}`);
  }
  const data = await res.json();
  if (!data.token) throw new Error('Login response missing token');
  return data.token;
}

async function main() {
  const token = await login();
  const userIds = Array.from({ length: concurrency }, () => randomUuid());

  const started = Date.now();
  const tasks = userIds.map((user_id) =>
    fetch(`${baseUrl}/api/bookings`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ user_id, slot_id: slotId }),
    }),
  );

  const responses = await Promise.all(tasks);
  const elapsed = Date.now() - started;

  let created = 0;
  let ok = 0;
  let conflict = 0;
  let other = 0;

  for (const r of responses) {
    if (r.status === 201) created++;
    else if (r.status === 200) ok++;
    else if (r.status === 409) conflict++;
    else other++;
  }

  console.log(JSON.stringify({ elapsedMs: elapsed, created, okReplay: ok, conflict, other }, null, 2));

  if (other > 0) {
    for (const r of responses) {
      if (r.status !== 201 && r.status !== 200 && r.status !== 409) {
        console.error(await r.text());
      }
    }
    process.exit(1);
  }

  if (created !== expectedCapacity) {
    console.error(`Expected ${expectedCapacity} Created (201), got ${created}`);
    process.exit(1);
  }
  if (conflict !== concurrency - expectedCapacity) {
    console.error(`Expected ${concurrency - expectedCapacity} Conflict (409), got ${conflict}`);
    process.exit(1);
  }

  console.log('Stress assertions passed: no overbooking by HTTP status distribution.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
