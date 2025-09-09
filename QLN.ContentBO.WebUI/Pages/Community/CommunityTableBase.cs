using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Extensions;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Community
{
    public class CommunityTableBase : QLComponentBase
    {
        [Parameter]
        public List<CommunityPostDto> Posts { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public string DeletingId { get; set; }

        [Parameter]
        public EventCallback<string> OnDelete { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        protected async Task DeleteSlotHandler(string id)
        {
            var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Community Post?" },
            { "ButtonTitle", "Delete" },
              { "OnConfirmed", EventCallback.Factory.Create(this, async () =>
                {
                    await OnDelete.InvokeAsync(id);
                })
            }
        };
            var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            var result = await dialog;
        }


        public string GetTimeDifferenceFromNowUtc(DateTime givenUtcTime)
        {
            try
            {
                // Skip execution for default or min DateTime
                if (givenUtcTime == DateTime.MinValue)
                    return "-";

                var now = DateTime.UtcNow.ToQatarTime();
                var diff = now - givenUtcTime.ToQatarTime();
                var isFuture = diff.TotalSeconds < 0;
                var absDiff = diff.Duration();

                if (absDiff.TotalHours >= 24)
                {
                    var days = (int)absDiff.TotalDays;
                    return isFuture ? $"in {days} day(s)" : $"{days} day(s) ago";
                }
                else if (absDiff.TotalHours >= 1)
                {
                    var hours = Math.Round(absDiff.TotalHours, 1);
                    return isFuture ? $"in {hours} hour(s)" : $"{hours} hour(s) ago";
                }
                else
                {
                    var minutes = (int)Math.Round(absDiff.TotalMinutes);
                    return isFuture ? $"in {minutes} minute(s)" : $"{minutes} minute(s) ago";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetTimeDifferenceFromNowUtc");
                return "N/A";
            }
        }
    }
}
