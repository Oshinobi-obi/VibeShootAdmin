/*
 * Makes VibeShoot Admin installable as an app (its own window and desktop/home-screen icon).
 * Any element with [data-install-app] becomes an "Install app" button when the browser allows it.
 * Note: browsers only offer installing on HTTPS sites (and on localhost for testing).
 */
(function () {
    'use strict';

    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js').catch(function () { /* http or unsupported: the site still works normally */ });
        });
    }

    var buttons = function () { return document.querySelectorAll('[data-install-app]'); };
    var installed = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    var isIos = /iphone|ipad|ipod/i.test(navigator.userAgent) && !window.MSStream;
    var deferred = null;

    function show(on) { buttons().forEach(function (b) { b.hidden = !on; }); }

    if (installed) { document.documentElement.classList.add('is-installed'); return; }

    // Chrome, Edge, Android: the browser tells us when installing is possible.
    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferred = e;
        show(true);
    });

    // iPhone/iPad: no install prompt exists, so explain the Safari steps instead.
    if (isIos) document.addEventListener('DOMContentLoaded', function () { show(true); });

    document.addEventListener('click', function (e) {
        if (!e.target.closest('[data-install-app]')) return;
        if (deferred) {
            deferred.prompt();
            deferred.userChoice.then(function () { deferred = null; show(false); });
        } else if (isIos && window.vsModal) {
            vsModal.notice({
                title: 'Install on iPhone',
                message: 'In Safari, tap the Share button (the square with an arrow), then choose "Add to Home Screen". VibeShoot Admin will open like an app.',
                variant: 'info', ok: 'Got it'
            });
        }
    });

    window.addEventListener('appinstalled', function () {
        show(false);
        if (window.vsToast) window.vsToast('VibeShoot Admin is installed. Open it from your desktop or home screen.');
    });
})();
