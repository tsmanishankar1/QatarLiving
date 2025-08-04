using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using System.Text.RegularExpressions;

namespace QLN.ContentBO.WebUI.Components.Classified.DescriptionBox
{
    public class DescriptionBoxBase : ComponentBase
    {
        [Parameter] public PreviewAdDto Item { get; set; } = default!;

        protected bool showMore = false;
        protected int MaxPreviewLength = 250;

        protected string? fullDescription => Item?.Description;

        protected string? shortDescription =>
            string.IsNullOrWhiteSpace(Item?.Description)
                ? string.Empty
                : GenerateShortDescription(Item.Description, MaxPreviewLength);

        protected bool ShouldShowToggle =>
            !string.IsNullOrWhiteSpace(Item?.Description) &&
            StripHtmlTags(Item.Description).Length > MaxPreviewLength;

        protected void ToggleReadMore()
        {
            showMore = !showMore;
        }

        protected static string StripHtmlTags(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        protected string GenerateShortDescription(string html, int maxLength)
        {
            var plainText = StripHtmlTags(html);

            if (plainText.Length <= maxLength)
                return html;

            var truncated = plainText.Substring(0, maxLength) + "...";
            return $"<p>{truncated}</p>";
        }
    }
}
