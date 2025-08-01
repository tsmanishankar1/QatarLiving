using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Extensions;

namespace QLN.ContentBO.WebUI.Components.Classified.ItemInfo
{
    public class ItemInfoBase : ComponentBase, IDisposable
    {
        [Parameter] public PreviewAdDto Item { get; set; } = default!;

        protected string? ShowContactType;

        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

        protected DotNetObjectReference<ItemInfoBase>? _dotNetRef;

        protected virtual void ShowCall() => ShowContactType = "call";
        protected virtual void ShowWhatsApp() => ShowContactType = "whatsapp";
        protected virtual void CloseContact() => ShowContactType = null;

        protected bool IsMobile => false;

        protected string FullPhoneNumber =>
            $"{Item?.ContactNumberCountryCode}{Item?.ContactNumber}".Trim();

        protected string FullWhatsAppNumber =>
            $"{Item?.WhatsappNumberCountryCode}{Item?.WhatsappNumber}".Trim();

        protected bool HasPhoneNumber =>
            !string.IsNullOrWhiteSpace(Item?.ContactNumberCountryCode) &&
            !string.IsNullOrWhiteSpace(Item?.ContactNumber);

        protected bool HasWhatsAppNumber =>
            !string.IsNullOrWhiteSpace(Item?.WhatsappNumberCountryCode) &&
            !string.IsNullOrWhiteSpace(Item?.WhatsappNumber);

        protected string GetWhatsAppLink(string number)
        {
            var clean = number.Replace("+", "").Replace("-", "").Replace(" ", "");
            return IsMobile
                ? $"https://wa.me/{clean}"
                : $"https://web.whatsapp.com/send?phone={clean}";
        }

        protected string GetTimeDifferenceFromNowUtc(DateTime givenUtcTime)
        {
            try
            {
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
                    var hours = (int)absDiff.TotalHours;
                    return isFuture ? $"in {hours} hour(s)" : $"{hours} hour(s) ago";
                }
                else
                {
                    var minutes = (int)Math.Round(absDiff.TotalMinutes);
                    return isFuture ? $"in {minutes} minute(s)" : $"{minutes} minute(s) ago";
                }
            }
            catch
            {
                return "N/A";
            }
        }

        public virtual void Dispose()
        {
            _dotNetRef?.Dispose();
        }
    }
}
