window.initializeSortable = (tableSelector, dotNetHelper) => {
    const tbody = document.querySelector(`${tableSelector} tbody`);
    if (!tbody) return;

    let dragStarted = false;

    const updateSlotNumbers = () => {
        tbody.querySelectorAll("tr").forEach((tr, index) => {
            const slotCell = tr.querySelector(".slot-position");
            if (slotCell) {
                slotCell.textContent = index + 1;
            }
        });
    };

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

            updateSlotNumbers(); // Update the visible row numbers after drag

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

    updateSlotNumbers(); // Set initial numbers on load
};


window.initializeLiveSortable = (tableSelector, dotNetHelper) => {
    var tbody = document.querySelector(`live-slots`);

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
