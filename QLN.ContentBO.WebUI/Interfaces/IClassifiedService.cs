using System.Net.Http;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IClassifiedService
    {
          /// <summary>
        /// Gets Classifieds by IdCategoryTrees.
        /// </summary>
        /// <param name="vertical">Classifieds CategoryTrees</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetAllCategoryTreesAsync(string vertical);

      
    }
}
