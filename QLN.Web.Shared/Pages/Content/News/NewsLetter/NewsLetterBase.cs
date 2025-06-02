using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
public class NewsLetterBase : ComponentBase
{
    [Inject] INewsLetterSubscription newsLetterSubscriptionService { get; set; }
    [Inject] ISnackbar Snackbar { get; set; }
    protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
    protected string SubscriptionStatusMessage = string.Empty;
    protected bool IsSubscribingToNewsletter { get; set; } = false;

    protected MudForm _form;

    protected async Task SubscribeAsync()
    {
        IsSubscribingToNewsletter = true;
        await _form.Validate();

        if (_form.IsValid)
        {
            try
            {
                var success = await newsLetterSubscriptionService.SubscribeAsync(SubscriptionModel);
                SubscriptionStatusMessage = success ? "Subscribed successfully!" : "Failed to subscribe.";
                if (success)
                {
                    Snackbar.Add("Subscription successful!", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Failed to subscribe. Please try again later.", Severity.Error);
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
        }
        else
        {
            Snackbar.Add("Failed to subscribe. Please try again later.", Severity.Error);

        }
    }
}