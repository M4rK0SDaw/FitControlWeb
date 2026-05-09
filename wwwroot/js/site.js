(() => {
    const body = document.body;
    const sidebarToggle = document.getElementById('toggleSidebar');
    const openMobileSidebar = document.getElementById('openMobileSidebar');
    const closeMobileSidebar = document.getElementById('closeMobileSidebar');
    const sidebarBackdrop = document.getElementById('sidebarBackdrop');
    const themeToggle = document.getElementById('themeToggle');

    const sidebarPreference = localStorage.getItem('fitcontrol-sidebar');
    const themePreference = localStorage.getItem('fitcontrol-theme');
    const visualPreferences = JSON.parse(localStorage.getItem('fitcontrol-visual-preferences') || '{}');

    function applyVisualPreferences(preferences) {
        const accent = preferences.accent || localStorage.getItem('fitcontrol-accent') || '#ff7a00';
        const background = preferences.background || localStorage.getItem('fitcontrol-background') || 'soft';
        const fontScale = preferences.fontScale || localStorage.getItem('fitcontrol-font-scale') || '1';

        document.documentElement.style.setProperty('--gym-orange', accent);
        document.documentElement.style.setProperty('--gym-orange-strong', accent);
        document.documentElement.style.setProperty('--gym-font-scale', fontScale);

        body.classList.remove('bg-soft', 'bg-clean', 'bg-contrast');
        body.classList.add(`bg-${background}`);
    }

    if (sidebarPreference === 'collapsed') {
        body.classList.add('sidebar-collapsed');
    }

    if (themePreference === 'dark') {
        body.classList.add('dark-mode');
    }

    applyVisualPreferences(visualPreferences);

    function closeMobileMenu() {
        body.classList.remove('mobile-sidebar-open');
    }

    sidebarToggle?.addEventListener('click', () => {
        body.classList.toggle('sidebar-collapsed');
        localStorage.setItem(
            'fitcontrol-sidebar',
            body.classList.contains('sidebar-collapsed') ? 'collapsed' : 'expanded'
        );
    });

    openMobileSidebar?.addEventListener('click', () => {
        body.classList.add('mobile-sidebar-open');
    });

    closeMobileSidebar?.addEventListener('click', closeMobileMenu);
    sidebarBackdrop?.addEventListener('click', closeMobileMenu);

    document.querySelectorAll('.sidebar-link').forEach(link => {
        link.addEventListener('click', () => {
            if (window.innerWidth < 992) {
                closeMobileMenu();
            }
        });
    });

    themeToggle?.addEventListener('click', () => {
        body.classList.toggle('dark-mode');
        localStorage.setItem(
            'fitcontrol-theme',
            body.classList.contains('dark-mode') ? 'dark' : 'light'
        );
    });

    document.addEventListener('fitcontrol:preferences-changed', (event) => {
        applyVisualPreferences(event.detail || {});
    });
})();
