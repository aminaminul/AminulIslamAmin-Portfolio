(function () {
    "use strict";

    var sidebar = document.getElementById("admin-sidebar");
    var overlay = document.querySelector("[data-sidebar-overlay]");
    var openButton = document.querySelector("[data-sidebar-open]");
    var closeButton = document.querySelector("[data-sidebar-close]");

    function setSidebar(open) {
        if (!sidebar) return;
        sidebar.classList.toggle("is-open", open);
        if (overlay) overlay.hidden = !open;
        if (openButton) openButton.setAttribute("aria-expanded", open ? "true" : "false");
        document.body.style.overflow = open ? "hidden" : "";
    }

    if (openButton) openButton.addEventListener("click", function () { setSidebar(true); });
    if (closeButton) closeButton.addEventListener("click", function () { setSidebar(false); });
    if (overlay) overlay.addEventListener("click", function () { setSidebar(false); });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && sidebar && sidebar.classList.contains("is-open")) {
            setSidebar(false);
            if (openButton) openButton.focus();
        }
    });

    // Confirmation dialogs. Use capture so this runs before the submit-once handler.
    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm") || "Are you sure?";
            if (!window.confirm(message)) {
                event.preventDefault();
                event.stopImmediatePropagation();
                document.querySelectorAll("[data-submit-once]").forEach(function (button) {
                    button.classList.remove("is-loading");
                    button.removeAttribute("aria-busy");
                });
                document.querySelectorAll("form").forEach(function (f) { f.dataset.submitting = "false"; });
            }
        }, true);
    });

    // Optional unsaved-changes protection.
    document.querySelectorAll("form[data-track-dirty]").forEach(function (form) {
        var dirty = false;
        function markDirty() { dirty = true; }
        form.addEventListener("input", markDirty);
        form.addEventListener("change", markDirty);
        form.addEventListener("submit", function () { dirty = false; });

        window.addEventListener("beforeunload", function (event) {
            if (!dirty) return;
            event.preventDefault();
            event.returnValue = "";
        });
    });

    // Toasts: dismissible and auto-hiding (errors stay longer).
    document.querySelectorAll(".toast").forEach(function (toast) {
        var close = toast.querySelector("[data-toast-close]");
        var remove = function () {
            toast.style.opacity = "0";
            toast.style.transform = "translateY(-6px)";
            window.setTimeout(function () { toast.remove(); }, 160);
        };
        if (close) close.addEventListener("click", remove);
        var timeout = toast.classList.contains("error") ? 12000 : 6000;
        window.setTimeout(remove, timeout);
    });

    // Copy helper for media/public URLs.
    document.querySelectorAll("[data-copy]").forEach(function (button) {
        button.addEventListener("click", function () {
            var value = button.getAttribute("data-copy");
            if (!value) return;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(value).then(function () {
                    var original = button.getAttribute("aria-label");
                    button.setAttribute("aria-label", "Copied");
                    window.setTimeout(function () {
                        if (original) button.setAttribute("aria-label", original);
                    }, 1500);
                });
            }
        });
    });

    // Enable/disable end-date inputs when "current" is checked.
    document.querySelectorAll("[data-current-toggle]").forEach(function (checkbox) {
        var targetId = checkbox.getAttribute("data-current-toggle");
        var target = document.getElementById(targetId);
        if (!target) return;
        function sync() {
            target.disabled = checkbox.checked;
            if (checkbox.checked) target.value = "";
        }
        checkbox.addEventListener("change", sync);
        sync();
    });
})();
