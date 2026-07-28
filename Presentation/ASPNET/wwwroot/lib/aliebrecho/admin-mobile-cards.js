(function () {
    'use strict';

    const mobileQuery = window.matchMedia('(max-width: 767.98px)');
    let scheduled = false;

    const normalizeLabel = (value) => String(value || '')
        .replace(/\s+/g, ' ')
        .trim();

    const isActionLabel = (label) => {
        const normalized = label.toLocaleLowerCase('pt-BR');
        return normalized.includes('ação')
            || normalized.includes('acoes')
            || normalized.includes('ações')
            || normalized === 'action'
            || normalized === 'actions';
    };

    const getHeaderLabels = (table) => Array.from(table.querySelectorAll('thead th'))
        .map(header => normalizeLabel(header.innerText || header.textContent));

    const decoratePlainTable = (table) => {
        if (
            table.classList.contains('order-table')
            || table.closest('.modal, .swal2-container, .e-grid')
            || !table.closest('.content-wrapper')
        ) {
            return;
        }

        const labels = getHeaderLabels(table);
        if (!labels.length) {
            return;
        }

        table.classList.add('admin-mobile-card-table');

        table.querySelectorAll('tbody tr').forEach(row => {
            const cells = Array.from(row.children).filter(cell => cell.tagName === 'TD');

            cells.forEach((cell, index) => {
                if (cell.tagName !== 'TD') {
                    return;
                }

                const label = labels[index] || '';
                cell.dataset.mobileLabel = label;
                cell.classList.toggle('admin-mobile-card-actions', isActionLabel(label));
            });

            cells
                .filter(cell => !cell.hasAttribute('colspan'))
                .find(cell => normalizeLabel(cell.innerText || cell.textContent))
                ?.classList.add('admin-mobile-card-primary');
        });
    };

    const decorateSyncfusionGrid = (grid) => {
        if (!grid.closest('.content-wrapper')) {
            return;
        }

        const labels = Array.from(grid.querySelectorAll('.e-gridheader .e-headercell'))
            .map(header => normalizeLabel(header.innerText || header.textContent));

        if (!labels.length) {
            return;
        }

        grid.classList.add('admin-mobile-card-grid');
        grid.querySelectorAll('.e-gridcontent .e-row').forEach(row => {
            const cells = Array.from(row.querySelectorAll(':scope > .e-rowcell'));

            cells.forEach((cell, index) => {
                cell.dataset.mobileLabel = labels[index] || '';
                cell.classList.toggle('admin-mobile-card-actions', isActionLabel(labels[index]));
            });

            cells
                .find(cell => normalizeLabel(cell.innerText || cell.textContent))
                ?.classList.add('admin-mobile-card-primary');
        });
    };

    const decorate = () => {
        scheduled = false;
        if (!mobileQuery.matches) {
            return;
        }

        document.querySelectorAll('.content-wrapper table').forEach(decoratePlainTable);
        document.querySelectorAll('.content-wrapper .e-grid').forEach(decorateSyncfusionGrid);
    };

    const scheduleDecorate = () => {
        if (scheduled) {
            return;
        }

        scheduled = true;
        window.requestAnimationFrame(decorate);
    };

    document.addEventListener('DOMContentLoaded', () => {
        scheduleDecorate();

        const content = document.querySelector('.content-wrapper');
        if (!content) {
            return;
        }

        new MutationObserver(scheduleDecorate).observe(content, {
            childList: true,
            subtree: true
        });
    });

    window.addEventListener('load', scheduleDecorate);
    mobileQuery.addEventListener('change', scheduleDecorate);
})();
