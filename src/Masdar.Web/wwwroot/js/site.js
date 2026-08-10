(() => {
    "use strict";

    const root = document.documentElement;
    const themeStorageKey = "masdar-theme";
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const preferredDark = window.matchMedia("(prefers-color-scheme: dark)");
    let activeDrawer = null;
    let activeDrawerTrigger = null;
    let drawerBackdrop = null;

    const iconMarkup = (name) =>
        `<svg aria-hidden="true"><use href="#icon-${name}"></use></svg>`;

    function storedTheme() {
        try {
            return localStorage.getItem(themeStorageKey);
        } catch (_) {
            return null;
        }
    }

    function setTheme(theme, persist = true) {
        const nextTheme = theme === "dark" ? "dark" : "light";
        root.dataset.theme = nextTheme;

        document.querySelectorAll("[data-theme-toggle]").forEach((toggle) => {
            const darkIsActive = nextTheme === "dark";
            toggle.setAttribute("aria-pressed", String(darkIsActive));
            toggle.setAttribute(
                "aria-label",
                darkIsActive ? "Switch to light theme" : "Switch to dark theme"
            );
            toggle.title = darkIsActive ? "Switch to light theme" : "Switch to dark theme";
        });

        const themeMeta = document.querySelector('meta[name="theme-color"]');
        if (themeMeta) {
            themeMeta.content = nextTheme === "dark" ? "#050a15" : "#081126";
        }

        if (persist) {
            try {
                localStorage.setItem(themeStorageKey, nextTheme);
            } catch (_) {
                // A blocked storage API should not prevent theme switching.
            }
        }
    }

    function removeToast(toast) {
        if (!toast || toast.dataset.removing === "true") return;
        toast.dataset.removing = "true";
        toast.classList.add("is-leaving");
        window.setTimeout(() => toast.remove(), reduceMotion.matches ? 0 : 190);
    }

    function initializeToast(toast) {
        const closeButton = toast.querySelector("[data-toast-close]");
        closeButton?.addEventListener("click", () => removeToast(toast));

        const dismissAfter = Number.parseInt(toast.dataset.autoDismiss || "", 10);
        if (!Number.isFinite(dismissAfter) || dismissAfter <= 0) return;

        let timeoutId = null;
        let remaining = dismissAfter;
        let startedAt = 0;

        const startTimer = () => {
            startedAt = Date.now();
            timeoutId = window.setTimeout(() => removeToast(toast), remaining);
        };
        const pauseTimer = () => {
            if (timeoutId === null) return;
            window.clearTimeout(timeoutId);
            timeoutId = null;
            remaining -= Date.now() - startedAt;
        };
        const resumeTimer = () => {
            if (timeoutId !== null || remaining <= 0) return;
            startTimer();
        };

        toast.addEventListener("mouseenter", pauseTimer);
        toast.addEventListener("mouseleave", resumeTimer);
        toast.addEventListener("focusin", pauseTimer);
        toast.addEventListener("focusout", resumeTimer);
        startTimer();
    }

    function showToast(message, type = "info", dismissAfter = 5200) {
        if (!message) return null;

        const typeMap = {
            success: { className: "toast-success", icon: "check", role: "status" },
            error: { className: "toast-error", icon: "alert", role: "alert" },
            warning: { className: "toast-warning", icon: "alert", role: "status" },
            info: { className: "toast-info", icon: "info", role: "status" }
        };
        const config = typeMap[type] || typeMap.info;
        let stack = document.querySelector(".toast-stack");

        if (!stack) {
            stack = document.createElement("div");
            stack.className = "toast-stack";
            stack.setAttribute("aria-live", "polite");
            stack.setAttribute("aria-atomic", "false");
            document.body.append(stack);
        }

        const toast = document.createElement("div");
        toast.className = `masdar-toast ${config.className}`;
        toast.setAttribute("role", config.role);
        if (dismissAfter > 0) toast.dataset.autoDismiss = String(dismissAfter);
        toast.innerHTML = `${iconMarkup(config.icon)}<span></span><button type="button" data-toast-close aria-label="Dismiss message">${iconMarkup("x")}</button>`;
        toast.querySelector("span").textContent = message;
        stack.append(toast);
        initializeToast(toast);
        return toast;
    }

    function focusableElements(container) {
        return Array.from(
            container.querySelectorAll(
                'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
            )
        ).filter((element) => !element.hidden && element.offsetParent !== null);
    }

    function ensureDrawerBackdrop() {
        if (drawerBackdrop) return drawerBackdrop;
        drawerBackdrop = document.querySelector(".panel-backdrop, .drawer-backdrop");
        if (!drawerBackdrop) {
            drawerBackdrop = document.createElement("div");
            drawerBackdrop.className = "panel-backdrop";
            drawerBackdrop.hidden = true;
            document.body.append(drawerBackdrop);
        }
        drawerBackdrop.addEventListener("click", closeDrawer);
        return drawerBackdrop;
    }

    function openDrawer(drawer, trigger = null) {
        if (!drawer) return;
        if (activeDrawer && activeDrawer !== drawer) {
            activeDrawer.classList.remove("open");
            activeDrawer.hidden = true;
            activeDrawer.setAttribute("aria-hidden", "true");
            activeDrawerTrigger?.setAttribute("aria-expanded", "false");
        }

        activeDrawer = drawer;
        activeDrawerTrigger = trigger || document.activeElement;
        const backdrop = ensureDrawerBackdrop();

        drawer.hidden = false;
        drawer.setAttribute("aria-hidden", "false");
        if (!drawer.hasAttribute("role")) drawer.setAttribute("role", "dialog");
        if (!drawer.hasAttribute("aria-modal")) drawer.setAttribute("aria-modal", "true");
        backdrop.hidden = false;

        window.requestAnimationFrame(() => {
            drawer.classList.add("open");
            backdrop.classList.add("open");
        });

        document.body.classList.add("panel-open");
        trigger?.setAttribute("aria-expanded", "true");

        const focusTarget = drawer.querySelector("[autofocus]") || focusableElements(drawer)[0] || drawer;
        if (focusTarget === drawer && !drawer.hasAttribute("tabindex")) drawer.tabIndex = -1;
        window.setTimeout(() => focusTarget.focus({ preventScroll: true }), reduceMotion.matches ? 0 : 225);
    }

    function closeDrawer(restoreFocus = true) {
        if (!activeDrawer) return;
        const drawer = activeDrawer;
        const trigger = activeDrawerTrigger;
        const backdrop = ensureDrawerBackdrop();

        drawer.classList.remove("open");
        drawer.setAttribute("aria-hidden", "true");
        backdrop.classList.remove("open");
        document.body.classList.remove("panel-open");
        trigger?.setAttribute("aria-expanded", "false");

        activeDrawer = null;
        activeDrawerTrigger = null;
        window.setTimeout(() => {
            drawer.hidden = true;
            backdrop.hidden = true;
        }, reduceMotion.matches ? 0 : 225);

        if (restoreFocus && trigger instanceof HTMLElement) {
            trigger.focus({ preventScroll: true });
        }
    }

    function handleDrawerKeydown(event) {
        if (!activeDrawer) return;
        if (event.key === "Escape") {
            event.preventDefault();
            closeDrawer();
            return;
        }
        if (event.key !== "Tab") return;

        const focusable = focusableElements(activeDrawer);
        if (focusable.length === 0) {
            event.preventDefault();
            activeDrawer.focus();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function resolveTarget(selectorOrId) {
        if (!selectorOrId) return null;
        try {
            return document.querySelector(selectorOrId.startsWith("#") ? selectorOrId : `#${selectorOrId}`);
        } catch (_) {
            return null;
        }
    }

    function initializeInteractions() {
        setTheme(root.dataset.theme || (preferredDark.matches ? "dark" : "light"), false);

        document.querySelectorAll("[data-theme-toggle]").forEach((toggle) => {
            toggle.addEventListener("click", () => {
                setTheme(root.dataset.theme === "dark" ? "light" : "dark");
            });
        });

        document.querySelectorAll(".masdar-toast").forEach(initializeToast);

        document.querySelectorAll("[data-drawer-target]").forEach((trigger) => {
            const target = resolveTarget(trigger.dataset.drawerTarget);
            if (target) {
                trigger.setAttribute("aria-controls", target.id);
                trigger.setAttribute("aria-expanded", "false");
            }
            trigger.addEventListener("click", () => openDrawer(target, trigger));
        });

        document.querySelectorAll("[data-drawer-close]").forEach((button) => {
            button.addEventListener("click", () => closeDrawer());
        });

        document.addEventListener("keydown", handleDrawerKeydown);

        document.querySelectorAll("[data-confirm]").forEach((element) => {
            element.addEventListener("click", (event) => {
                const message = element.dataset.confirm || "Are you sure you want to continue?";
                if (!window.confirm(message)) event.preventDefault();
            });
        });

        document.querySelectorAll("select[data-auto-submit], input[data-auto-submit]").forEach((control) => {
            control.addEventListener("change", () => control.form?.requestSubmit());
        });

        document.querySelectorAll("input[type='file'][data-file-input]").forEach((input) => {
            const output = resolveTarget(input.dataset.fileName || "") ||
                input.closest("form")?.querySelector("[data-file-name]");
            input.addEventListener("change", () => {
                if (!output) return;
                const files = Array.from(input.files || []);
                output.textContent = files.length === 0
                    ? output.dataset.emptyText || "No file selected"
                    : files.map((file) => file.name).join(", ");
            });
        });

        document.querySelectorAll("[data-password-toggle]").forEach((button) => {
            const input = resolveTarget(button.dataset.passwordToggle);
            if (!(input instanceof HTMLInputElement)) return;
            button.addEventListener("click", () => {
                const reveal = input.type === "password";
                input.type = reveal ? "text" : "password";
                button.setAttribute("aria-pressed", String(reveal));
                button.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
            });
        });

        document.querySelectorAll("form").forEach((form) => {
            form.addEventListener("submit", () => {
                form.classList.add("is-submitting");
                form.setAttribute("aria-busy", "true");
                form.querySelectorAll("[data-disable-on-submit]").forEach((control) => {
                    control.disabled = true;
                });
            });
        });

        const validationSummary = document.querySelector(".validation-summary-errors");
        if (validationSummary) {
            validationSummary.setAttribute("role", "alert");
            validationSummary.tabIndex = -1;
        }

        if (window.bootstrap?.Tooltip) {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((element) => {
                window.bootstrap.Tooltip.getOrCreateInstance(element);
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeInteractions, { once: true });
    } else {
        initializeInteractions();
    }

    preferredDark.addEventListener?.("change", (event) => {
        if (!storedTheme()) setTheme(event.matches ? "dark" : "light", false);
    });

    window.MasdarUI = Object.freeze({
        closeDrawer,
        openDrawer,
        setTheme,
        showToast
    });
})();
