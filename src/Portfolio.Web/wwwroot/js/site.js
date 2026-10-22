(function () {
    "use strict";

    var root = document.documentElement;
    var storageKey = "portfolio-theme";

    function currentTheme() {
        return root.getAttribute("data-theme") === "dark" ? "dark" : "light";
    }

    function applyTheme(theme, persist) {
        root.setAttribute("data-theme", theme);
        root.style.colorScheme = theme;
        if (persist) {
            try {
                window.localStorage.setItem(storageKey, theme);
            } catch (e) { /* private mode: theme still applies for this page */ }
        }
        document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
            button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
        });
    }

    document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
        button.setAttribute("aria-pressed", currentTheme() === "dark" ? "true" : "false");
        button.addEventListener("click", function () {
            applyTheme(currentTheme() === "dark" ? "light" : "dark", true);
        });
    });

    // Follow the operating system when the visitor has not chosen a theme.
    if (window.matchMedia) {
        var media = window.matchMedia("(prefers-color-scheme: dark)");
        var listener = function (event) {
            var stored = null;
            try {
                stored = window.localStorage.getItem(storageKey);
            } catch (e) { stored = null; }
            if (!stored) applyTheme(event.matches ? "dark" : "light", false);
        };
        if (media.addEventListener) media.addEventListener("change", listener);
    }

    // Mobile navigation
    var menuToggle = document.querySelector("[data-menu-toggle]");
    var mobileNav = document.getElementById("mobile-nav");
    if (menuToggle && mobileNav) {
        menuToggle.addEventListener("click", function () {
            var expanded = menuToggle.getAttribute("aria-expanded") === "true";
            menuToggle.setAttribute("aria-expanded", expanded ? "false" : "true");
            mobileNav.hidden = expanded;
            menuToggle.setAttribute("aria-label", expanded ? "Open menu" : "Close menu");
        });

        mobileNav.addEventListener("click", function (event) {
            if (event.target.closest("a")) {
                mobileNav.hidden = true;
                menuToggle.setAttribute("aria-expanded", "false");
                menuToggle.setAttribute("aria-label", "Open menu");
            }
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !mobileNav.hidden) {
                mobileNav.hidden = true;
                menuToggle.setAttribute("aria-expanded", "false");
                menuToggle.focus();
            }
        });
    }

    // Scroll reveal
    var revealItems = document.querySelectorAll("[data-reveal]");
    if (revealItems.length) {
        if ("IntersectionObserver" in window && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("is-visible");
                        observer.unobserve(entry.target);
                    }
                });
            }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });
            revealItems.forEach(function (item) { observer.observe(item); });
        } else {
            revealItems.forEach(function (item) { item.classList.add("is-visible"); });
        }
    }

    // Prevent accidental duplicate submissions and show progress.
    document.querySelectorAll("form").forEach(function (form) {
        var button = form.querySelector("[data-submit-once]");
        if (!button) return;
        form.addEventListener("submit", function (event) {
            if (form.dataset.submitting === "true") {
                event.preventDefault();
                return;
            }
            if (!form.checkValidity || form.checkValidity()) {
                form.dataset.submitting = "true";
                button.classList.add("is-loading");
                button.setAttribute("aria-busy", "true");
                var label = button.querySelector(".btn-label");
                if (label) label.textContent = button.getAttribute("data-loading-label") || "Sending…";
            }
        });
    });

    // Page-level form state cleanup after successful navigation from bfcache.
    window.addEventListener("pageshow", function (event) {
        if (!event.persisted) return;
        document.querySelectorAll("[data-submit-once]").forEach(function (button) {
            button.classList.remove("is-loading");
            button.removeAttribute("aria-busy");
        });
    });
})();
