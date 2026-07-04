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

    const getTableMetrics = () => {
        if (!main) {
            return {
                actionCount: 0,
                columnCount: 0,
                rowCount: 0,
                tableCount: 0
            };
        }

        const tables = Array.from(main.querySelectorAll('table'));
        const rowCount = tables.reduce((total, table) => {
            const rows = Array.from(table.querySelectorAll('tbody tr'));
            return total + rows.filter((row) => !row.querySelector('td[colspan]')).length;
        }, 0);
        const columnCount = tables.reduce((total, table) =>
            Math.max(total, table.querySelectorAll('thead th').length), 0);
        const actionCount = main.querySelectorAll('td .btn, td form, .k-admin-toolbar .btn').length;
        const formCount = main.querySelectorAll('form').length;
        const fieldCount = main.querySelectorAll('.form-control, .form-select, textarea, input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"])').length;
        const cardCount = main.querySelectorAll('.card, .system-panel, .branch-admin-card, .adminops-panel').length;

        return {
            actionCount,
            cardCount,
            columnCount,
            fieldCount,
            formCount,
            rowCount,
            tableCount: tables.length
        };
    };

    const makeMetric = (label, value, tone = '') => {
        const metric = document.createElement('span');
        metric.className = `k-admin-hero-metric ${tone}`.trim();
        metric.innerHTML = `<strong>${value}</strong><small>${label}</small>`;
        return metric;
    };

    const decorateHeroSummary = () => {
        if (!main) {
            return;
        }

        const hero = main.querySelector(
            ':scope > .k-admin-generated-hero, ' +
            ':scope > .system-page-hero, ' +
            ':scope > .branch-admin-heading'
        );

        if (!hero || hero.classList.contains('adminops-dashboard-header') || hero.querySelector('.k-admin-hero-panel')) {
            return;
        }

        // Codex 2026-07-04: Make each Admin screen visibly command-focused without changing Razor business logic.
        const metrics = getTableMetrics();
        hero.classList.add('k-admin-command-hero');

        const panel = document.createElement('div');
        panel.className = 'k-admin-hero-panel';

        if (metrics.tableCount > 0) {
            panel.append(
                makeMetric('Bảng', metrics.tableCount, 'is-info'),
                makeMetric('Dòng dữ liệu', metrics.rowCount, 'is-success'),
                makeMetric('Thao tác', metrics.actionCount, 'is-warning')
            );
        } else {
            panel.append(
                makeMetric('Form', metrics.formCount || 1, 'is-info'),
                makeMetric('Trường nhập', metrics.fieldCount, 'is-success'),
                makeMetric('Khối nội dung', metrics.cardCount || 1, 'is-warning')
            );
        }

        hero.append(panel);
    };

    const decoratePageHeroes = () => {
        if (!main) {
            return;
        }

        // Codex 2026-07-04: Keep legacy admin page heroes visually aligned with the new admin shell.
        main.querySelectorAll(':scope > .page-hero').forEach((hero, index) => {
            hero.classList.add('system-page-hero', 'compact', 'adminops-page-hero');

            const heading = hero.querySelector('h1, h2, h3, h4');
            if (!heading) {
                return;
            }

            if (!heading.id) {
                heading.id = `admin-page-hero-title-${index + 1}`;
            }

            hero.setAttribute('aria-labelledby', heading.id);
        });
    };

    const decorateTableStatusCells = (table) => {
        const statusMap = new Map([
            ['yes', ['Hoạt động', 'is-success']],
            ['no', ['Tắt', 'is-neutral']],
            ['active', ['Hoạt động', 'is-success']],
            ['inactive', ['Tạm dừng', 'is-neutral']],
            ['true', ['Có', 'is-success']],
            ['false', ['Không', 'is-neutral']],
            ['pending', ['Chờ xử lý', 'is-warning']],
            ['approved', ['Đã duyệt', 'is-success']],
            ['rejected', ['Từ chối', 'is-danger']],
            ['completed', ['Hoàn tất', 'is-success']],
            ['cancelled', ['Đã hủy', 'is-neutral']]
        ]);

        table.querySelectorAll('tbody td').forEach((cell) => {
            if (cell.children.length > 0 || cell.hasAttribute('colspan')) {
                return;
            }

            const rawText = cell.textContent.replace(/\s+/g, ' ').trim();
            const normalized = rawText.toLowerCase();
            if (!statusMap.has(normalized)) {
                return;
            }

            const [label, tone] = statusMap.get(normalized);
            const pill = document.createElement('span');
            pill.className = `k-admin-status-pill ${tone}`;
            pill.textContent = label;

            cell.classList.add('k-admin-status-cell');
            cell.textContent = '';
            cell.append(pill);
        });
    };

    const decorateTableHeaders = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('.k-admin-grid-wrap').forEach((wrapper, index) => {
            const table = wrapper.querySelector('table');
            if (!table || wrapper.previousElementSibling?.classList.contains('k-admin-table-head')) {
                return;
            }

            const rowCount = Array.from(table.querySelectorAll('tbody tr'))
                .filter((row) => !row.querySelector('td[colspan]')).length;
            const columnCount = table.querySelectorAll('thead th').length;
            const actionCount = table.querySelectorAll('tbody td .btn, tbody td form').length;
            const title = index === 0 ? normalizeTitle() : `Bảng dữ liệu ${index + 1}`;
            const toolbar = wrapper.previousElementSibling?.classList.contains('k-admin-toolbar')
                ? wrapper.previousElementSibling
                : null;
            const head = document.createElement('div');

            head.className = 'k-admin-table-head';
            head.innerHTML = `
                <div class="k-admin-table-title">
                    <span>Dữ liệu quản trị</span>
                    <strong>${title}</strong>
                </div>
                <div class="k-admin-table-meta">
                    <span>${rowCount} dòng</span>
                    <span>${columnCount} cột</span>
                    <span>${actionCount} thao tác</span>
                </div>
            `;

            if (toolbar) {
                wrapper.parentNode.insertBefore(head, toolbar);
                head.append(toolbar);
            } else {
                wrapper.parentNode.insertBefore(head, wrapper);
            }

            wrapper.classList.add('k-admin-grid-wrap-with-head');
        });
    };

    const decorateCards = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('.card').forEach((card) => {
            card.classList.add('k-admin-card');

            const body = card.querySelector(':scope > .card-body');
            if (!body) {
                return;
            }

            const title = body.querySelector(':scope > h1:first-child, :scope > h2:first-child, :scope > h3:first-child, :scope > h4:first-child, :scope > h5:first-child');
            if (title) {
                title.classList.add('k-admin-card-title');
            }

            if (title?.nextElementSibling?.tagName === 'P') {
                title.nextElementSibling.classList.add('k-admin-card-note');
            }
        });
    };

    const decorateForms = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('form').forEach((form) => {
            const isTableForm = Boolean(form.closest('table'));

            if (isTableForm) {
                form.classList.add('k-admin-inline-form');
            } else {
                form.classList.add('k-admin-form');
            }

            form.querySelectorAll('.mb-3, .mb-2, .col-12, .col-sm-6, .col-md-3, .col-md-4, .col-md-6, .col-lg-4, .col-lg-6').forEach((field) => {
                if (field.querySelector('.form-control, .form-select, .form-check-input, textarea, select, input:not([type="hidden"])')) {
                    field.classList.add('k-admin-field');
                }
            });

            if (form.closest('td') && form.querySelector('.form-control, .form-select')) {
                form.classList.add('k-admin-review-form');
            }
        });
    };

    const decorateActionCells = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('td').forEach((cell) => {
            if (!cell.querySelector('.btn, form')) {
                return;
            }

            cell.classList.add('k-admin-action-cell');

            cell.querySelectorAll(':scope > .d-flex, :scope > form, :scope > a.btn').forEach((element) => {
                element.classList.add('k-admin-row-actions');
            });
        });

        main.querySelectorAll('td[colspan]').forEach((cell) => {
            if (cell.textContent.trim().length > 0) {
                cell.classList.add('k-admin-empty-cell');
            }
        });
    };

    const decorateTables = () => {
        if (!main) {
            return;
        }

        main.querySelectorAll('table').forEach((table) => {
            table.classList.add('k-admin-grid');
            table.setAttribute('data-mobile-grid', 'true');
            decorateTableStatusCells(table);

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

    decoratePageHeroes();
    insertGeneratedHeading();
    decorateLooseSurfaces();
    decorateCards();
    decorateForms();
    decorateActionCells();
    decorateTables();
    decorateHeroSummary();
    decorateTableHeaders();
    improveFormFeedback();
    body.classList.add('admin-ui-ready');
})();
