// Skenovani carovych kodu kamerou (vyzaduje HTTPS nebo localhost).
// Pouziva lokalne vendorovanou knihovnu wwwroot/js/vendor/zxing-browser.min.js
// (@zxing/browser, MIT) - zadne CDN, kvuli CSP script-src 'self'.
(function () {
    if (typeof ZXingBrowser === 'undefined') {
        return;
    }

    let activeControls = null;

    function stopActive() {
        if (activeControls) {
            activeControls.stop();
            activeControls = null;
        }
    }

    function closeOverlay(overlay) {
        stopActive();
        overlay.remove();
    }

    async function lookupBarcode(ean, nameTarget, brandTarget) {
        try {
            const response = await fetch(`/api/barcode/${encodeURIComponent(ean)}`);
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            if (!data.found) {
                return;
            }

            if (nameTarget && !nameTarget.value && data.name) {
                nameTarget.value = data.name;
            }

            if (brandTarget && !brandTarget.value && data.brand) {
                brandTarget.value = data.brand.split(',')[0].trim();
            }
        } catch {
            // Vyhledani se nezdarilo - uzivatel dopise nazev rucne, EAN uz ma.
        }
    }

    async function startScan(button) {
        stopActive();

        const eanTarget = document.getElementById(button.getAttribute('data-ean-target'));
        const nameTargetId = button.getAttribute('data-name-target');
        const brandTargetId = button.getAttribute('data-brand-target');
        const nameTarget = nameTargetId ? document.getElementById(nameTargetId) : null;
        const brandTarget = brandTargetId ? document.getElementById(brandTargetId) : null;

        const overlay = document.createElement('div');
        overlay.className = 'barcode-overlay';

        const video = document.createElement('video');
        video.setAttribute('playsinline', '');
        video.muted = true;
        overlay.appendChild(video);

        const hint = document.createElement('p');
        hint.className = 'barcode-overlay__hint';
        hint.textContent = 'Namiř na čárový kód…';
        overlay.appendChild(hint);

        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn btn--secondary barcode-overlay__close';
        closeBtn.textContent = 'Zavřít';
        closeBtn.addEventListener('click', () => closeOverlay(overlay));
        overlay.appendChild(closeBtn);

        document.body.appendChild(overlay);

        const reader = new ZXingBrowser.BrowserMultiFormatReader();

        try {
            activeControls = await reader.decodeFromConstraints(
                { video: { facingMode: 'environment' } },
                video,
                (result) => {
                    if (result) {
                        const ean = result.getText();
                        if (eanTarget) {
                            eanTarget.value = ean;
                        }
                        closeOverlay(overlay);
                        lookupBarcode(ean, nameTarget, brandTarget);
                    }
                }
            );
        } catch {
            hint.textContent = 'Kamera není dostupná (zkontroluj povolení). Zadej kód ručně.';
        }
    }

    document.addEventListener('click', (event) => {
        const button = event.target.closest('[data-barcode-scan]');
        if (button) {
            startScan(button);
        }
    });
})();
