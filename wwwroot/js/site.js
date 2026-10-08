// Bez inline skriptu/atributu - Content-Security-Policy povoluje jen
// script-src 'self', takze veskere chovani je tu, ne v onclick/onsubmit.

document.addEventListener('submit', (event) => {
    const message = event.target.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        event.preventDefault();
        return;
    }

    // Po odeslani zablokovat tlacitka formulare proti dvojkliku. Az po
    // dokonceni udalosti, jinak by prohlizec formular vubec neodeslal.
    // Hlavni ochranu stejne dela server (operaceId), tohle jen setri pozadavky.
    const form = event.target;
    setTimeout(() => {
        form.querySelectorAll('button[type="submit"]:not([disabled]), button:not([type]):not([disabled])').forEach((button) => {
            button.disabled = true;
            button.dataset.zablokovanoOdeslanim = '1';
        });
    }, 0);
});

// Navrat tlacitkem Zpet muze obnovit stranku z mezipameti i se zablokovanymi tlacitky.
window.addEventListener('pageshow', (event) => {
    if (event.persisted) {
        document.querySelectorAll('button[data-zablokovano-odeslanim]').forEach((button) => {
            button.disabled = false;
            delete button.dataset.zablokovanoOdeslanim;
        });
    }
});

document.addEventListener('click', (event) => {
    const trigger = event.target.closest('[data-select-target]');
    if (trigger) {
        const target = document.getElementById(trigger.getAttribute('data-select-target'));
        target?.select();
        return;
    }

    const copyButton = event.target.closest('[data-copy-target]');
    if (copyButton) {
        const target = document.getElementById(copyButton.getAttribute('data-copy-target'));
        if (target) {
            navigator.clipboard?.writeText(target.value);
        }
    }
});

// Pruvodce po krocich (data-wizard): jeden formular rozdeleny na fieldsety
// data-wizard-step. Bez JavaScriptu zustane jedna dlouha stranka; hodnoty
// se pri prechazeni mezi kroky neztraceji, protoze formular je stale jeden.
document.querySelectorAll('[data-wizard]').forEach((wizard) => {
    const steps = [...wizard.querySelectorAll('[data-wizard-step]')];
    if (steps.length < 2) {
        return;
    }

    const makeButton = (text, className) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = className;
        button.textContent = text;
        return button;
    };

    const nav = document.createElement('div');
    nav.className = 'actions wizard-nav';
    const back = makeButton('Zpět', 'btn btn--secondary');
    const next = makeButton('Další', 'btn');
    nav.append(back, next);
    wizard.append(nav);

    let current = 0;
    const show = (index, scroll) => {
        current = index;
        steps.forEach((step, i) => { step.hidden = i !== index; });
        back.hidden = index === 0;
        next.hidden = index === steps.length - 1;
        if (scroll) {
            wizard.scrollIntoView({ block: 'start' });
        }
    };

    // Dal jen kdyz jsou pole aktualniho kroku platna (napr. zaporne mnozstvi).
    const stepIsValid = () => [...steps[current].querySelectorAll('input, select, textarea')]
        .every((field) => field.reportValidity());

    back.addEventListener('click', () => show(current - 1, true));
    next.addEventListener('click', () => {
        if (stepIsValid()) {
            show(current + 1, true);
        }
    });

    // Enter v poli by odeslal cely formular uprostred pruvodce.
    wizard.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && event.target.matches('input') && current < steps.length - 1) {
            event.preventDefault();
            next.click();
        }
    });

    // "Vse v plnem stavu": doplni cilovou zasobu tam, kde je znama.
    wizard.addEventListener('click', (event) => {
        const fill = event.target.closest('[data-wizard-fill]');
        if (fill) {
            fill.closest('[data-wizard-step]').querySelectorAll('[data-wizard-target]').forEach((input) => {
                const target = input.getAttribute('data-wizard-target');
                if (target) {
                    input.value = target;
                }
            });
        }
    });

    // Po chybe ze serveru otevrit krok, ve kterem chyba je.
    const withError = steps.findIndex((step) => step.querySelector('.input-validation-error, .field-validation-error'));
    show(withError >= 0 ? withError : 0, false);
});
