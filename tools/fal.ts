// Run one fal generation through its queue, keep the raw output, and log the call.
//
//   node tools/fal.ts <name> <endpoint> <arguments.json>
//   node tools/fal.ts --resume <name>
//
// Every call is paid and needs the owner's approval first. The raw outputs go to raw/
// (gitignored: the only copy), and fal-log.json gets the endpoint, arguments, request id,
// fal's billing headers and the files. Read the exact charge afterwards with
// tools/fal-usage.ts, since some endpoints' own cost fields are placeholders.
//
// A submitted job is saved to raw/pending-<name>.json until its result is logged. If the
// network drops while polling (it did on 2026-11-10, after fal had already billed the job),
// `--resume <name>` collects the finished job instead of paying for it again.

import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { extname, join } from 'node:path';

type Pending = {
  name: string;
  endpoint: string;
  started: string;
  request_id: string;
  status_url: string;
  response_url: string;
  arguments: Record<string, unknown>;
};

const key = process.env.FAL_KEY;
if (!key) throw new Error('FAL_KEY is not set (it lives in ~/.config/env/global.env)');
const headers = {
  Authorization: `Key ${key}`,
  'Content-Type': 'application/json',
  // Only the model asked for may run, never a fallback.
  'x-app-fal-disable-fallback': 'true',
};
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

// Retry network failures (no response at all), never HTTP errors: a 4xx or 5xx is fal's answer.
async function fetchRetry(url: string, init?: RequestInit): Promise<Response> {
  for (let attempt = 1; ; attempt++) {
    try {
      return await fetch(url, init);
    } catch (err) {
      if (attempt >= 8) throw err;
      console.log(`network error (${(err as Error).message}), retry ${attempt} of 7`);
      await sleep(2000 * attempt);
    }
  }
}

const pendingPath = (name: string) => join('raw', `pending-${name}.json`);
mkdirSync('raw', { recursive: true });

let job: Pending;
const argv = process.argv.slice(2);
if (argv[0] === '--resume') {
  const name = argv[1];
  if (!name || !existsSync(pendingPath(name))) throw new Error(`no pending job named ${name} in raw/`);
  job = JSON.parse(readFileSync(pendingPath(name), 'utf8')) as Pending;
  console.log(`resuming ${job.request_id}`);
} else {
  const [name, endpoint, argsPath] = argv;
  if (!name || !endpoint || !argsPath) throw new Error('usage: node tools/fal.ts <name> <endpoint> <arguments.json> | --resume <name>');
  if (existsSync(pendingPath(name))) throw new Error(`${pendingPath(name)} exists: resume it with --resume ${name} rather than paying again`);
  const args = JSON.parse(readFileSync(argsPath, 'utf8')) as Record<string, unknown>;
  const started = new Date().toISOString();
  // No retry on submit: a lost answer could mean a job that runs and bills twice.
  const submit = await fetch(`https://queue.fal.run/${endpoint}`, { method: 'POST', headers, body: JSON.stringify(args) });
  if (!submit.ok) throw new Error(`submit ${submit.status}: ${await submit.text()}`);
  const s = (await submit.json()) as { request_id: string; status_url: string; response_url: string };
  job = { name, endpoint, started, request_id: s.request_id, status_url: s.status_url, response_url: s.response_url, arguments: args };
  writeFileSync(pendingPath(name), `${JSON.stringify(job, null, 1)}\n`);
  console.log(`submitted ${job.request_id}`);
}

for (;;) {
  const res = await fetchRetry(job.status_url, { headers });
  const status = (await res.json()) as { status: string };
  if (status.status === 'COMPLETED') break;
  if (!['IN_QUEUE', 'IN_PROGRESS'].includes(status.status)) throw new Error(`status ${JSON.stringify(status)}`);
  await sleep(5000);
}

const result = await fetchRetry(job.response_url, { headers });
const billing = Object.fromEntries([...result.headers].filter(([h]) => h.startsWith('x-fal')));
const body = (await result.json()) as Record<string, unknown>;
if (!result.ok) throw new Error(`result ${result.status}: ${JSON.stringify(body)}`);

// Download every file URL in the result once, untouched. (Tripo's result names one GLB
// three times, as model_mesh, model_urls.glb and model_urls.pbr_model.)
const files: string[] = [];
const urls: string[] = [];
const walk = (v: unknown): void => {
  if (typeof v === 'string' && /^https:\/\/\S+\.(mp4|webm|png|jpg|jpeg|webp|glb|fbx|obj|zip|mp3|wav)$/i.test(v)) {
    if (!urls.includes(v)) urls.push(v);
  }
  else if (v && typeof v === 'object') Object.values(v).forEach(walk);
};
walk(body);
for (const [i, url] of urls.entries()) {
  const file = join('raw', `${job.name}${urls.length > 1 ? `-${i + 1}` : ''}${extname(new URL(url).pathname)}`);
  const res = await fetchRetry(url);
  if (!res.ok) throw new Error(`download ${res.status}: ${url}`);
  const bytes = new Uint8Array(await res.arrayBuffer());
  writeFileSync(file, bytes);
  files.push(file);
  console.log(`saved ${file} (${bytes.length} bytes)`);
}

const logPath = 'fal-log.json';
const log = existsSync(logPath) ? (JSON.parse(readFileSync(logPath, 'utf8')) as unknown[]) : [];
log.push({
  name: job.name,
  endpoint: job.endpoint,
  started: job.started,
  seconds: Math.round((Date.now() - new Date(job.started).getTime()) / 100) / 10,
  request_id: job.request_id,
  billing,
  arguments: job.arguments,
  result: body,
  files,
});
writeFileSync(logPath, `${JSON.stringify(log, null, 1)}\n`);
rmSync(pendingPath(job.name));
console.log(`logged in ${logPath}`, billing);
