using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop; 
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Helpers;
using System.Collections.Generic;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using MudBlazor;
public class ArticleDetailCardBase : ComponentBase
{
    [Parameter]
    public ContentPost Post { get; set; }
    protected bool imageLoaded = false;
    [Inject]
    private NavigationManager Navigation { get; set; }
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    [Inject]
    protected ISnackbar Snackbar { get; set; }
    [Parameter]
    public bool loading { get; set; }
    private string CurrentUrl => Navigation.ToAbsoluteUri(Navigation.Uri).ToString();
    public class MenuItem
    {
        public string Text { get; set; }
        public string ImageSrc { get; set; }
        public string Route { get; set; }
        public bool OpenInNewTab { get; set; } = false;
        public Func<Task> OnClick { get; set; }
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
            ImageSrc = "/qln-images/instagram_share_icon.svg",
            Route = SocialShareHelper.GetInstagramUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/whatsApp_share_icon.svg",
            Route = SocialShareHelper.GetWhatsAppUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/tiktok_share_icon.svg",
            Route = SocialShareHelper.GetTikTokUrl(CurrentUrl),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/x_share_icon.svg",
            Route = SocialShareHelper.GetXUrl(CurrentUrl, Post?.Title ?? ""),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/linkedin_share_icon.svg",
            Route = SocialShareHelper.GetLinkedInUrl(CurrentUrl, Post?.Title ?? "", Post?.Description ?? ""),
            OpenInNewTab = true
        },
        new MenuItem
        {
            ImageSrc = "/qln-images/copy_link_icon.svg",
            OnClick = async () =>
            {
                bool copied = false;
                copied = await SocialShareHelper.CopyLinkToClipboardAsync(JSRuntime, CurrentUrl);
                if (copied)
                {
                    Snackbar.Add("Item link has been copied to the clipboard", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Failed to copy link. Please try again.", Severity.Error);
                }
            }
        }
    }; protected int commentsCount = 0;
    public string DescriptionHtml { get; set; }
    public string FormattedDate { get; set; }
    protected MarkupString ParsedDescription => new MarkupString(DescriptionHtml);
    protected override void OnParametersSet()
    {
        imageLoaded = false; 
        if (Post != null)
        {
            DescriptionHtml = Post.Description;
            if (!string.IsNullOrEmpty(Post?.DateCreated))
            {
                FormattedDate = FormatDateToReadable(Post.DateCreated);
            }
            commentsCount = Post?.Comments?.Count ?? 0;
        }
        Console.WriteLine("the html description is the " +  DescriptionHtml);

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
        protected void OnImageLoaded()
        {
            imageLoaded = true;
            StateHasChanged();
        }
 
        protected void OnImageError()
        {
            imageLoaded = true; 
            StateHasChanged();
        }
}
