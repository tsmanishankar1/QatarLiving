using Dapr.Actors;
using QLN.Common.DTO_s.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IProductService
{
    public interface IV2UserAddonActor : IActor
    {
        /// <summary>
        /// Sets addon data with fast write operation
        /// </summary>
        Task<bool> FastSetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets addon data (alias for FastSetDataAsync)
        /// </summary>
        Task<bool> SetDataAsync(V2UserAddonDto data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets addon data from actor state
        /// </summary>
        Task<V2UserAddonDto?> GetDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if addon has enough quota for the requested usage
        /// </summary>
        Task<bool> ValidateUsageAsync(string quotaType, decimal requestedAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records usage against addon quota
        /// </summary>
        Task<bool> RecordUsageAsync(string quotaType, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if addon is currently active
        /// </summary>
        Task<bool> IsActiveAsync(CancellationToken cancellationToken = default);
    }
}
