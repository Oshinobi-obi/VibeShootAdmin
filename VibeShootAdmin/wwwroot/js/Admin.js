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

    // Confirm dialogs: <form data-confirm="Are you sure?">
    document.addEventListener('submit', function (e) {
        var form = e.target;
        var msg = form.getAttribute('data-confirm');
        if (msg && !window.confirm(msg)) e.preventDefault();
    });

    // Reject dialog: buttons with data-reject="<paymentId>" open a <dialog id="rejectDialog">
    var rejectDialog = document.getElementById('rejectDialog');
    if (rejectDialog) {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-reject]');
            if (!btn) return;
            rejectDialog.querySelector('input[name="id"]').value = btn.getAttribute('data-reject');
            rejectDialog.querySelector('[data-reject-label]').textContent = btn.getAttribute('data-label') || '';
            rejectDialog.showModal();
        });
        rejectDialog.querySelector('[data-cancel]').addEventListener('click', function () { rejectDialog.close(); });
    }
})();
