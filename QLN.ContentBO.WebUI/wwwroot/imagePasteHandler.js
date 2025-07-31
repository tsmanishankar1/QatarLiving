window.pasteImageHandler = {
    register: function (elementId, dotNetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        el.addEventListener('paste', function (event) {
            const items = (event.clipboardData || event.originalEvent.clipboardData).items;

            for (const item of items) {
                // Only allow PNG or JPEG
                if (item.type === "image/png" || item.type === "image/jpeg") {
                    const blob = item.getAsFile();
                    const reader = new FileReader();
                    reader.onload = function (event) {
                        dotNetRef.invokeMethodAsync("OnImagePasted", event.target.result);
                    };
                    reader.readAsDataURL(blob);
                    return; // Exit after handling the first image
                }
            }
        });
    }
};
