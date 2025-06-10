using Microsoft.JSInterop;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public partial class QLAnalyticsService : IQLAnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public QLAnalyticsService(
            HttpClient httpClient,
            IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task TrackEventAsync(
            QLAnalyticsCallProps props,
            string browserId,
            string sessionId)
        {
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "X-Browser-ID", browserId },
                    { "X-Session-ID", sessionId }
                };

                if (!string.IsNullOrEmpty(props.Token))
                {
                    headers["Authorization"] = $"Bearer {props.Token}";
                }

                var queryParams = new Dictionary<string, string>
                {
                    { "type", props.AnalyticType.ToString() },
                    { "vertical", props.VerticalTag.ToString() }
                };

                string urlSuffix = "";
                if (props.AnalyticType == (int)AnalyticType.ACTION_TRACKING)
                {
                    urlSuffix = "/action-tracking";
                }

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var url = $"/analytics{urlSuffix}?{queryString}";

                // Compose the body
                var body = new Dictionary<string, object?>();
                if (props.AdditionalTag != null)
                    body["additional_tag"] = props.AdditionalTag;

                // Add all other properties except Token and AnalyticType and AdditionalTag
                var propsType = typeof(QLAnalyticsCallProps);
                foreach (var prop in propsType.GetProperties())
                {
                    if (prop.Name == nameof(QLAnalyticsCallProps.Token) ||
                        prop.Name == nameof(QLAnalyticsCallProps.AnalyticType) ||
                        prop.Name == nameof(QLAnalyticsCallProps.VerticalTag))
                        continue;

                    var value = prop.GetValue(props);
                    if (value != null)
                    {
                        // Convert PascalCase to snake_case for JSON keys
                        var key = string.Concat(prop.Name.Select((x, i) =>
                            i > 0 && char.IsUpper(x) ? "_" + char.ToLower(x) : char.ToLower(x).ToString()));
                        body[key] = value;
                    }
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };

                foreach (var header in headers)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        continue;
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"This error occurred when calling analytics. Error: {ex}");
            }
        }

        public Task TrackEventForVehiclesAsync(
            VehicleEventAnalyticsData data,
            string browserId,
            string sessionId,
            string? token)
        {
            return TrackEventAsync(
                new QLAnalyticsCallProps
                {
                    AnalyticType = (int)AnalyticType.ACTION_TRACKING,
                    AdditionalTag = null,
                    Token = token,
                    Lead = VerticalTag.VEHICLES.ToString(),
                    // Map other properties from data as needed
                },
                browserId,
                sessionId
            );
        }

        public Task TrackEventForPropertiesAsync(
            PropertyEventAnalyticsData data,
            string browserId,
            string sessionId,
            string? token)
        {
            return TrackEventAsync(
                new QLAnalyticsCallProps
                {
                    AnalyticType = (int)AnalyticType.ACTION_TRACKING,
                    AdditionalTag = null,
                    Token = token,
                    VerticalTag = (int)VerticalTag.PROPERTIES,
                    // Map other properties from data as needed
                },
                browserId,
                sessionId
            );
        }

        public Task TrackEventForRewardsAsync(
            RewardEventAnalyticsData data,
            string browserId,
            string sessionId,
            string? token)
        {
            return TrackEventAsync(
                new QLAnalyticsCallProps
                {
                    AnalyticType = (int)AnalyticType.ACTION_TRACKING,
                    AdditionalTag = null,
                    Token = token,
                    VerticalTag = (int)VerticalTag.REWARDS,
                    // Map other properties from data as needed
                },
                browserId,
                sessionId
            );
        }

        public Task TrackEventForContentAsync(
            ContentEventAnalyticsData data,
            string browserId,
            string sessionId,
            string? token)
        {
            return TrackEventAsync(
                new QLAnalyticsCallProps
                {
                    AnalyticType = (int)AnalyticType.ACTION_TRACKING,
                    AdditionalTag = null,
                    Token = token,
                    VerticalTag = (int)VerticalTag.CONTENT,
                    // Map other properties from data as needed
                },
                browserId,
                sessionId
            );
        }

        public Task TrackEventForJobsAsync(
            JobEventAnalyticsData data,
            string browserId,
            string sessionId,
            string? token)
        {
            return TrackEventAsync(
                new QLAnalyticsCallProps
                {
                    AnalyticType = (int)AnalyticType.ACTION_TRACKING,
                    AdditionalTag = null,
                    Token = token,
                    VerticalTag = (int)VerticalTag.JOBS,
                    // Map other properties from data as needed
                },
                browserId,
                sessionId
            );
        }
        public async Task TrackEventFromClientAsync(
            QLAnalyticsCallProps props,
            string browserId,
            string sessionId)
        {
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "X-Browser-ID", browserId },
                    { "X-Session-ID", sessionId }
                };

                if (!string.IsNullOrEmpty(props.Token))
                {
                    headers["Authorization"] = $"Bearer {props.Token}";
                }

                var queryParams = new Dictionary<string, string>
                {
                    { "type", props.AnalyticType.ToString() },
                    { "vertical", props.VerticalTag.ToString() }
                };

                string urlSuffix = "";
                if (props.AnalyticType == (int)AnalyticType.ACTION_TRACKING)
                {
                    urlSuffix = "/action-tracking";
                }

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var url = $"/analytics{urlSuffix}?{queryString}";

                // Compose the body
                var body = new Dictionary<string, object?>();
                if (props.AdditionalTag != null)
                    body["additional_tag"] = props.AdditionalTag;

                var propsType = typeof(QLAnalyticsCallProps);
                foreach (var prop in propsType.GetProperties())
                {
                    if (prop.Name == nameof(QLAnalyticsCallProps.Token) ||
                        prop.Name == nameof(QLAnalyticsCallProps.AnalyticType) ||
                        prop.Name == nameof(QLAnalyticsCallProps.VerticalTag))
                        continue;

                    var value = prop.GetValue(props);
                    if (value != null)
                    {
                        var key = string.Concat(prop.Name.Select((x, i) =>
                            i > 0 && char.IsUpper(x) ? "_" + char.ToLower(x) : char.ToLower(x).ToString()));
                        body[key] = value;
                    }
                }

                var fetchRequest = new
                {
                    method = "POST",
                    headers,
                    body = JsonSerializer.Serialize(body)
                };

                await _jsRuntime.InvokeVoidAsync("fetch", url, fetchRequest);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"This error occurred when calling analytics from client. Error: {ex}");
            }
        }
    }

    public class VehicleEventAnalyticsData
    {
        public string EventName { get; set; } = default!;
        // Add other strongly-typed properties as needed
    }

    public class PropertyEventAnalyticsData
    {
        public string EventName { get; set; } = default!;
        // Add other strongly-typed properties as needed
    }

    public class RewardEventAnalyticsData
    {
        public string EventName { get; set; } = default!;
        // Add other strongly-typed properties as needed
    }

    public class ContentEventAnalyticsData
    {
        public string EventName { get; set; } = default!;
        // Add other strongly-typed properties as needed
    }

    public class JobEventAnalyticsData
    {
        public string EventName { get; set; } = default!;
        // Add other strongly-typed properties as needed
    }

    public enum AnalyticType
    {
        BANNER_CLICK = 6,
        VIEW_BANNER_IMPRESSION = 7,
        LEAD = 8,
        ACTION_TRACKING = 9
    }

    public static class AnalyticsLead
    {
        public const string CALL_REVEAL = "call_reveal";
        public const string WHATSAPP_REVEAL = "whatsapp_reveal";
        public const string CALL_CLICK = "call_click";
        public const string WHATSAPP_CLICK = "whatsapp_click";
        public const string SMS_REVEAL = "sms_reveal";
        public const string SMS_CLICK = "sms_click";
        public const string SHARED_CLICK = "shared_click";
        public const string FAVORITE_CLICK = "favorite_click";
        public const string STATIC_MAP_CLICK = "static_map_click";
    }

    public enum VerticalTag
    {
        VEHICLES = 1,
        PROPERTIES = 2,
        REWARDS = 11,
        CONTENT = 12,
        JOBS = 13
    }
}
