window.exportToExcel = (data, fileName = "export.xlsx", sheetName = "Sheet1") => {
    try {
        if (!data || !data.length) {
            alert("No data available to export.");
            return;
        }

        const worksheet = XLSX.utils.json_to_sheet(data);
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
        XLSX.writeFile(workbook, fileName);
    } catch (error) {
        console.error("Excel export error:", error);
        alert("An error occurred while exporting.");
    }
};
