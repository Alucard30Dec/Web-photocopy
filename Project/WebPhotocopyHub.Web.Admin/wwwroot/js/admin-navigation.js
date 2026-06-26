(() => {
    'use strict';

    const body = document.body;
    const shell = document.querySelector('[data-admin-mega-shell]');
    const nav = document.querySelector('[data-admin-mega-nav]');
    const navToggle = document.querySelector('[data-admin-mega-nav-toggle]');
    const closeTargets = Array.from(document.querySelectorAll('[data-admin-mega-close]'));
    const items = Array.from(document.querySelectorAll('[data-admin-mega-item]'));
    const main = document.querySelector('.adminops-main');
    const desktopQuery = window.matchMedia('(min-width: 1101px)');

    if (!shell || !nav) {
        return;
    }

    const getTrigger = (item) => item.querySelector('[data-admin-mega-trigger]');
    const getPanel = (item) => item.querySelector('[data-admin-mega-panel]');

    const setNavToggleState = (expanded) => {
        if (!navToggle) {
            return;
        }

        navToggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
    };

    const closeItem = (item, restoreFocus = false) => {
        const trigger = getTrigger(item);
        const panel = getPanel(item);

        item.classList.remove('is-open');

        if (trigger) {
            trigger.setAttribute('aria-expanded', 'false');
        }

        if (panel) {
            panel.hidden = true;
        }

        if (restoreFocus && trigger) {
            trigger.focus();
        }
    };

    const closeAllItems = (exceptItem = null) => {
        items.forEach((item) => {
            if (item !== exceptItem) {
                closeItem(item);
            }
        });
    };

    const openItem = (item, focusFirstLink = false) => {
        const trigger = getTrigger(item);
        const panel = getPanel(item);

        if (!trigger || !panel) {
            return;
        }

        closeAllItems(item);
        item.classList.add('is-open');
        trigger.setAttribute('aria-expanded', 'true');
        panel.hidden = false;

        if (focusFirstLink) {
            window.requestAnimationFrame(() => {
                const firstLink = panel.querySelector('a[href]');
                if (firstLink) {
                    firstLink.focus();
                }
            });
        }
    };

    const closeMobileNav = () => {
        body.classList.remove('adminmega-nav-open');
        setNavToggleState(false);
        closeAllItems();
    };

    const toggleMobileNav = () => {
        const shouldOpen = !body.classList.contains('adminmega-nav-open');
        body.classList.toggle('adminmega-nav-open', shouldOpen);
        setNavToggleState(shouldOpen);

        if (!shouldOpen) {
            closeAllItems();
        }
    };

    items.forEach((item) => {
        const trigger = getTrigger(item);
        const panel = getPanel(item);

        if (!trigger || !panel) {
            return;
        }

        trigger.addEventListener('click', () => {
            const isOpen = item.classList.contains('is-open');

            if (isOpen) {
                closeItem(item);
                return;
            }

            openItem(item);
        });

        trigger.addEventListener('keydown', (event) => {
            if (event.key === 'ArrowDown') {
                event.preventDefault();
                openItem(item, true);
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                closeItem(item, true);
            }
        });

        panel.addEventListener('keydown', (event) => {
            if (event.key !== 'Escape') {
                return;
            }

            event.preventDefault();
            closeItem(item, true);
        });
    });

    if (navToggle) {
        navToggle.addEventListener('click', toggleMobileNav);
    }

    closeTargets.forEach((target) => {
        target.addEventListener('click', closeMobileNav);
    });

    const topLevelControls = Array.from(
        nav.querySelectorAll(':scope > .adminmega-nav-link, :scope > .adminmega-nav-item > .adminmega-nav-trigger')
    );

    topLevelControls.forEach((control, index) => {
        control.addEventListener('keydown', (event) => {
            if (!desktopQuery.matches || topLevelControls.length === 0) {
                return;
            }

            let nextIndex = -1;

            if (event.key === 'ArrowRight') {
                nextIndex = (index + 1) % topLevelControls.length;
            }

            if (event.key === 'ArrowLeft') {
                nextIndex = (index - 1 + topLevelControls.length) % topLevelControls.length;
            }

            if (event.key === 'Home') {
                nextIndex = 0;
            }

            if (event.key === 'End') {
                nextIndex = topLevelControls.length - 1;
            }

            if (nextIndex < 0) {
                return;
            }

            event.preventDefault();
            topLevelControls[nextIndex].focus();
        });
    });

    nav.querySelectorAll('a[href]').forEach((link) => {
        link.addEventListener('click', () => {
            if (!desktopQuery.matches) {
                closeMobileNav();
            }
        });
    });

    document.addEventListener('click', (event) => {
        if (!desktopQuery.matches || nav.contains(event.target)) {
            return;
        }

        closeAllItems();
    });

    document.addEventListener('focusin', (event) => {
        if (!desktopQuery.matches || nav.contains(event.target)) {
            return;
        }

        closeAllItems();
    });

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') {
            return;
        }

        if (body.classList.contains('adminmega-nav-open')) {
            closeMobileNav();

            if (navToggle) {
                navToggle.focus();
            }

            return;
        }

        closeAllItems();
    });

    const handleViewportChange = () => {
        closeAllItems();

        if (desktopQuery.matches) {
            body.classList.remove('adminmega-nav-open');
            setNavToggleState(false);
        }
    };

    if (typeof desktopQuery.addEventListener === 'function') {
        desktopQuery.addEventListener('change', handleViewportChange);
    }

    if (typeof desktopQuery.addEventListener !== 'function'
        && typeof desktopQuery.addListener === 'function') {
        desktopQuery.addListener(handleViewportChange);
    }

    const normalizeTitle = () => {
        const rawTitle = document.title || 'Quản trị hệ thống';
        return rawTitle.split(' - ')[0].trim() || 'Quản trị hệ thống';
    };

    const hasPageHeading = () => {
        if (!main) {
            return true;
        }

        return Boolean(main.querySelector(
            ':scope > .system-page-hero, ' +
            ':scope > .page-hero, ' +
            ':scope > .adminops-dashboard-header, ' +
            ':scope > .adminref-dashboard-hero, ' +
            ':scope > .branch-admin-page > .branch-admin-heading, ' +
            ':scope > .branch-admin-form-page > .branch-admin-heading, ' +
            ':scope > .admin-login-shell, ' +
            ':scope > .k-admin-generated-hero'
        ));
    };

    const insertGeneratedHeading = () => {
        if (!main || hasPageHeading()) {
            return;
        }

        const hero = document.createElement('section');
        hero.className = 'k-admin-generated-hero adminops-generated-hero';
        hero.setAttribute('aria-labelledby', 'admin-generated-page-title');

        const copy = document.createElement('div');
        copy.className = 'k-admin-generated-hero-copy';

        const eyebrow = document.createElement('span');
        eyebrow.className = 'adminops-eyebrow';
        eyebrow.textContent = 'System administration';

        const heading = document.createElement('h1');
        heading.id = 'admin-generated-page-title';
        heading.textContent = normalizeTitle();

        const description = document.createElement('p');
        description.textContent = 'Quản lý dữ liệu và thao tác nghiệp vụ trong phạm vi quyền quản trị hiện tại.';

        copy.append(eyebrow, heading, description);
        hero.append(copy);

        const firstContent = Array.from(main.children).find((element) => !element.classList.contains('alert'));
        if (firstContent) {
            main.insertBefore(hero, firstContent);
            return;
        }

        main.append(hero);
    };

    const decorateTables = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('table').forEach((table) => {
            table.classList.add('k-admin-grid');
            table.setAttribute('data-mobile-grid', 'true');

            const wrapper = table.closest('.table-responsive');
            if (wrapper) {
                wrapper.classList.add('k-admin-grid-wrap');
            }

            const headers = Array.from(table.querySelectorAll('thead th')).map((headerCell) =>
                headerCell.textContent.replace(/\s+/g, ' ').trim()
            );

            table.querySelectorAll('tbody tr').forEach((row) => {
                Array.from(row.children).forEach((cell, index) => {
                    if (cell.tagName !== 'TD' || cell.hasAttribute('colspan')) {
                        return;
                    }

                    cell.setAttribute('data-label', headers[index] || 'Thông tin');
                });
            });
        });
    };

    const decorateLooseSurfaces = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll(':scope > .table-responsive').forEach((element) => {
            element.classList.add('k-admin-surface');
        });
        main.querySelectorAll(':scope > .d-flex.justify-content-end.mb-3').forEach((element) => {
            element.classList.add('k-admin-toolbar');
        });
    };

    const improveFormFeedback = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('form[method="post"]').forEach((form) => {
            form.addEventListener('submit', () => {
                if (!form.checkValidity()) {
                    return;
                }

                form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach((button) => {
                    button.classList.add('k-admin-loading');
                    button.setAttribute('aria-busy', 'true');
                });
            });
        });
    };

    insertGeneratedHeading();
    decorateLooseSurfaces();
    decorateTables();
    improveFormFeedback();
    body.classList.add('admin-ui-ready');
})();
