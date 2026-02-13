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
