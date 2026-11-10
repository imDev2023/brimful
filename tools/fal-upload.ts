// Upload a local file to fal's storage and print its URL, for use as a reference image.
//
//   node tools/fal-upload.ts <file>
//
// Free: storage is not a generation call. Only upload our own files (they become public URLs).
// Network failures are retried: an upload costs nothing, so a repeat is harmless.

import { readFileSync } from 'node:fs';
import { basename, extname } from 'node:path';

const [file] = process.argv.slice(2);
if (!file) throw new Error('usage: node tools/fal-upload.ts <file>');
const key = process.env.FAL_KEY;
if (!key) throw new Error('FAL_KEY is not set (it lives in ~/.config/env/global.env)');

const types: Record<string, string> = { '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.png': 'image/png', '.webp': 'image/webp' };
const contentType = types[extname(file).toLowerCase()];
if (!contentType) throw new Error(`unsupported file type: ${file}`);

async function fetchRetry(url: string, init: RequestInit): Promise<Response> {
  for (let attempt = 1; ; attempt++) {
    try {
      return await fetch(url, init);
    } catch (err) {
      if (attempt >= 6) throw err;
      console.error(`network error (${(err as Error).message}), retry ${attempt} of 5`);
      await new Promise((r) => setTimeout(r, 3000 * attempt));
    }
  }
}

const init = await fetchRetry('https://rest.alpha.fal.ai/storage/upload/initiate?storage_type=fal-cdn-v3', {
  method: 'POST',
  headers: { Authorization: `Key ${key}`, 'Content-Type': 'application/json' },
  body: JSON.stringify({ content_type: contentType, file_name: basename(file) }),
});
if (!init.ok) throw new Error(`initiate ${init.status}: ${await init.text()}`);
const { upload_url, file_url } = (await init.json()) as { upload_url: string; file_url: string };

const put = await fetchRetry(upload_url, { method: 'PUT', headers: { 'Content-Type': contentType }, body: readFileSync(file) });
if (!put.ok) throw new Error(`upload ${put.status}: ${await put.text()}`);
console.log(file_url);
