window.initializeSortable = (tableSelector, dotNetHelper) => {
    const tbody = document.querySelector(`${tableSelector} tbody`);
    if (!tbody) return;

    let dragStarted = false;

    new Sortable(tbody, {
        animation: 150,
        handle: '.drag-handle',
        onStart: () => {
            dragStarted = true;
        },
        onEnd: () => {
            const newOrder = [];
            tbody.querySelectorAll("tr").forEach(tr => {
                newOrder.push(tr.getAttribute("data-slot-id"));
            });

            dotNetHelper.invokeMethodAsync('OnTableReordered', newOrder);
            setTimeout(() => dragStarted = false, 0);
        }
    });

    tbody.addEventListener('click', (e) => {
        if (dragStarted) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }
    }, true);
};
