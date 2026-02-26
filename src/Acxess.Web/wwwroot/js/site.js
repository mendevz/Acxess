document.addEventListener('alpine:init', () => {
    Alpine.data('layout', () => ({
        sidebarOpen: false, 
        darkMode: localStorage.getItem('theme') === 'dark',
        init() {
            // Inicializar tema
            if (this.darkMode) document.documentElement.classList.add('dark');
            else document.documentElement.classList.remove('dark');
        },
        toggleTheme() {
            this.darkMode = !this.darkMode;
            localStorage.setItem('theme', this.darkMode ? 'dark' : 'light');
            if (this.darkMode) document.documentElement.classList.add('dark');
            else document.documentElement.classList.remove('dark');
        }
    }));

});


// =========================================================================
// INTERCEPTOR GLOBAL DE AUTOFILL (Bloquea sugerencias del navegador)
// =========================================================================
function disableGlobalAutofill(container) {
    const inputs = container.querySelectorAll('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]), textarea');

    inputs.forEach(input => {
        input.setAttribute('autocomplete', 'off');

        input.setAttribute('autocomplete', 'nope');

        input.setAttribute('data-lpignore', 'true');
        input.setAttribute('data-1p-ignore', 'true');
        input.setAttribute('data-form-type', 'other');

        input.style.webkitBoxShadow = "0 0 0px 1000px transparent inset";
        input.style.transition = "background-color 5000s ease-in-out 0s";
    });
}

document.addEventListener('DOMContentLoaded', () => {
    disableGlobalAutofill(document);
});

document.addEventListener('htmx:load', (evt) => {
    disableGlobalAutofill(evt.detail.elt);
});