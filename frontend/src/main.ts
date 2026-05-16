import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Safety net against stale-chunk problems after a backend deploy.
// Angular lazy-loads route bundles by content-hashed filename. If the
// browser still has the old index.html cached and tries to fetch a chunk
// whose hash has changed in the new deploy, the request 404s and the
// app dies silently. Catching the error and reloading recovers the user
// because the reload re-fetches index.html (Caddy serves it no-cache)
// and pulls the new chunk names.
//
// The session-storage timestamp prevents an infinite reload loop in
// case the chunk is truly missing (e.g. a deploy that removed a route).
const CHUNK_RELOAD_KEY = 'tc.chunkReloadAt';
const CHUNK_RELOAD_COOLDOWN_MS = 10_000;

function looksLikeChunkLoadError(message: string): boolean {
  return message.includes('Loading chunk')
    || message.includes('Failed to fetch dynamically imported')
    || message.includes('Importing a module script failed')
    || message.includes('ChunkLoadError');
}

function reloadOnceForChunkError(reason: string): void {
  const last = Number(sessionStorage.getItem(CHUNK_RELOAD_KEY) ?? 0);
  if (Date.now() - last < CHUNK_RELOAD_COOLDOWN_MS) return;
  sessionStorage.setItem(CHUNK_RELOAD_KEY, String(Date.now()));
  // eslint-disable-next-line no-console
  console.warn('Chunk load error, reloading page:', reason);
  location.reload();
}

window.addEventListener('error', (event) => {
  if (looksLikeChunkLoadError(event.message ?? '')) {
    reloadOnceForChunkError(event.message ?? 'window error');
  }
});

window.addEventListener('unhandledrejection', (event) => {
  const message = String((event.reason as { message?: unknown })?.message ?? event.reason ?? '');
  if (looksLikeChunkLoadError(message)) {
    reloadOnceForChunkError(message);
  }
});

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
