(function () {
    'use strict';

    // Anti-forgery token for fetch() calls.
    window.vsToken = function () {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    };

    window.vsToast = function (message, isError) {
        var host = document.getElementById('admToasts');
        if (!host) return;
        var div = document.createElement('div');
        div.className = 'adm-toast ' + (isError ? 'err' : 'ok');
        div.innerHTML = '<svg><use href="#' + (isError ? 'i-x' : 'i-check') + '"/></svg><span></span>';
        div.querySelector('span').textContent = message;
        host.appendChild(div);
        dismissLater(div);
    };

    function dismissLater(el) {
        setTimeout(function () {
            el.classList.add('hide');
            setTimeout(function () { el.remove(); }, 320);
        }, 4500);
    }
    document.querySelectorAll('.adm-toast').forEach(dismissLater);

    // Mobile sidebar
    var side = document.getElementById('admSide');
    var scrim = document.getElementById('admScrim');
    var menu = document.getElementById('admMenu');
    function toggleSide(open) {
        side.classList.toggle('open', open);
        scrim.classList.toggle('open', open);
    }
    if (menu) menu.addEventListener('click', function () { toggleSide(true); });
    if (scrim) scrim.addEventListener('click', function () { toggleSide(false); });

    // Photographer switcher keeps the other query parameters.
    var switcher = document.getElementById('photogSwitcher');
    if (switcher) {
        switcher.addEventListener('change', function () {
            var url = new URL(window.location.href);
            if (switcher.value) url.searchParams.set('photographerId', switcher.value);
            else url.searchParams.delete('photographerId');
            url.searchParams.delete('page');
            window.location.href = url.toString();
        });
    }

    // Image viewer: any element with data-view="<src>" opens it.
    var viewer = document.getElementById('admViewer');
    var viewerImg = document.getElementById('admViewerImg');
    var viewerCap = document.getElementById('admViewerCap');
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-view]');
        if (trigger) {
            e.preventDefault();
            viewerImg.src = trigger.getAttribute('data-view');
            viewerCap.textContent = trigger.getAttribute('data-caption') || '';
            viewer.classList.add('open');
            viewer.setAttribute('aria-hidden', 'false');
            return;
        }
        if (viewer.classList.contains('open') && (e.target === viewer || e.target.closest('.adm-viewer-close'))) {
            closeViewer();
        }
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && viewer.classList.contains('open')) closeViewer();
    });
    function closeViewer() {
        viewer.classList.remove('open');
        viewer.setAttribute('aria-hidden', 'true');
        viewerImg.src = '';
    }

    // Confirmations (data-confirm / data-confirm-btn) are handled by modal.js.

    // Reject dialog: buttons with data-reject="<paymentId>" open the <dialog id="rejectDialog">.
    // Looked up on each click because live refresh can replace the page content.
    document.addEventListener('click', function (e) {
        var dialog = document.getElementById('rejectDialog');
        if (!dialog) return;
        var btn = e.target.closest('[data-reject]');
        if (btn) {
            dialog.querySelector('input[name="id"]').value = btn.getAttribute('data-reject');
            dialog.querySelector('[data-reject-label]').textContent = btn.getAttribute('data-label') || '';
            dialog.showModal();
        } else if (e.target.closest('[data-cancel]')) {
            dialog.close();
        }
    });
})();

// =====================================================================
// Live alerts: sound + desktop notification for new bookings and GCash
// payments (only the signed-in admin's own photographer).
// =====================================================================
(function () {
    'use strict';

    var KEY = 'vs-alerts';            // 'on' | 'off' (absent = not decided yet)
    var SEEN = 'vs-alerts-seen';      // ids already announced (shared by all open tabs)
    var POLL_MS = 30000;

    var bell = document.getElementById('alertsBell');
    var prompt = document.getElementById('alertsPrompt');
    if (!bell) return;

    function get(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
    function set(k, v) { try { localStorage.setItem(k, v); } catch (e) { } }
    function isOn() { return get(KEY) === 'on'; }

    // Browsers only allow sound after a click on the page; if blocked, play on the next click.
    function sound(name) {
        var audio = new Audio('/audio/' + name + '.mp3');
        var p = audio.play();
        if (p && p.catch) p.catch(function () {
            var retry = function () { audio.play().catch(function () { }); window.removeEventListener('pointerdown', retry, true); };
            window.addEventListener('pointerdown', retry, true);
        });
    }

    function notify(title, body, url) {
        if (!('Notification' in window) || Notification.permission !== 'granted') return;
        try {
            var n = new Notification(title, { body: body, icon: '/favicon.svg', tag: url });
            n.onclick = function () { window.focus(); window.location.href = url; n.close(); };
        } catch (e) { }
    }

    function render() {
        bell.classList.toggle('on', isOn());
        bell.title = isOn() ? 'Alerts are on (click to turn off)' : 'Turn on sound & desktop alerts';
        if (prompt) prompt.hidden = get(KEY) !== null || sessionStorage.getItem('vs-alerts-later') === '1';
    }

    function enable() {
        set(KEY, 'on');
        var ask = ('Notification' in window && Notification.permission === 'default') ? Notification.requestPermission() : Promise.resolve();
        ask.then(function () {
            sound('alerts-enabled');
            var blocked = 'Notification' in window && Notification.permission === 'denied';
            window.vsToast(blocked
                ? 'Sound alerts on. Desktop notifications are blocked in your browser settings.'
                : 'Alerts on. You will hear new bookings and payments while this tab is open.');
            render();
        });
    }

    bell.addEventListener('click', function () {
        if (isOn()) { set(KEY, 'off'); render(); window.vsToast('Alerts turned off.'); }
        else enable();
    });
    if (prompt) {
        document.getElementById('alertsEnable').addEventListener('click', enable);
        document.getElementById('alertsLater').addEventListener('click', function () {
            sessionStorage.setItem('vs-alerts-later', '1');
            render();
        });
    }

    // Feedback sound for the action just taken (verify / decline / cancel).
    var cue = document.body.getAttribute('data-sound');
    if (cue && isOn()) sound(cue);

    // ------------------------------------------------------------ Live refresh + alerts
    // Every 5 s (15 s while the tab is in the background) ask the server whether anything changed.
    var VISIBLE_MS = 5000, HIDDEN_MS = 15000;
    var since = null, stamp = null, timer = null, refreshPending = false;
    var content = document.querySelector('.adm-content');

    function seen() { try { return JSON.parse(get(SEEN) || '[]'); } catch (e) { return []; } }
    function markSeen(id) {
        var list = seen();
        if (list.indexOf(id) >= 0) return false;
        list.push(id);
        set(SEEN, JSON.stringify(list.slice(-200)));
        return true;
    }

    function setBadge(href, count, gold) {
        var link = document.querySelector('.adm-nav a[href$="' + href + '"]');
        if (!link) return;
        var badge = link.querySelector('.adm-badge');
        if (!count) { if (badge) badge.remove(); return; }
        if (!badge) { badge = document.createElement('span'); badge.className = 'adm-badge' + (gold ? ' gold' : ''); link.appendChild(badge); }
        badge.textContent = count;
    }

    // Typing in a form, a dialog or photo viewer open, or text selected: wait before refreshing.
    if (content) content.addEventListener('input', function () { content.dataset.dirty = '1'; });
    function busy() {
        var active = document.activeElement;
        if (active && content && content.contains(active) && /^(INPUT|TEXTAREA|SELECT)$/.test(active.tagName)) return true;
        if (content && content.dataset.dirty === '1') return true;
        if (document.querySelector('dialog[open], .adm-viewer.open')) return true;
        var sel = window.getSelection && window.getSelection();
        return !!(sel && sel.toString());
    }

    function refreshPage() {
        var mode = content && content.getAttribute('data-live');
        if (!mode) return;
        if (mode === 'event') { document.dispatchEvent(new CustomEvent('vs:live')); return; }
        if (busy()) { refreshPending = true; return; }
        refreshPending = false;

        fetch(window.location.href, { cache: 'no-store', credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.text() : null; })
            .then(function (html) {
                if (!html || busy()) { refreshPending = true; return; }
                var fresh = new DOMParser().parseFromString(html, 'text/html').querySelector('.adm-content');
                if (!fresh) return;
                content.classList.add('live');      // no entrance animations on refreshed content
                content.innerHTML = fresh.innerHTML;
                render();
            })
            .catch(function () { refreshPending = true; });
    }

    function poll() {
        fetch('/Admin/AlertsFeed' + (since ? '?since=' + since : ''), { cache: 'no-store', credentials: 'same-origin' })
            .then(function (r) { return r.ok && (r.headers.get('content-type') || '').indexOf('json') >= 0 ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                since = data.now;
                setBadge('/Admin/Bookings', data.counts.pendingBookings, false);
                setBadge('/Admin/Transactions', data.counts.pendingPayments, true);

                if (stamp !== null && data.stamp !== stamp) refreshPage();
                else if (refreshPending && !document.hidden) refreshPage();
                stamp = data.stamp;

                if (!isOn()) return;
                var newBookings = data.bookings.filter(function (b) { return markSeen('b:' + b.id); });
                var newPayments = data.payments.filter(function (p) { return markSeen('p:' + p.id); });

                newBookings.forEach(function (b) {
                    notify('New booking request', b.client + ' - ' + b.category + ' on ' + b.date, '/Admin/BookingDetails/' + encodeURIComponent(b.id));
                    window.vsToast('New booking: ' + b.client + ' (' + b.category + ', ' + b.date + ')');
                });
                newPayments.forEach(function (p) {
                    notify('New GCash payment to verify', p.client + ' - ' + p.amount + ' (' + p.type + ')', '/Admin/BookingDetails/' + encodeURIComponent(p.booking));
                    window.vsToast('Payment to verify: ' + p.client + ' - ' + p.amount);
                });

                // One sound per check: a payment needs action, so it wins over a booking.
                if (newPayments.length) sound('new-payment');
                else if (newBookings.length) sound('new-booking');
                if (newPayments.length || newBookings.length) {
                    bell.classList.remove('ring'); void bell.offsetWidth; bell.classList.add('ring');
                }
            })
            .catch(function () { })
            .then(schedule);
    }

    function schedule() {
        clearTimeout(timer);
        timer = setTimeout(poll, document.hidden ? HIDDEN_MS : VISIBLE_MS);
    }

    // Coming back to the tab: check straight away.
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) { clearTimeout(timer); poll(); }
    });

    render();
    poll();
})();


// Discount fields: only show the details once a discount type is chosen.
(function () {
    function sync(box) {
        var sel = box.querySelector('[data-discount-type]');
        box.classList.toggle('has-disc', !!(sel && sel.value));
    }
    document.querySelectorAll('[data-discount]').forEach(sync);
    document.addEventListener('change', function (e) {
        if (e.target.matches('[data-discount-type]')) sync(e.target.closest('[data-discount]'));
    });
})();
