using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using System.Linq;

namespace QLN.ContentBO.WebUI.Components.FilePreviewDialog
{
    public class FilePreviewDialogBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Parameter] public string PdfUrl { get; set; } = string.Empty;
        [Parameter] public string CanvasId { get; set; } = "pdf-canvas";

        // protected override async Task OnAfterRenderAsync(bool firstRender)
        // {
        //     if (firstRender && !string.IsNullOrEmpty(PdfUrl))
        //     {
        //         await JS.InvokeVoidAsync("viewPdf", "https://qlnfilesdev.blob.core.windows.net/services-images/15ac70a21f.pdf", CanvasId);
        //     }
        // }
        // public void Close() => MudDialog.Close();
    
    }
}