using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IAddonService
{
    public interface IAddonActor : IActor
    {
        // General get/set methods for addon data
        Task<bool> SetAddonDataAsync(AddonDataDto data, CancellationToken cancellationToken = default);
        Task<AddonDataDto?> GetAddonDataAsync(CancellationToken cancellationToken = default);
    }
}
