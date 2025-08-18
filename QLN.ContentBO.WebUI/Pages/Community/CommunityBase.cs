using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Community
{
    public class CommunityBase : QLComponentBase
    {
        protected string searchText = string.Empty;

        [Inject] public ICommunityService communityservice { get; set; }
        [Inject] public ILogger<CommunityBase> Logger { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }


        protected List<CommunityPostDto> _posts = new();
        protected bool IsLoading = true;
        protected string? DeletingId;
        protected bool ascending = true;
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int totalRecords;

        protected int TotalCount => totalRecords;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await AuthorizedPage();
                await LoadPostsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        protected async Task LoadPostsAsync()
        {
            IsLoading = true;

            try
            {
                var response = await communityservice.GetAllCommunityPosts(
                    categoryId: null,
                    search: searchText,
                    page: currentPage,
                    pageSize: pageSize,
                    sortDirection: ascending ? "desc" : "asc"
                );

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<CommunityListResponseDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (result != null)
                    {
                        _posts = result.Items;
                        totalRecords = result.Total;
                    }
                    else
                    {
                        Logger.LogWarning("API returned null result.");
                        _posts = new();
                        totalRecords = 0;
                    }
                }
                else
                {
                    Logger.LogError("API call failed with status code: {StatusCode}", response.StatusCode);
                    _posts = new();
                    totalRecords = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching community posts");
                _posts = new();
                totalRecords = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task HandleSearch(string value)
        {
            searchText = value?.Trim() ?? string.Empty;
            currentPage = 1;
            await LoadPostsAsync();
        }

        protected async Task HandleSort(bool sortAscending)
        {
            ascending = sortAscending;
            await LoadPostsAsync();
        }

        protected async Task HandlePageChange(int page)
        {
            currentPage = page;
            await LoadPostsAsync();
        }

        protected async Task HandlePageSizeChange(int size)
        {
            pageSize = size;
            currentPage = 1;
            await LoadPostsAsync();
        }

    protected async Task DeletePost(string id)
{
    try
    {
        DeletingId = id;

        var response = await communityservice.DeleteCommunity(id);

                if (response.IsSuccessStatusCode)
                {
                    _posts.RemoveAll(p => p.Id == id);
                    Snackbar.Add("Community post deleted successfully.", Severity.Success);
                }
                else
                {
                    Logger.LogError("Delete failed with status {StatusCode}", response.StatusCode);
                }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error deleting community post");
    }
    finally
    {
        DeletingId = null;
    }
}


    }
}
