function viewPdf(pdfUrl, canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error(`Canvas with id ${canvasId} not found.`);
        return;
    }

    const context = canvas.getContext('2d');

    pdfjsLib.getDocument(pdfUrl).promise.then(pdf => {
        return pdf.getPage(1);
    }).then(page => {
        const viewport = page.getViewport({ scale: 1.5 });
        canvas.height = viewport.height;
        canvas.width = viewport.width;

        const renderContext = {
            canvasContext: context,
            viewport: viewport
        };
        page.render(renderContext);
    }).catch(error => {
        console.error("Error rendering PDF:", error);
    });
}