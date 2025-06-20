
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface INewsService
    {
        Task<HttpResponseMessage> GetAllArticle(int id);
        Task<HttpResponseMessage> CreateArticle(V2ContentNewsDto newsArticle);
    }
}
