using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;
using QLN.ContentBO.WebUI.Interfaces;
public class ReplaceDialogModalBase : ComponentBase
{
    [Inject]
    public IDialogService DialogService { get; set; } = default!;
    [Inject]
    public IClassifiedService ClassifiedService { get; set; }
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public int SlotNumber { get; set; } 

    [Parameter]
    public string Title { get; set; } = "Featured Event";

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string Placeholder { get; set; } = "Item Name*";
    protected override void OnInitialized()
    {
        SelectedPick.SlotNumber = SlotNumber;
    }


    public void Cancel() => MudDialog.Cancel();
    [Parameter]
    public List<SeasonalPickDto> events { get; set; } = new();
    [Parameter] public EventCallback<SeasonalPickDto> OnAdd { get; set; }

    protected string? _autocompleteError;
    protected string? _searchText;
    protected FeaturedLanding SelectedPick { get; set; } = new FeaturedLanding();
    public class FeaturedLanding
    {
        public int SlotNumber { get; set; }
        public SeasonalPickDto? Seasonal { get; set; }
    }
    protected async Task AddClicked()
    {
        _autocompleteError = null;

        var selected = SelectedPick?.Seasonal;
        var slot = SlotNumber;
        if (selected == null || string.IsNullOrWhiteSpace(selected.CategoryName))
        {
            _autocompleteError = "No matching seasonal pick found.";
            StateHasChanged();
            return;
        }

        var response = await ClassifiedService.ReplaceSeasonalPickAsync(selected.Id, slot, "classifieds");

        if (response?.IsSuccessStatusCode == true)
        {
            Snackbar.Add("Seasonal pick replaced successfully.", Severity.Success);

            if (OnAdd.HasDelegate)
            {
                await OnAdd.InvokeAsync(selected);
            }

            MudDialog.Close(DialogResult.Ok(selected));
        }
        else
        {
            Snackbar.Add("Failed to replace seasonal pick.", Severity.Error);
        }
    }

    protected Task<IEnumerable<SeasonalPickDto>> SearchEventTitles(string value, CancellationToken token)
    {
        IEnumerable<SeasonalPickDto> result;
        if (string.IsNullOrWhiteSpace(value))
        {
            result = events;
        }
        else
        {
            result = events
                .Where(e => e.CategoryName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        Console.WriteLine("result", result);

        return Task.FromResult(result);
    }
    protected void ValidateTypedText(string text)
    {
        _searchText = text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _autocompleteError = null;
            return;
        }
        var matched = events.Any(e => e.CategoryName.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        _autocompleteError = matched ? null : "No matching event found.";
    }
    
}