(() => {
    const body = document.body;
    const isAuthenticated = body.dataset.authenticated === 'true';
    const sidebarToggle = document.getElementById('toggleSidebar');
    const openMobileSidebar = document.getElementById('openMobileSidebar');
    const closeMobileSidebar = document.getElementById('closeMobileSidebar');
    const sidebarBackdrop = document.getElementById('sidebarBackdrop');
    const themeToggle = document.getElementById('themeToggle');

    const sidebarPreference = isAuthenticated ? localStorage.getItem('fitcontrol-sidebar') : null;

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

    const themePreference = isAuthenticated ? getCookie('fitcontrol-theme') : null;
    const visualPreferences = {
        accent: isAuthenticated ? getCookie('fitcontrol-accent') : null,
        background: isAuthenticated ? getCookie('fitcontrol-background') : null,
        fontScale: isAuthenticated ? getCookie('fitcontrol-font-scale') : null
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

    function applyThemeLogo() {
        const isDark = body.classList.contains('dark-mode');

        document.querySelectorAll('.js-theme-logo').forEach(logo => {
            const nextSrc = isDark ? logo.dataset.logoDark : logo.dataset.logoLight;
            if (nextSrc && !logo.src.endsWith(nextSrc)) {
                logo.src = nextSrc;
            }
        });

        const favicon = document.getElementById('appFavicon');
        if (favicon) {
            const faviconSrc = isDark
                ? '/img/logo-fitcontrol-canva-transparent-dark.png'
                : '/img/logo-fitcontrol-canva-transparent-light.png';
            favicon.setAttribute('href', faviconSrc);
        }
    }

    if (sidebarPreference === 'collapsed') {
        body.classList.add('sidebar-collapsed');
    }

    if (themePreference === 'dark') {
        body.classList.add('dark-mode');
    }

    applyThemeLogo();

    if (isAuthenticated) {
        applyVisualPreferences(visualPreferences);
    }

    function closeMobileMenu() {
        body.classList.remove('mobile-sidebar-open');
    }

    sidebarToggle?.addEventListener('click', () => {
        if (!isAuthenticated) return;
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
        if (!isAuthenticated) return;
        body.classList.toggle('dark-mode');
        setCookie('fitcontrol-theme', body.classList.contains('dark-mode') ? 'dark' : 'light', 3650);
        applyThemeLogo();
    });

    document.addEventListener('fitcontrol:preferences-changed', (event) => {
        if (!isAuthenticated) return;
        const prefs = event.detail || {};
        if (prefs.theme) setCookie('fitcontrol-theme', prefs.theme, 3650);
        if (prefs.accent) setCookie('fitcontrol-accent', prefs.accent, 3650);
        if (prefs.background) setCookie('fitcontrol-background', prefs.background, 3650);
        if (prefs.fontScale) setCookie('fitcontrol-font-scale', prefs.fontScale, 3650);
        if (prefs.theme) {
            body.classList.toggle('dark-mode', prefs.theme === 'dark');
        }
        applyVisualPreferences(prefs);
        applyThemeLogo();
    });
})();
