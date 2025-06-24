//using Dapr.Client;
//using QLN.Common.DTO_s;
//using QLN.Common.Infrastructure.IService.V2IContent;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using static QLN.Common.Infrastructure.Constants.ConstantValues;

//namespace QLN.Content.MS.Service.NewsInternalService
//{
//    public class NewsInternalService : IV2ContentNews
//    {
//        private readonly DaprClient _daprClient;
//        private const string StateStoreName = V2Content.ContentStoreName;

//        public NewsInternalService(DaprClient daprClient)
//        {
//            _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
//        }

//        public async Task<NewsSummary> ProcessNewsContentAsync(ContentNewsDto dto, string userId, CancellationToken cancellationToken = default)
//        {
//            if (dto == null) throw new ArgumentNullException(nameof(dto));
//            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required");

//            int totalSections =
//                (dto.FinanceData?.Topics?.Count ?? 0) +
//                (dto.LifestyleData?.Topics?.Count ?? 0) +
//                (dto.NewsData?.Topics?.Count ?? 0) +
//                (dto.SportsData?.Topics?.Count ?? 0);

//            var topicCounts = new Dictionary<string, int>
//            {
//                ["Finance"] = dto.FinanceData?.Topics?.Count ?? 0,
//                ["Lifestyle"] = dto.LifestyleData?.Topics?.Count ?? 0,
//                ["News"] = dto.NewsData?.Topics?.Count ?? 0,
//                ["Sports"] = dto.SportsData?.Topics?.Count ?? 0
//            };

//            var summary = new NewsSummary
//            {
//                TotalSections = totalSections,
//                TopicBreakdown = topicCounts,
//                ProcessedAt = DateTime.UtcNow
//            };

//            // Save to Dapr state store using userId as key
//            await _daprClient.SaveStateAsync(StateStoreName, userId, summary, cancellationToken: cancellationToken);

//            return summary;
//        }

//        public async Task<NewsSummary?> GetNewsSummaryAsync(string userId, CancellationToken cancellationToken = default)
//        {
//            if (string.IsNullOrWhiteSpace(userId))
//                throw new ArgumentException("User ID is required");

//            return await _daprClient.GetStateAsync<NewsSummary>(StateStoreName, userId, cancellationToken: cancellationToken);
//        }
  

    
//    }
//}
