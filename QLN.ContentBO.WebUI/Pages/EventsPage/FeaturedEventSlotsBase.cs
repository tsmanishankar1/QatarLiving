using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;
using MudBlazor;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Components;
using System.Net;
using Markdig.Syntax;
namespace QLN.ContentBO.WebUI.Pages
{
    public class FeaturedEventSlotsBase : QLComponentBase
    {
         [Parameter] public List<FeaturedSlot> FeaturedEventSlots { get; set; }
        [Parameter] public List<EventCategoryModel> Categories { get; set; }
        [Parameter] public EventCallback<FeaturedSlot> ReplaceSlot { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }
    }
}