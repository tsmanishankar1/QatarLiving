using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2ContentDailyService
    {
        Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default);
    }
}
