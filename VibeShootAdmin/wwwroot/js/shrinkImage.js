/*
 * Shrinks a photo in the browser before it is uploaded, so images stored in the
 * database stay small (a phone screenshot of 3-8 MB becomes roughly 200-500 KB).
 *
 *   vsShrinkImage(file, { maxSize: 1600, quality: 0.82 }) -> Promise<File>
 *
 * PNGs are kept as PNG unless they are large (so logos keep transparency and
 * QR codes stay pixel-sharp). Anything that can't be decoded is returned as-is
 * and the server decides.
 */
(function () {
    'use strict';

    window.vsShrinkImage = function (file, options) {
        var opts = Object.assign({ maxSize: 1600, quality: 0.82, keepPngUnder: 1.5 * 1024 * 1024 }, options || {});
        if (!file || !/^image\//.test(file.type)) return Promise.resolve(file);
        if (file.type === 'image/png' && file.size <= opts.keepPngUnder) return Promise.resolve(file);

        return new Promise(function (resolve) {
            var url = URL.createObjectURL(file);
            var img = new Image();
            img.onload = function () {
                URL.revokeObjectURL(url);
                var scale = Math.min(1, opts.maxSize / Math.max(img.naturalWidth, img.naturalHeight));
                if (scale === 1 && file.size < 400 * 1024) { resolve(file); return; }

                var canvas = document.createElement('canvas');
                canvas.width = Math.round(img.naturalWidth * scale);
                canvas.height = Math.round(img.naturalHeight * scale);
                var ctx = canvas.getContext('2d');
                ctx.fillStyle = '#fff'; // JPEG has no transparency
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

                canvas.toBlob(function (blob) {
                    if (!blob || blob.size >= file.size) { resolve(file); return; }
                    var name = file.name.replace(/\.[^.]+$/, '') + '.jpg';
                    resolve(new File([blob], name, { type: 'image/jpeg', lastModified: Date.now() }));
                }, 'image/jpeg', opts.quality);
            };
            img.onerror = function () { URL.revokeObjectURL(url); resolve(file); };
            img.src = url;
        });
    };

    /*
     * Shrinks the files of every <input type="file" data-shrink> in a form right before it is
     * submitted. Optional attributes: data-shrink-max="2048", data-shrink-quality="0.85".
     */
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form.dataset.vsShrunk === '1') return;
        var inputs = Array.prototype.filter.call(form.querySelectorAll('input[type=file][data-shrink]'), function (i) { return i.files && i.files.length; });
        if (!inputs.length || typeof DataTransfer === 'undefined') return;

        e.preventDefault();
        var submitter = e.submitter;
        if (submitter) submitter.disabled = true;

        Promise.all(inputs.map(function (input) {
            var opts = {
                maxSize: Number(input.dataset.shrinkMax) || 1600,
                quality: Number(input.dataset.shrinkQuality) || 0.82
            };
            return Promise.all(Array.prototype.map.call(input.files, function (f) { return window.vsShrinkImage(f, opts); }))
                .then(function (files) {
                    var dt = new DataTransfer();
                    files.forEach(function (f) { dt.items.add(f); });
                    input.files = dt.files;
                });
        })).then(function () {
            form.dataset.vsShrunk = '1';
            if (submitter && submitter.name) {
                var hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = submitter.name;
                hidden.value = submitter.value;
                form.appendChild(hidden);
            }
            HTMLFormElement.prototype.submit.call(form);
        });
    }, true);
})();
