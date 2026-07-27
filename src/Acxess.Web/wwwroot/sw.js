// wwwroot/sw.js
const CACHE_NAME = 'acxess-cache-v1';

self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(clients.claim());
});

self.addEventListener('fetch', event => {
    // event.respondWith(fetch(event.request));
    return;
});