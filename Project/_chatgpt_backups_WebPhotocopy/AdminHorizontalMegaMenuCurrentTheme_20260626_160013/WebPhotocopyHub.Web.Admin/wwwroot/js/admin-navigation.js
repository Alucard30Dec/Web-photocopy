(() => {
    'use strict';

    const body = document.body;
    const shell = document.querySelector('[data-admin-shell]');
    const sidebar = document.getElementById('adminops-sidebar');
    const main = document.querySelector('.adminops-main');
    const toggleButtons = Array.from(document.querySelectorAll('[data-admin-sidebar-toggle]'));
    const closeButtons = Array.from(document.querySelectorAll('[data-admin-sidebar-close]'));
    const navToggles = Array.from(document.querySelectorAll('[data-admin-nav-toggle]'));
    const desktopQuery = window.matchMedia('(min-width: 1101px)');
    const collapsedStorageKey = 'webphotocopy-admin-sidebar-collapsed';

    if (!shell || !sidebar) {
        return;
    }

    const setToggleState = (expanded) => {
        toggleButtons.forEach((button) => button.setAttribute('aria-expanded', expanded ? 'true' : 'false'));
    };

    const readCollapsedState = () => {
        try {
            return window.localStorage.getItem(collapsedStorageKey) === 'true';
        } catch {
            return false;
        }
    };

    const writeCollapsedState = (isCollapsed) => {
        try {
            window.localStorage.setItem(collapsedStorageKey, isCollapsed ? 'true' : 'false');
        } catch {
            // Local storage can be unavailable in restricted browser sessions.
        }
    };

    const closeMobileSidebar = () => {
        body.classList.remove('adminops-sidebar-open');
        setToggleState(false);
    };

    const applyResponsiveState = () => {
        if (desktopQuery.matches) {
            body.classList.remove('adminops-sidebar-open');
            body.classList.toggle('adminops-sidebar-collapsed', readCollapsedState());
            setToggleState(!body.classList.contains('adminops-sidebar-collapsed'));
            return;
        }

        body.classList.remove('adminops-sidebar-collapsed');
        setToggleState(body.classList.contains('adminops-sidebar-open'));
    };

    const toggleSidebar = () => {
        if (!desktopQuery.matches) {
            body.classList.toggle('adminops-sidebar-open');
            setToggleState(body.classList.contains('adminops-sidebar-open'));
            return;
        }

        const isCollapsed = !body.classList.contains('adminops-sidebar-collapsed');
        body.classList.toggle('adminops-sidebar-collapsed', isCollapsed);
        writeCollapsedState(isCollapsed);
        setToggleState(!isCollapsed);
    };

    toggleButtons.forEach((button) => button.addEventListener('click', toggleSidebar));
    closeButtons.forEach((button) => button.addEventListener('click', closeMobileSidebar));

    const closeNavGroup = (group) => {
        const trigger = group.querySelector('[data-admin-nav-toggle]');
        group.classList.remove('is-open');
        if (trigger) {
            trigger.setAttribute('aria-expanded', 'false');
        }
    };

    const openNavGroup = (group) => {
        const trigger = group.querySelector('[data-admin-nav-toggle]');
        group.classList.add('is-open');
        if (trigger) {
            trigger.setAttribute('aria-expanded', 'true');
        }
    };

    navToggles.forEach((trigger) => {
        trigger.addEventListener('click', () => {
            const group = trigger.closest('.adminops-nav-group');
            if (!group) {
                return;
            }

            if (desktopQuery.matches && body.classList.contains('adminops-sidebar-collapsed')) {
                body.classList.remove('adminops-sidebar-collapsed');
                writeCollapsedState(false);
                setToggleState(true);
                openNavGroup(group);
                return;
            }

            if (group.classList.contains('is-open')) {
                closeNavGroup(group);
                return;
            }

            openNavGroup(group);
        });
    });

    sidebar.querySelectorAll('a').forEach((link) => {
        link.addEventListener('click', () => {
            if (!desktopQuery.matches) {
                closeMobileSidebar();
            }
        });
    });

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') {
            return;
        }

        if (body.classList.contains('adminops-sidebar-open')) {
            closeMobileSidebar();
            const firstToggle = toggleButtons[0];
            if (firstToggle) {
                firstToggle.focus();
            }
        }
    });

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

        main.querySelectorAll(':scope > .table-responsive').forEach((element) => element.classList.add('k-admin-surface'));
        main.querySelectorAll(':scope > .d-flex.justify-content-end.mb-3').forEach((element) => element.classList.add('k-admin-toolbar'));
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

    applyResponsiveState();
    desktopQuery.addEventListener('change', applyResponsiveState);
    insertGeneratedHeading();
    decorateLooseSurfaces();
    decorateTables();
    improveFormFeedback();
    body.classList.add('admin-ui-ready');
})();
