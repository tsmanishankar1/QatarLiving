using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Helpers;
using System.Collections.Generic;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class ArticleDetailCardBase : ComponentBase
{
    [Parameter]
    public ContentPost Post { get; set; }
     [Inject]
    private NavigationManager Navigation { get; set; }
    [Parameter]
    public bool loading { get; set; }
    private string CurrentUrl => Navigation.ToAbsoluteUri(Navigation.Uri).ToString();
    public class MenuItem
    {
        public string Text { get; set; }
        public string ImageSrc { get; set; }
        public string Route { get; set; }
        public bool OpenInNewTab { get; set; } = false;
    }
    protected List<MenuItem> shareMenuItems => new()
    {
        new MenuItem
        {
            ImageSrc = "/qln-images/facebook_share_icon.svg",
            Route = SocialShareHelper.GetFacebookUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/whatsApp_share_icon.svg",
            Route = SocialShareHelper.GetWhatsAppUrl(CurrentUrl),
            OpenInNewTab = true
        }
    };    protected int commentsCount = 0;
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
