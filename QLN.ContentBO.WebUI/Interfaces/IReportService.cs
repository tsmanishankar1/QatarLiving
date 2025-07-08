using System.IdentityModel.Tokens.Jwt;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IReportService
    {
        Task<HttpResponseMessage> UpdateReport(string endpoint, string id, bool isKeep, bool isDelete);

       
  Task<HttpResponseMessage> GetReportsWithPaginationAsync(
    string endpoint,
    string? sortOrder = "desc",
    int pageNumber = 1,
    int pageSize = 12,
    string? searchTerm = null
);



    }
}
