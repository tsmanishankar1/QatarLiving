window.initializeSortable = (tableSelector, dotNetHelper) => {
    const tbody = document.querySelector(`${tableSelector} tbody`);
    console.log(tableSelector);
    console.log(tbody);
    if (!tbody) return;

    let dragStarted = false;
    // Store original rows in order
    const originalOrder = Array.from(tbody.children).map(row => row.cloneNode(true));

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

    // Attach reset function to window
    window.resetTableOrder = () => {
        tbody.innerHTML = ""; // Clear current
        originalOrder.forEach(row => tbody.appendChild(row.cloneNode(true)));
        updateSlotNumbers();
    };
};


window.initializeArticleSortable = (tableSelector, dotNetHelper) => {

    const table = document.querySelector(`#${tableSelector}`);
    if (!table) {
        console.error(`Table with ID '${tableSelector}' not found.`);
        return;
    }

    const tbody = table.querySelector("tbody");
    if (!tbody) {
        console.error("tbody not found in table.");
        return;
    }


    let dragStarted = false;
    // Store original rows in order
    const originalOrder = Array.from(tbody.children).map(row => row.cloneNode(true));

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

    // Attach reset function to window
    window.resetArticleTableOrder = () => {
        tbody.innerHTML = ""; // Clear current
        originalOrder.forEach(row => tbody.appendChild(row.cloneNode(true)));
        updateSlotNumbers();
    };

};
