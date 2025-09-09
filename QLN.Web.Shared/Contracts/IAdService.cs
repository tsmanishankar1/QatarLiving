using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.Contracts
{ 
    public interface IAdService
    {
        Task<IEnumerable<AdModel>> GetAdDetail();

    }
}
