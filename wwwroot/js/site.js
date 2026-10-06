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
