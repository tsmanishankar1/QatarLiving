using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditAdBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }

        protected ItemEditAdPost adPostModel { get; set; } = new();

        [Parameter] public string Id { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadAdDataAsync();
        }

        private async Task LoadAdDataAsync()
        {
            try
            {
                var response = await ClassifiedService.GetAdByIdAsync("items", Id);
                if (response is { IsSuccessStatusCode: true })
                {
                    var json = await response.Content.ReadAsStringAsync();
                    adPostModel = JsonSerializer.Deserialize<ItemEditAdPost>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new ItemEditAdPost();
                }
                else
                {
                    // Handle 404 or error gracefully
                    adPostModel = new ItemEditAdPost();
                }
            }
            catch (JsonException jsonEx)
            {
                // Log and fallback if deserialization fails
                Console.Error.WriteLine($"Deserialization error: {jsonEx.Message}");
                adPostModel = new ItemEditAdPost();
            }
            catch (Exception ex)
            {
                // General fallback
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                adPostModel = new ItemEditAdPost();
            }
        }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/items/view/listing");
        }
    }
}
