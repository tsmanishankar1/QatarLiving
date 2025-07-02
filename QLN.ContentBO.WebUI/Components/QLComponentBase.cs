using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Components
{
    public class QLComponentBase : ComponentBase
    {
        [Inject] public ISnackbar Snackbar { get; set; }
    }
}
