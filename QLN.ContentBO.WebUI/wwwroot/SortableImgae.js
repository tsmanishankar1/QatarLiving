window.initSortable = (elementId, dotnetHelper) => {
    const el = document.getElementById(elementId);
    if (!el) return;

    Sortable.create(el, {
        animation: 150,
        onEnd: function (evt) {
            dotnetHelper.invokeMethodAsync("OnReorder", evt.oldIndex, evt.newIndex);
        }
    });
};
