(() => {
    const body = document.body;
    const sidebarToggle = document.getElementById('toggleSidebar');
    const openMobileSidebar = document.getElementById('openMobileSidebar');
    const closeMobileSidebar = document.getElementById('closeMobileSidebar');
    const sidebarBackdrop = document.getElementById('sidebarBackdrop');
    const themeToggle = document.getElementById('themeToggle');

    const sidebarPreference = localStorage.getItem('fitcontrol-sidebar');

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            return parts.pop().split(';').shift();
        }
        return null;
    }

    function setCookie(name, value, days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value}; expires=${date.toUTCString()}; path=/; SameSite=Lax`;
    }

    const themePreference = getCookie('fitcontrol-theme');
    const visualPreferences = {
        accent: getCookie('fitcontrol-accent'),
        background: getCookie('fitcontrol-background'),
        fontScale: getCookie('fitcontrol-font-scale')
    };

    function applyVisualPreferences(preferences) {
        const accent = preferences.accent || getCookie('fitcontrol-accent') || '#ff7a00';
        const background = preferences.background || getCookie('fitcontrol-background') || 'soft';
        const fontScale = preferences.fontScale || getCookie('fitcontrol-font-scale') || '1';

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
        setCookie('fitcontrol-theme', body.classList.contains('dark-mode') ? 'dark' : 'light', 3650);
    });

    document.addEventListener('fitcontrol:preferences-changed', (event) => {
        const prefs = event.detail || {};
        if (prefs.theme) setCookie('fitcontrol-theme', prefs.theme, 3650);
        if (prefs.accent) setCookie('fitcontrol-accent', prefs.accent, 3650);
        if (prefs.background) setCookie('fitcontrol-background', prefs.background, 3650);
        if (prefs.fontScale) setCookie('fitcontrol-font-scale', prefs.fontScale, 3650);
        applyVisualPreferences(prefs);
    });
})();
