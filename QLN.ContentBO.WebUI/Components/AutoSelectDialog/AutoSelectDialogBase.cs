using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
namespace QLN.ContentBO.WebUI.Components.AutoSelectDialog;

public class AutoSelectDialogBase : ComponentBase
{
    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string Title { get; set; } = "Select Item";
    [Parameter] public string Label { get; set; } = "Select*";
    [Parameter] public string ButtonText { get; set; } = "Continue";

    [Parameter] public List<DropdownItem> ListItems { get; set; } = new();

    protected string? _autocompleteError;
    protected string? _searchText;

    [Parameter] public EventCallback<DropdownItem> OnSelect { get; set; }

    protected DropdownItem? SelectedItem { get; set; }

    protected async Task AddClicked()
    {
        _autocompleteError = null;

        var isValid = ListItems.Any(i => i.Label.Equals(SelectedItem?.Label, StringComparison.InvariantCultureIgnoreCase));
        if (!isValid)
        {
            _autocompleteError = "No matching item found.";
            StateHasChanged();
            return;
        }

        if (OnSelect.HasDelegate && SelectedItem != null)
        {
            await OnSelect.InvokeAsync(SelectedItem);
        }

        Cancel();
    }

    protected Task<IEnumerable<DropdownItem>> SearchItems(string value, CancellationToken token)
    {
        IEnumerable<DropdownItem> result;
        if (string.IsNullOrWhiteSpace(value))
        {
            result = ListItems;
        }
        else
        {
            result = ListItems
                .Where(i => i.Label.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

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

        var matched = ListItems.Any(i => i.Label.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        _autocompleteError = matched ? null : "No matching item found.";
    }

    protected void Cancel() => MudDialog.Cancel();
    
}