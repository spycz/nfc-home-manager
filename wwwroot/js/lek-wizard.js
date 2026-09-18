(function () {
    'use strict';
    const form = document.getElementById('lek-wizard');
    if (!form) return;
    const steps = [...form.querySelectorAll('[data-wizard-step]')];
    const field = name => document.getElementById('Input_' + name);
    const product = field('PripravekId');
    let current = 0;
    function applyProduct() {
        const option = product.selectedOptions[0];
        const existing = !!product.value;
        if (existing) {
            for (const [name, attr] of Object.entries({ Nazev: 'name', Sila: 'strength', Forma: 'form', ObsahBaleni: 'size', Jednotka: 'unit' }))
                field(name).value = option.dataset[attr] || '';
            field('Ean').value = option.dataset.ean || '';
        }
        form.querySelectorAll('[data-product-field]').forEach(input => {
            if (input.tagName === 'SELECT') input.disabled = existing;
            else input.readOnly = existing;
        });
    }
    product.addEventListener('change', applyProduct);
    function findCode() {
        const code = field('Ean').value.trim();
        const match = [...product.options].find(o => code && o.dataset.ean === code);
        const result = document.getElementById('code-result');
        if (match) {
            product.value = match.value;
            applyProduct();
            result.textContent = 'Nalezeno ve vlastní evidenci. V dalším kroku ověř variantu.';
        } else {
            product.value = '';
            // Nový sken nesmí zdědit identitu přípravku předchozího kódu.
            ['Nazev', 'Sila', 'Forma', 'ObsahBaleni'].forEach(n => { field(n).value = ''; });
            applyProduct();
            result.textContent = code ? 'Kód zatím neznáme. Zadej přípravek ručně.' : 'Pokračuj ručním zadáním nebo výběrem.';
        }
    }
    field('Ean').addEventListener('change', findCode);
    function summary() {
        const root = document.getElementById('wizard-summary');
        root.replaceChildren();
        const values = {
            'Přípravek': [field('Nazev').value, field('Sila').value, field('Forma').value].filter(Boolean).join(' · '),
            'Zůstatek': field('MnozstviNezname').checked ? 'Neznámý' : field('Mnozstvi').value + ' ' + field('Jednotka').value,
            'Expirace': field('ExpiraceNeznama').checked ? 'K doplnění' : field('Expirace').value,
            'Umístění': field('LekarnickaId').selectedOptions[0]?.textContent,
            'Sledovat expiraci': field('SledovatExpiraci').checked ? 'Ano' : 'Ne'
        };
        for (const [label, value] of Object.entries(values)) {
            const dt = document.createElement('dt'); dt.textContent = label;
            const dd = document.createElement('dd'); dd.textContent = value || 'Neuvedeno';
            root.append(dt, dd);
        }
    }
    function show(index) {
        current = index;
        steps.forEach((step, i) => { step.hidden = i !== index; });
        document.getElementById('wizard-progress').textContent = `Krok ${index + 1} z ${steps.length}`;
        document.getElementById('wizard-back').hidden = index === 0;
        document.getElementById('wizard-next').hidden = index === steps.length - 1;
        if (index === steps.length - 1) summary();
        const legend = steps[index].querySelector('legend');
        legend.tabIndex = -1;
        legend.focus();
    }
    document.getElementById('wizard-back').addEventListener('click', () => show(current - 1));
    document.getElementById('wizard-next').addEventListener('click', () => {
        const invalid = [...steps[current].querySelectorAll('input, select, textarea')].find(i => !i.checkValidity());
        if (invalid) { invalid.reportValidity(); return; }
        show(current + 1);
    });
    // Nativní validace musí nejprve odkrýt pole ze skrytého kroku.
    form.addEventListener('invalid', event => {
        const index = steps.findIndex(step => step.contains(event.target));
        if (index >= 0) show(index);
    }, true);
    form.addEventListener('submit', event => {
        if (current !== steps.length - 1) { event.preventDefault(); show(current + 1); }
    });
    applyProduct();
    const errors = form.querySelector('.validation-summary-errors');
    // Při serverové chybě jsou všechna pole dostupná k opravě, včetně chybové zprávy.
    if (!errors) { document.getElementById('wizard-navigation').hidden = false; show(0); }
    else current = steps.length - 1;
})();
