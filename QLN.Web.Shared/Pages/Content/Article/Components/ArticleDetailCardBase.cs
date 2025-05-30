using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class ArticleDetailCardBase : ComponentBase
{
    [Parameter]
    public ContentPost Post { get; set; }
    [Parameter]
    public bool loading { get; set; }
    protected int commentsCount = 0;
    public string DescriptionHtml { get; set; }
    public string FormattedDate { get; set; }
    protected MarkupString ParsedDescription => new MarkupString(DescriptionHtml);
    protected override void OnParametersSet()
    {
        if (Post != null)
        {
            DescriptionHtml = Post.Description;
            if (!string.IsNullOrEmpty(Post?.DateCreated))
            {
                FormattedDate = FormatDateToReadable(Post.DateCreated);
            }
            commentsCount = Post?.Comments?.Count ?? 0;
        }

    }
     protected string FormatDateToReadable(string inputDate)
    {
        if (DateTime.TryParseExact(inputDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedDate))
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); // GMT+1
            var dateInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(parsedDate, timeZone);
            return $"{dateInTimeZone:MMMM d, yyyy 'at' h:mm tt} GMT{timeZone.BaseUtcOffset.Hours:+#;-#;+0}";
        }
        return inputDate; 
    }
}
