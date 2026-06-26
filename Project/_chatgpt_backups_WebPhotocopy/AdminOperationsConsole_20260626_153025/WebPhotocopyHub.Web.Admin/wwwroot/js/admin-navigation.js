(() => {
    'use strict';

    const body = document.body;
    const header = document.querySelector('.adminref-header');
    const main = document.querySelector('.adminref-main');
    const groups = Array.from(document.querySelectorAll('.adminref-nav-group'));
    let lastTrigger = null;

    const closeGroup = (group) => {
        const trigger = group.querySelector('.adminref-nav-trigger');
        group.classList.remove('is-open');
        if (trigger) {
            trigger.setAttribute('aria-expanded', 'false');
        }
    };

    const closeAll = (exceptGroup = null) => {
        groups.forEach((group) => {
            if (group !== exceptGroup) {
                closeGroup(group);
            }
        });
    };

    const openGroup = (group, focusTarget = false) => {
        const trigger = group.querySelector('.adminref-nav-trigger');
        const panel = group.querySelector('.adminref-nav-dropdown');
        if (!trigger || !panel) {
            return;
        }

        closeAll(group);
        group.classList.add('is-open');
        trigger.setAttribute('aria-expanded', 'true');
        lastTrigger = trigger;

        if (focusTarget) {
            const firstLink = panel.querySelector('a:not([hidden])');
            if (firstLink) {
                firstLink.focus();
            }
        }
    };

    groups.forEach((group) => {
        const trigger = group.querySelector('.adminref-nav-trigger');
        const panel = group.querySelector('.adminref-nav-dropdown');
        if (!trigger || !panel) {
            return;
        }

        const links = () => Array.from(panel.querySelectorAll('a:not([hidden])'));

        trigger.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();

            if (group.classList.contains('is-open')) {
                closeGroup(group);
                return;
            }

            openGroup(group);
        });

        trigger.addEventListener('keydown', (event) => {
            if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openGroup(group, true);
            }
        });

        panel.addEventListener('click', (event) => {
            event.stopPropagation();
        });

        panel.addEventListener('keydown', (event) => {
            const items = links();
            if (items.length === 0) {
                return;
            }

            const currentIndex = items.indexOf(document.activeElement);
            let targetIndex = currentIndex;

            if (event.key === 'ArrowDown') {
                targetIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
            }

            if (event.key === 'ArrowUp') {
                targetIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
            }

            if (event.key === 'Home') {
                targetIndex = 0;
            }

            if (event.key === 'End') {
                targetIndex = items.length - 1;
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                closeGroup(group);
                trigger.focus();
                return;
            }

            if (targetIndex !== currentIndex) {
                event.preventDefault();
                items[targetIndex].focus();
            }
        });
    });

    document.addEventListener('click', () => closeAll());

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') {
            return;
        }

        closeAll();
        if (lastTrigger) {
            lastTrigger.focus();
        }
    });

    window.addEventListener('resize', () => closeAll());

    const updateHeaderState = () => {
        if (header) {
            header.classList.toggle('is-scrolled', window.scrollY > 4);
        }
    };

    updateHeaderState();
    window.addEventListener('scroll', updateHeaderState, { passive: true });

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

        const title = normalizeTitle();
        const hero = document.createElement('section');
        hero.className = 'k-admin-generated-hero';
        hero.setAttribute('aria-labelledby', 'admin-generated-page-title');

        const copy = document.createElement('div');
        copy.className = 'k-admin-generated-hero-copy';

        const eyebrow = document.createElement('span');
        eyebrow.className = 'system-eyebrow';
        eyebrow.textContent = 'System administration';

        const heading = document.createElement('h1');
        heading.id = 'admin-generated-page-title';
        heading.textContent = title;

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

                    const label = headers[index] || 'Thông tin';
                    cell.setAttribute('data-label', label);
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

                const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
                submitButtons.forEach((button) => {
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
