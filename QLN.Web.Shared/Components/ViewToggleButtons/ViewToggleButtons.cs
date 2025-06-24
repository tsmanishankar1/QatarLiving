using Microsoft.AspNetCore.Components;

namespace QLN.Web.Shared.Components.ViewToggleButtons
{
    public partial class ViewToggleButtons : ComponentBase
    {
        [Parameter] 
        public List<ViewToggleOption> Items { get; set; }
        [Parameter] public string? TextClass { get; set; }
   
        [Parameter]
        public string? Height { get; set; } = "60px"; 
        [Parameter] 
        public string SelectedValue { get; set; }

        [Parameter] 
        public EventCallback<string> OnSelected { get; set; }

        public class ViewToggleOption
        {
            public string? ImageUrl { get; set; }
            public string Label { get; set; }
            public string Value { get; set; }
        }
    }
}