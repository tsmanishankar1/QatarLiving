// SortableImgae.js
// This script initializes a sortable list using the Sortable.js library.
          window.initSortable = (elementId, dotNetHelper) => {
            const el = document.getElementById(elementId);
            if (!el) return;

            new Sortable(el, {
                animation: 150,
                onEnd: function (evt) {
                    const oldIndex = evt.oldIndex;
                    const newIndex = evt.newIndex;

                    // Call Blazor C# method
                    dotNetHelper.invokeMethodAsync('OnReorder', oldIndex, newIndex);
                }
            });
        };
