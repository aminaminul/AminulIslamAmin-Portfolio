(function () {
    var root = document.documentElement;
    root.classList.add("js");

    var stored = null;
    try {
        stored = window.localStorage.getItem("portfolio-theme");
    } catch (e) {
        stored = null;
    }

    var preference = root.getAttribute("data-theme-preference") || "system";
    var prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    var theme = stored === "light" || stored === "dark"
        ? stored
        : (preference === "dark" || (preference === "system" && prefersDark) ? "dark" : "light");

    root.setAttribute("data-theme", theme);
    root.style.colorScheme = theme;
})();
