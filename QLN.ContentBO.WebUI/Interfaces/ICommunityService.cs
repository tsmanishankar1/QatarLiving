using System.Net.Http;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ICommunityService
    {
        Task<HttpResponseMessage> GetAllCommunityPosts(string? categoryId, string? search, int page, int pageSize, string? sortDirection);
         Task<HttpResponseMessage> DeleteCommunity(string id);
    }
}
