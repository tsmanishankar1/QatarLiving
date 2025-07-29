using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using QLN.ContentBO.WebUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IWebHostEnvironment _env;

        public ExcelExportService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> ExportAsync<T>(
            List<T> data,
            Dictionary<string, Func<T, object?>> columnSelectors,
            string sheetName = "Sheet1",
            string filePrefix = "Export"
        )
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Add headers
            int colIndex = 1;
            foreach (var column in columnSelectors.Keys)
            {
                worksheet.Cell(1, colIndex++).SetValue(column);
            }

            // Add rows
            for (int i = 0; i < data.Count; i++)
            {
                var rowIndex = i + 2;
                int cellIndex = 1;

                foreach (var selector in columnSelectors.Values)
                {
                    var value = selector(data[i]);
                    worksheet.Cell(rowIndex, cellIndex++).SetValue(value?.ToString() ?? "-");
                }
            }

            worksheet.Columns().AdjustToContents();

            // Generate filename
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-tt", CultureInfo.InvariantCulture);
            var fileName = $"{filePrefix}_{timestamp}.xlsx";
            var exportDir = Path.Combine(_env.WebRootPath, "exports");

            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            var fullPath = Path.Combine(exportDir, fileName);
            workbook.SaveAs(fullPath);

            return fileName;
        }
    }
}
