window.pasteImageHandler = {
    register: function (elementId, dotNetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        el.addEventListener('paste', function (event) {
            const items = (event.clipboardData || event.originalEvent.clipboardData).items;

            for (const item of items) {
                if (item.type.indexOf("image") === 0) {
                    const blob = item.getAsFile();
                    const reader = new FileReader();
                    reader.onload = function (event) {
                        dotNetRef.invokeMethodAsync("OnImagePasted", event.target.result);
                    };
                    reader.readAsDataURL(blob);
                }
            }
        });
    }
};
