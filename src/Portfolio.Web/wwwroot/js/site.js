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
    // Citation modal interactions (CSP-compliant, no inline scripts or onclick)
    var citationModal = document.getElementById("citation-modal");
    if (citationModal) {
        document.querySelectorAll("[data-open-citation]").forEach(function (button) {
            button.addEventListener("click", function () {
                if (typeof citationModal.showModal === "function") {
                    citationModal.showModal();
                }
            });
        });

        document.querySelectorAll("[data-close-citation]").forEach(function (button) {
            button.addEventListener("click", function () {
                if (typeof citationModal.close === "function") {
                    citationModal.close();
                }
            });
        });

        var bibPanel = document.getElementById("panel-bibtex");
        var apaPanel = document.getElementById("panel-apa");
        var bibTab = document.getElementById("btn-tab-bibtex");
        var apaTab = document.getElementById("btn-tab-apa");

        document.querySelectorAll("[data-citation-tab]").forEach(function (tab) {
            tab.addEventListener("click", function () {
                var type = tab.getAttribute("data-citation-tab");
                if (type === "bibtex") {
                    if (bibPanel) bibPanel.style.display = "block";
                    if (apaPanel) apaPanel.style.display = "none";
                    if (bibTab) bibTab.classList.add("is-active");
                    if (apaTab) apaTab.classList.remove("is-active");
                } else {
                    if (bibPanel) bibPanel.style.display = "none";
                    if (apaPanel) apaPanel.style.display = "block";
                    if (apaTab) apaTab.classList.add("is-active");
                    if (bibTab) bibTab.classList.remove("is-active");
                }
            });
        });

        var copyBtn = document.getElementById("btn-copy-citation");
        if (copyBtn) {
            copyBtn.addEventListener("click", function () {
                var codeEl = (apaPanel && apaPanel.style.display !== "none")
                    ? document.getElementById("code-apa")
                    : document.getElementById("code-bibtex");
                if (!codeEl) return;
                var text = codeEl.textContent || codeEl.innerText;
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(text).then(function () {
                        var label = document.getElementById("copy-citation-text");
                        if (!label) return;
                        var original = label.textContent;
                        label.textContent = copyBtn.getAttribute("data-copied-label") || "Copied!";
                        setTimeout(function () { label.textContent = original; }, 2000);
                    });
                }
            });
        }
    }
})();
