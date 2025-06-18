using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Collections.Generic;
using System.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

public class NewsLetterBase : ComponentBase
{
    [Inject] INewsLetterSubscription newsLetterSubscriptionService { get; set; }
    [Inject] ISnackbar Snackbar { get; set; }
    [Inject] protected IJSRuntime JS { get; set; }
    [Inject] public HttpClient Http { get; set; }
    protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
    protected string SubscriptionStatusMessage = string.Empty;
    protected bool IsSubscribingToNewsletter { get; set; } = false;
    protected MudForm _form;

    protected async Task SubscribeAsync()
    {
        IsSubscribingToNewsletter = true;
        await _form.Validate();
        if (_form.IsValid && SubscriptionModel != null && !string.IsNullOrWhiteSpace(SubscriptionModel.Email))
        {
            try
            {
                string baseUrl = "https://qatarliving.us9.list-manage.com/subscribe/post-json";
                string u = "3ab0436d22c64716e67a03f64";
                string id = "94198fac96";
                string email = SubscriptionModel.Email;
                string callback = $"jQuery{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                string botField = "";
                string subscribe = "Subscribe";
                string cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["u"] = u;
                query["id"] = id;
                query["c"] = callback;
                query["EMAIL"] = email;
                query["b_3ab0436d22c64716e67a03f64_94198fac96"] = botField;
                query["subscribe"] = subscribe;
                query["_"] = cacheBuster;

                string url = $"{baseUrl}?{query}";
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                request.Headers.Add("Referer", "https://qatarliving.com/");
                request.Headers.Add("Origin", "https://qatarliving.com");
                var response = await Http.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var successPatteren = "Thank you for subscribing!";

                var matches = Regex.Matches(responseContent, @"\((\{.*?\})\)");
                string msg = "";
                foreach (Match match in matches)
                {
                    string json = match.Groups[1].Value;
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("msg", out var msgElement))
                    {
                        msg = msgElement.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind != JsonValueKind.Null)
                    {
                        msg = errorsElement.ToString();
                    }
                }

                if (response.IsSuccessStatusCode && successPatteren.Equals(msg, StringComparison.OrdinalIgnoreCase))

                {
                    Snackbar.Add($"Subscription submitted: {msg}", Severity.Success);
                    SubscriptionStatusMessage = $"Subscription submitted: {msg}";
                    SubscriptionModel.Email = string.Empty;
                    StateHasChanged();
                }
                else if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(msg))
                {
                    Snackbar.Add($"{msg}", Severity.Warning);
                    SubscriptionStatusMessage = $"{msg}";
                    SubscriptionModel.Email = string.Empty;
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Failed to subscribe. Please try again.", Severity.Error);
                    SubscriptionStatusMessage = "Failed to subscribe. Please try again.";
                    SubscriptionModel.Email = string.Empty;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} Newsletter subscription failed.");
                SubscriptionStatusMessage = "An error occurred while subscribing.";
                Snackbar.Add($"Failed to subscribe: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsSubscribingToNewsletter = false;
            }
        } else
        {
            IsSubscribingToNewsletter = false;
        }
    }
}