// Read the exact charge of logged fal calls from fal's usage API.
//
//   node tools/fal-usage.ts            # the last call in fal-log.json
//   node tools/fal-usage.ts <name>     # every logged call with that name
//
// Needs FAL_BILLING_KEY (a read-only BILLING-preset key in ~/.config/env/global.env);
// the model key FAL_KEY cannot read usage. fal buckets usage by endpoint and minute, so a
// window can hold other calls to the same endpoint (two mood-still calls a minute apart
// came back as one row of 4 images on 2026-11-10). Each call is therefore priced as its
// own billed units (the x-fal-billable-units header in fal-log.json) at the unit price the
// usage API reports, and the window's total is printed beside it.

import { readFileSync } from 'node:fs';

type Entry = { name: string; endpoint: string; started: string; seconds: number; request_id: string; billing: Record<string, string> };
type Result = { endpoint_id: string; unit: string; quantity: number; unit_price: number; cost_total: number; currency: string };

const key = process.env.FAL_BILLING_KEY;
if (!key) throw new Error('FAL_BILLING_KEY is not set (it lives in ~/.config/env/global.env)');

const log = JSON.parse(readFileSync('fal-log.json', 'utf8')) as Entry[];
const wanted = process.argv[2];
const entries = wanted ? log.filter((e) => e.name === wanted) : log.slice(-1);
if (entries.length === 0) throw new Error(`no logged call named ${wanted}`);

let total = 0;
for (const e of entries) {
  const start = new Date(new Date(e.started).getTime() - 60_000);
  const end = new Date(new Date(e.started).getTime() + (e.seconds + 300) * 1000);
  const url = new URL('https://api.fal.ai/v1/models/usage');
  url.searchParams.set('endpoint_id', e.endpoint);
  url.searchParams.set('start', start.toISOString());
  url.searchParams.set('end', end.toISOString());
  const res = await fetch(url, { headers: { Authorization: `Key ${key}` } });
  if (!res.ok) throw new Error(`usage ${res.status}: ${await res.text()}`);
  const body = (await res.json()) as { time_series: { bucket: string; results: Result[] }[] };
  const results = body.time_series.flatMap((b) => b.results);
  if (results.length === 0) {
    console.log(`${e.name} ${e.endpoint} ${e.request_id}: nothing billed yet`);
    continue;
  }
  const windowUnits = results.reduce((s, r) => s + r.quantity, 0);
  const windowCost = results.reduce((s, r) => s + r.cost_total, 0);
  const prices = [...new Set(results.map((r) => r.unit_price))];
  const own = Number(e.billing?.['x-fal-billable-units']);
  if (prices.length !== 1 || !Number.isFinite(own)) {
    console.log(`${e.name}: window holds ${windowUnits} ${results[0].unit} = ${windowCost.toFixed(4)} USD; cannot split it per call, read the dashboard`);
    continue;
  }
  const cost = own * prices[0];
  const shared = windowUnits > own ? `, window total ${windowUnits} ${results[0].unit} = ${windowCost.toFixed(4)} USD (other calls in the same minutes)` : '';
  console.log(`${e.name} ${e.endpoint} ${e.request_id}: ${own} ${results[0].unit} at ${prices[0]} = ${cost.toFixed(4)} USD${shared}`);
  total += cost;
}
if (entries.length > 1) console.log(`total ${total.toFixed(4)} USD`);
