  function initSortable(dotNetHelper) {
        const container = document.getElementById('image-upload-container');
        new Sortable(container, {
            animation: 150,
            draggable: ".upload-box",
            onEnd: function (evt) {
                const newOrder = Array.from(container.children)
                    .filter(el => el.dataset.id)
                    .map(el => el.dataset.id);
                dotNetHelper.invokeMethodAsync('UpdateOrder', newOrder);
            }
        });
    }