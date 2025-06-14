using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IBackOfficeService
{
    public interface IBackOfficeService<T>
        where T : class
    {
        Task<string> UpsertState(T item, CancellationToken ct);
        Task<T?> GetByIdState(string id, CancellationToken ct);
        Task<IList<T>> GetAllState(CancellationToken ct);
        Task DeleteState(string id, CancellationToken ct);
    }
}
