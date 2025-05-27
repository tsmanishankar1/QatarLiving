using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service.ContentService
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {
        public async Task<QlnContentsDailyPageResponse?> GetContentsDailyPageAsync()
        {
            return await httpClient.GetFromJsonAsync<QlnContentsDailyPageResponse>($"{ContentConstants.LandingPath}/{ContentConstants.QlnContentsDaily}");
        }

        public async Task<NewsCommunityPageResponse?> GetNewsCommunityAsync()
        {
            return await httpClient.GetFromJsonAsync<NewsCommunityPageResponse>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsCommunity}");
        }

        public async Task<NewsQatarPageResponse?> GetNewsQatarAsync()
        {
            return await httpClient.GetFromJsonAsync<NewsQatarPageResponse>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsQatar}");
        }

        //public async Task<NewsMiddleEast?> GetNewsMiddleEastAsync()
        //{
        //    return await httpClient.GetFromJsonAsync<NewsMiddleEast>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsMiddleEast}");
        //}

        //public async Task<NewsWorld?> GetNewsWorldAsync()
        //{
        //    return await httpClient.GetFromJsonAsync<NewsWorld>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsWorld}");
        //}

        //public async Task<NewsHealthEducation?> GetNewsHealthEducationAsync()
        //{
        //    return await httpClient.GetFromJsonAsync<NewsHealthEducation>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsHealthEducation}");
        //}

        //public async Task<NewsLaw?> GetNewsLawAsync()
        //{
        //    return await httpClient.GetFromJsonAsync<NewsLaw>($"{ContentConstants.LandingPath}/{ContentConstants.QlnNewsNewsLaw}");
        //}

        /// <summary>
        /// Tester method for testing out as yet unmapped Drupal queues
        /// </summary>
        /// <param name="queue_name">Name of Queue on Drupal System</param>
        /// <returns>anonymous object, so you can see all that is returned</returns>
        public async Task<dynamic?> GetLandingByQueuePageAsync(string queue_name)
        {
            return await httpClient.GetFromJsonAsync<dynamic>($"{ContentConstants.LandingPath}/{queue_name}");
        }

        public async Task<ContentPost?> GetPostBySlugAsync(string slug)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{ContentConstants.GetPostBySlugPath}?slug={slug}");

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;
        }

        public async Task<ContentEvent?> GetEventBySlugAsync(string slug)
        {
            var results = await httpClient.GetFromJsonAsync<ContentEvent>($"{ContentConstants.GetEventBySlugPath}?slug={slug}");

            if (results?.NodeType == "event")
            {
                return results;
            }

            return null;
        }
    }
}
