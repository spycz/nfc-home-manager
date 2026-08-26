// Bez inline skriptu/atributu - Content-Security-Policy povoluje jen
// script-src 'self', takze veskere chovani je tu, ne v onclick/onsubmit.

document.addEventListener('submit', (event) => {
    const message = event.target.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        event.preventDefault();
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
