using System.Net.Http;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ICommunityService
    {
        Task<HttpResponseMessage> GetAllCommunity();
        Task<HttpResponseMessage> DeleteCommunity(int id);
        Task<HttpResponseMessage> GetAllCommunitySearch(object payload);
    }
}
