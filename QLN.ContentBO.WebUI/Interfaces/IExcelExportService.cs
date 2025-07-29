using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IExcelExportService
    {
        Task<string> ExportAsync<T>(
            List<T> data,
            Dictionary<string, Func<T, object?>> columnSelectors,
            string sheetName = "Sheet1",
            string filePrefix = "Export"
        );
    }
}
