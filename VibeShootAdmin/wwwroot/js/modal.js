/*
 * VibeShoot modal dialogs: confirmations and notifications (shared by the public site and the admin).
 *
 *   vsModal.confirm({ title, message, ok, cancel, variant: 'gold' | 'danger' }) -> Promise<boolean>
 *   vsModal.notice({ title, message, ok, variant: 'success' | 'error' | 'info' }) -> Promise<void>
 *
 * Declarative use (no script needed):
 *   <form data-confirm="Verify this payment?" data-confirm-title="Verify payment" data-confirm-ok="Verify">
 *   <button data-confirm="Decline this booking?" data-confirm-variant="danger">
 *   <a href="..." data-confirm="Leave this page?">
 *   <template data-notice data-title="Saved" data-variant="success">Your changes were saved.</template>
 *
 * These are for clarity and to prevent mistakes; every action is still checked by the server.
 */
(function () {
    'use strict';
    if (window.vsModal) return;

    var ICONS = {
        gold: '<path d="M12 8v5m0 3.5h.01M10.3 3.9 2.6 17.2A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.8L13.7 3.9a2 2 0 0 0-3.4 0Z"/>',
        danger: '<path d="M12 8v5m0 3.5h.01M10.3 3.9 2.6 17.2A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.8L13.7 3.9a2 2 0 0 0-3.4 0Z"/>',
        success: '<path d="m5 12.5 4.5 4.5L19 7.5"/>',
        error: '<path d="M7 7l10 10M17 7 7 17"/>',
        info: '<path d="M12 11v6m0-9.5h.01"/><circle cx="12" cy="12" r="9.5"/>',
        question: '<path d="M9.2 9.3a2.9 2.9 0 1 1 4.3 2.6c-.9.5-1.5 1.1-1.5 2.1v.5m0 3h.01"/><circle cx="12" cy="12" r="9.5"/>'
    };

    var queue = Promise.resolve();
    var lastFocus = null;

    function el(tag, cls, html) {
        var e = document.createElement(tag);
        if (cls) e.className = cls;
        if (html != null) e.innerHTML = html;
        return e;
    }

    function open(opts) {
        return new Promise(function (resolve) {
            var isConfirm = opts.kind === 'confirm';
            var variant = opts.variant || (isConfirm ? 'gold' : 'success');
            var icon = ICONS[opts.icon || (isConfirm && variant === 'gold' ? 'question' : variant)] || ICONS.info;

            var backdrop = el('div', 'vsm-backdrop');
            var box = el('div', 'vsm-box vsm-' + variant);
            box.setAttribute('role', isConfirm ? 'alertdialog' : 'dialog');
            box.setAttribute('aria-modal', 'true');

            var iconEl = el('span', 'vsm-icon', '<svg viewBox="0 0 24 24" aria-hidden="true">' + icon + '</svg>');
            var title = el('h2', 'vsm-title'); title.id = 'vsm-title-' + Date.now(); title.textContent = opts.title || (isConfirm ? 'Are you sure?' : 'Done');
            var msg = el('p', 'vsm-msg'); msg.textContent = opts.message || '';
            box.setAttribute('aria-labelledby', title.id);

            var actions = el('div', 'vsm-actions');
            var okBtn = el('button', 'vsm-btn vsm-ok'); okBtn.type = 'button';
            okBtn.textContent = opts.ok || (isConfirm ? 'Confirm' : 'OK');
            if (isConfirm) {
                var cancelBtn = el('button', 'vsm-btn vsm-cancel'); cancelBtn.type = 'button';
                cancelBtn.textContent = opts.cancel || 'Cancel';
                actions.appendChild(cancelBtn);
            }
            actions.appendChild(okBtn);

            box.appendChild(iconEl); box.appendChild(title);
            if (opts.message) box.appendChild(msg);
            box.appendChild(actions);
            backdrop.appendChild(box);

            lastFocus = document.activeElement;
            document.body.appendChild(backdrop);
            requestAnimationFrame(function () { backdrop.classList.add('show'); });
            (isConfirm ? cancelBtn : okBtn).focus();   // safe default: Enter on a confirm = cancel

            function close(result) {
                document.removeEventListener('keydown', onKey, true);
                backdrop.classList.remove('show');
                setTimeout(function () { backdrop.remove(); }, 220);
                if (lastFocus && lastFocus.focus) { try { lastFocus.focus(); } catch (e) { } }
                resolve(result);
            }
            function onKey(e) {
                if (e.key === 'Escape') { e.preventDefault(); close(false); }
                else if (e.key === 'Tab') {   // keep focus inside the dialog
                    var f = box.querySelectorAll('button');
                    var first = f[0], last = f[f.length - 1];
                    if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                    else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
                }
            }
            document.addEventListener('keydown', onKey, true);
            okBtn.addEventListener('click', function () { close(true); });
            if (cancelBtn) cancelBtn.addEventListener('click', function () { close(false); });
            backdrop.addEventListener('click', function (e) { if (e.target === backdrop) close(false); });
        });
    }

    // One dialog at a time: later calls wait for the current one to close.
    function enqueue(opts) {
        var p = queue.then(function () { return open(opts); });
        queue = p.then(function () { }, function () { });
        return p;
    }

    window.vsModal = {
        confirm: function (opts) { return enqueue(Object.assign({ kind: 'confirm' }, opts || {})); },
        notice: function (opts) { return enqueue(Object.assign({ kind: 'notice' }, opts || {})); }
    };

    // ------------------------------------------------------------------ Declarative confirmations

    function optsFrom(node) {
        return {
            title: node.getAttribute('data-confirm-title') || undefined,
            message: node.getAttribute('data-confirm') || node.getAttribute('data-confirm-btn') || '',
            ok: node.getAttribute('data-confirm-ok') || undefined,
            variant: node.getAttribute('data-confirm-variant') || undefined
        };
    }

    // Buttons and links with data-confirm (checked first, so a button's own message wins over its form's).
    document.addEventListener('click', function (e) {
        var node = e.target.closest('[data-confirm], [data-confirm-btn]');
        if (!node || node.tagName === 'FORM' || node.dataset.vsmOk === '1') return;
        e.preventDefault();
        e.stopImmediatePropagation();
        if (!allowed(node.form || node.closest('form'))) return;
        vsModal.confirm(optsFrom(node)).then(function (yes) {
            if (!yes) return;
            if (node.tagName === 'A') { window.location.href = node.href; return; }
            var form = node.form || node.closest('form');
            if (form) {
                form.dataset.vsmOk = '1';             // the form's own confirmation is covered by this one
                if (form.requestSubmit) form.requestSubmit(node.type === 'submit' ? node : undefined);
                else { node.dataset.vsmOk = '1'; node.click(); }
            } else {
                node.dataset.vsmOk = '1'; node.click(); delete node.dataset.vsmOk;
            }
        });
    }, true);

    // A form can veto the confirmation (e.g. "please choose a rating first") by cancelling this event.
    function allowed(form) {
        return !form || form.dispatchEvent(new CustomEvent('vs:beforeconfirm', { cancelable: true }));
    }

    // Forms with data-confirm. Runs before other submit handlers (window capture phase).
    window.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.hasAttribute || !form.hasAttribute('data-confirm')) return;
        if (form.dataset.vsmOk === '1') { delete form.dataset.vsmOk; return; }
        e.preventDefault();
        e.stopImmediatePropagation();
        if (!allowed(form)) return;
        var submitter = e.submitter;
        vsModal.confirm(optsFrom(form)).then(function (yes) {
            if (!yes) return;
            form.dataset.vsmOk = '1';
            if (form.requestSubmit) form.requestSubmit(submitter && submitter.form === form ? submitter : undefined);
            else form.submit();
        });
    }, true);

    // Clean up the "already confirmed" flag once a confirmed form has actually gone through.
    window.addEventListener('submit', function (e) {
        var f = e.target;
        if (f.dataset && f.dataset.vsmOk === '1' && !f.hasAttribute('data-confirm')) delete f.dataset.vsmOk;
    });

    // ------------------------------------------------------------------ Server notices on page load
    function showNotices() {
        document.querySelectorAll('template[data-notice]').forEach(function (t) {
            vsModal.notice({
                title: t.getAttribute('data-title') || undefined,
                message: (t.content ? t.content.textContent : t.textContent).trim(),
                variant: t.getAttribute('data-variant') || 'success',
                ok: t.getAttribute('data-ok') || undefined
            });
            t.remove();
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', showNotices);
    else showNotices();
})();
