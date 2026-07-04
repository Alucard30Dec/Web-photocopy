(() => {
    const menus = Array.from(document.querySelectorAll("[data-customer-menu]"));

    const closeMenu = (menu) => {
        const trigger = menu.querySelector(".customer-menu__trigger");
        menu.removeAttribute("data-open");
        trigger?.setAttribute("aria-expanded", "false");
    };

    const closeAll = (exceptMenu) => {
        menus.forEach((menu) => {
            if (menu !== exceptMenu) {
                closeMenu(menu);
            }
        });
    };

    // Codex 2026-07-04: Enable customer header comboboxes and notification panel under the app CSP using a local script.
    menus.forEach((menu) => {
        const trigger = menu.querySelector(".customer-menu__trigger");
        if (!trigger) {
            return;
        }

        trigger.addEventListener("click", (event) => {
            event.preventDefault();
            const isOpen = menu.hasAttribute("data-open");
            closeAll(menu);

            if (isOpen) {
                closeMenu(menu);
                return;
            }

            menu.setAttribute("data-open", "true");
            trigger.setAttribute("aria-expanded", "true");
        });
    });

    document.addEventListener("click", (event) => {
        if (event.target instanceof Element && !event.target.closest("[data-customer-menu]")) {
            closeAll(null);
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeAll(null);
        }
    });

    const mobileToggle = document.querySelector("[data-customer-mobile-toggle]");
    const mobilePanel = document.querySelector("[data-customer-mobile-panel]");
    const closeMobilePanel = () => {
        if (!mobilePanel || !mobileToggle) {
            return;
        }

        mobilePanel.hidden = true;
        mobilePanel.removeAttribute("data-open");
        mobileToggle.setAttribute("aria-expanded", "false");
    };

    if (mobileToggle && mobilePanel) {
        mobileToggle.addEventListener("click", () => {
            const nextOpen = !mobilePanel.hasAttribute("data-open");
            mobilePanel.hidden = !nextOpen;
            mobilePanel.toggleAttribute("data-open", nextOpen);
            mobileToggle.setAttribute("aria-expanded", String(nextOpen));
            closeAll(null);
        });

        mobilePanel.addEventListener("click", (event) => {
            if (event.target instanceof Element && event.target.closest("a")) {
                closeMobilePanel();
            }
        });
    }

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeMobilePanel();
        }
    });

    document.querySelectorAll("[data-customer-alert-close]").forEach((button) => {
        button.addEventListener("click", () => {
            button.closest(".customer-alert")?.remove();
        });
    });
})();
