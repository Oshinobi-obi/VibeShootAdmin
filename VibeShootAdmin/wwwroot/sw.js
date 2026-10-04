/*
 * VibeShoot Admin service worker: makes the console installable and shows a friendly
 * offline screen. It only caches the app's own styles, scripts, icons and sounds;
 * pages with client data, images of payments and anything sent to the server are
 * never stored, so nothing private stays on the device.
 */
'use strict';

var VERSION = 'vibeshoot-admin-v1';
var OFFLINE_URL = '/offline.html';
var PRECACHE = [OFFLINE_URL, '/favicon.svg', '/icons/icon-192.png'];

self.addEventListener('install', function (event) {
    event.waitUntil(caches.open(VERSION).then(function (cache) { return cache.addAll(PRECACHE); }));
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys()
            .then(function (keys) { return Promise.all(keys.filter(function (k) { return k !== VERSION; }).map(function (k) { return caches.delete(k); })); })
            .then(function () { return self.clients.claim(); })
    );
});

function isStaticAsset(url) {
    return url.origin === self.location.origin &&
        /^\/(css|js|icons|audio|lib)\//.test(url.pathname) || url.pathname === '/favicon.svg';
}

self.addEventListener('fetch', function (event) {
    var req = event.request;
    if (req.method !== 'GET') return;                       // never touch form posts
    var url = new URL(req.url);

    // Pages: always from the network (live data). Offline -> friendly screen.
    if (req.mode === 'navigate') {
        event.respondWith(fetch(req).catch(function () { return caches.match(OFFLINE_URL); }));
        return;
    }

    // App files: serve from cache, refresh in the background. (CSS/JS URLs carry a version, so updates arrive.)
    if (isStaticAsset(url)) {
        event.respondWith(
            caches.open(VERSION).then(function (cache) {
                return cache.match(req).then(function (cached) {
                    var network = fetch(req).then(function (res) {
                        if (res.ok) cache.put(req, res.clone());
                        return res;
                    }).catch(function () { return cached; });
                    return cached || network;
                });
            })
        );
    }
    // Everything else (data feeds, /media images, alerts) goes straight to the network.
});

// Clicking a notification focuses the open console (or opens it).
self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var target = (event.notification.data && event.notification.data.url) || '/Admin/Dashboard';
    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (list) {
            for (var i = 0; i < list.length; i++) {
                if ('focus' in list[i]) { list[i].navigate(target); return list[i].focus(); }
            }
            return self.clients.openWindow(target);
        })
    );
});
