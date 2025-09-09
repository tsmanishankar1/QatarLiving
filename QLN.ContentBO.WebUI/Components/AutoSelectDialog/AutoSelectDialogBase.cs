using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using System.Net;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Components.AutoSelectDialog
{
    public class AutoSelectDialogBase : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
        [Inject] public IDrupalUserService DrupalUserService { get; set; }

        [Parameter] public string Title { get; set; } = "Select Item";
        [Parameter] public string Label { get; set; } = "Select*";
        [Parameter] public string ButtonText { get; set; } = "Continue";

        [Parameter] public EventCallback<DropdownItem> OnSelect { get; set; }

        protected DropdownItem? SelectedItem { get; set; }
        protected string? _autocompleteError;
        protected string? _searchText;

        protected async Task AddClicked()
        {
            _autocompleteError = null;

            if (SelectedItem == null || string.IsNullOrWhiteSpace(SelectedItem.Label))
            {
                _autocompleteError = "Please select a valid user.";
                StateHasChanged();
                return;
            }

            if (OnSelect.HasDelegate)
                await OnSelect.InvokeAsync(SelectedItem);

            Cancel();
        }

        protected async Task<IEnumerable<DropdownItem>> SearchItems(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
                return Enumerable.Empty<DropdownItem>();

            var responseList = await DrupalUserService.SearchDrupalUsersAsync(value);
            var response = responseList.FirstOrDefault();

            if (response == null || !response.IsSuccessStatusCode)
                return Enumerable.Empty<DropdownItem>();

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                var users = JsonSerializer.Deserialize<List<DrupalUserResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

                return users.Select(u => new DropdownItem
                {
                    Id = int.TryParse(u.Uid, out var idVal) ? idVal : 0,
                    Label = u.Mail // Email becomes the label
                });
            }
            catch
            {
                return Enumerable.Empty<DropdownItem>();
            }
        }

        protected async void ValidateTypedText(string text)
        {
            _searchText = text;

            if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
            {
                _autocompleteError = null;
                return;
            }

            var responseList = await DrupalUserService.SearchDrupalUsersAsync(text);
            var response = responseList.FirstOrDefault();

            if (response != null && response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<DrupalUserResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _autocompleteError = (users?.Any() == true) ? null : "No matching user found.";
            }
            else
            {
                _autocompleteError = "Error retrieving users.";
            }

            StateHasChanged();
        }

        protected void Cancel() => MudDialog.Cancel();
    }
}
