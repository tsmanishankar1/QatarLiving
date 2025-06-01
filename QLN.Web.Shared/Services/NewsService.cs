using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class NewsService : INewsService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public NewsService(HttpClient httpClient, IOptions<ApiSettings> options, ILogger<NewsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
         private readonly Dictionary<string, string> _tabEndpointMap = new()
        {
            { "Qatar", "qln_news_news_qatar" },
            { "Community", "qln_news_news_community" },
            { "Law", "qln_news_news_law" },
            { "Health & Education", "qln_news_news_health_education" },
            { "Middle East", "qln_news_news_middle_east" },
            { "World", "qln_news_news_world" },
            { "Qatar Economy", "qln_news_finance_qatar" },
            { "Market Updates", "qln_news_finance_market_update" },
            { "Real Estate", "qln_news_finance_real_estate" },
            { "Entrepreneurship", "qln_news_finance_entrepreneurship" },
            { "Finance", "qln_news_finance_finance" },
            { "Jobs & Careers", "qln_news_finance_jobs_careers" },
            { "Qatar Sports", "qln_news_sports_qatar_sports" },
            { "Football", "qln_news_sports_football" },
            { "International", "qln_news_sports_international" },
            { "Motorsports", "qln_news_sports_motorsports" },
            { "Olympics", "qln_news_sports_olympics" },
            { "Athlete Features", "qln_news_sports_athlete_features" },
            { "Food & Dining", "ql_news_lifestyle_food_dining" },
            { "Travel & Leisure", "qln_news_lifestyle_travel_leisure" },
            { "Arts & Culture", "qln_news_lifestyle_arts_culture" },
            { "Events", "qln_news_lifestyle_events" },
            { "Fashion & Style", "qln_news_lifestyle_fashion_style" },
            { "Home & Living", "qln_news_lifestyle_home_living" },
        };

        public async Task<HttpResponseMessage?> GetNewsCommunityAsync()
        {
            try
            {
                Console.WriteLine("response is" + response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsHealthAndEducationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_news_news_health_education/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsLawAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_news_news_law/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsMiddleEastAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_news_news_middle_east/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsQatarAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_news_news_qatar/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsWorldAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_news_news_world/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetNewsBySlugAsync(string slug )
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/news/{slug}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetNewsAsync(string tab)
        {
            try
            {
                  if (_tabEndpointMap.TryGetValue(tab, out var slug))
                {
                    var response = await _httpClient.GetAsync("/api/content/{slug}/landing");
                    Console.WriteLine("response is" + response);
                    return response;
                }
                else
                { 
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}

