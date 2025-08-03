window.pasteImageHandler = {
    register: function (elementId, dotNetRef) {
        try {
            if (!elementId || !dotNetRef) {
                throw new Error("Element ID and DotNet reference must be provided.");
            }

            const el = document.getElementById(elementId);
            if (!el) return;

            el.addEventListener('paste', function (event) {
                const items = (event.clipboardData || event.originalEvent.clipboardData).items;
                for (const item of items) {
                    // Only allow PNG or JPEG
                    if (item.type === "image/png" || item.type === "image/jpeg") {
                        console.log(item);
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

        catch (error) {
            console.error("Error in pasteImageHandler:", error);
            return;
        }
    }
};
