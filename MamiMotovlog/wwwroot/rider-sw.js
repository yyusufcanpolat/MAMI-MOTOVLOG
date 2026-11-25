'use strict';
// Private pages, HTML and API responses are NEVER cached. Offline support is static-shell only.
const CACHE = 'mami-journal-v54-static-1';
const ASSETS = ['/css/rider.css','/css/mono.css','/css/editorial-v4.css','/css/journal-v5.css','/logo-bike.svg','/favicon.svg'];
self.addEventListener('install', e => e.waitUntil(caches.open(CACHE).then(c=>c.addAll(ASSETS)).then(()=>self.skipWaiting())));
self.addEventListener('activate', e => e.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(k=>k.startsWith('mami-')&&k!==CACHE).map(k=>caches.delete(k)))).then(()=>self.clients.claim())));
self.addEventListener('fetch', e => {
  const request=e.request;const url=new URL(request.url);
  if (request.method!=='GET'||url.origin!==self.location.origin||url.search||!ASSETS.includes(url.pathname))return;
  e.respondWith(caches.match(request).then(cached=>cached||fetch(request)));
});
