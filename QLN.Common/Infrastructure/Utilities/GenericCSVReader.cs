using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
namespace QLN.Common.Infrastructure.Utilities
{
    public static class GenericCSVReader
    {
        public static async Task<List<T>> ReadCsv<T>(string url)
        {
            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(url);

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            return csv.GetRecords<T>().ToList();
        }

    }
}
