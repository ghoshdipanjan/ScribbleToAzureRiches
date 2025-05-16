// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $("#loading-spinner").show();

    // Theme switching logic
    const darkModeToggle = document.getElementById('darkModeToggle');
    const lightModeIcon = document.getElementById('lightModeIcon');
    const darkModeIcon = document.getElementById('darkModeIcon');
    const htmlElement = document.documentElement;

    // Function to update theme and icons
    function updateTheme(isDark) {
        htmlElement.setAttribute('data-bs-theme', isDark ? 'dark' : 'light');
        lightModeIcon.style.display = isDark ? 'none' : 'block';
        darkModeIcon.style.display = isDark ? 'block' : 'none';
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    }

    // Check system preference and stored preference
    const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
    const storedTheme = localStorage.getItem('theme');

    // Set initial theme
    if (storedTheme) {
        updateTheme(storedTheme === 'dark');
    } else {
        updateTheme(prefersDarkScheme.matches);
    }

    // Listen for theme toggle button clicks
    darkModeToggle.addEventListener('click', () => {
        const currentTheme = htmlElement.getAttribute('data-bs-theme');
        updateTheme(currentTheme === 'light');
    });

    // Listen for system theme changes
    prefersDarkScheme.addEventListener('change', (e) => {
        if (!localStorage.getItem('theme')) {
            updateTheme(e.matches);
        }
    });

    // Trigger a theme change event for other components to listen to
    function dispatchThemeChangeEvent() {
        const event = new CustomEvent('themeChanged', {
            detail: { theme: htmlElement.getAttribute('data-bs-theme') }
        });
        document.dispatchEvent(event);
    }

    // Listen for theme changes and dispatch event
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.attributeName === 'data-bs-theme') {
                dispatchThemeChangeEvent();
            }
        });
    });

    observer.observe(htmlElement, {
        attributes: true,
        attributeFilter: ['data-bs-theme']
    });
});

$(window).on("load", function () {
    $("#loading-spinner").hide();
});