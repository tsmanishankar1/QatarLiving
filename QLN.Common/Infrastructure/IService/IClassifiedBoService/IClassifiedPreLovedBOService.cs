using QLN.Common.DTO_s.ClassifiedsBo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IClassifiedBoService
{
    public interface IClassifiedPreLovedBOService
    {
        Task<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>> ViewPreLovedSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, string? SortBy,string? SortOrder="desc", CancellationToken cancellationToken = default);

        Task<ClassifiedBOPageResponse<PreLovedViewP2PDto>> ViewPreLovedP2PSubscriptions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder = "desc", CancellationToken cancellationToken = default);

        Task<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>> ViewPreLovedP2PTransactions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder = "desc", CancellationToken cancellationToken = default);

        Task<string> BulkEditP2PSubscriptions(BulkEditPreLovedP2PDto dto, CancellationToken cancellationToken = default);

    }
}
