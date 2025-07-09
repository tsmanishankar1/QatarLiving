using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages
{
    public class DailyTopicsTableBase : QLComponentBase
    {
        [Parameter] public EventCallback<FeaturedSlot> ReplaceSlot { get; set; }
        [Parameter] public EventCallback<DailyLivingArticleDto> AddItem { get; set; }
        [Parameter] public EventCallback<DailyLivingArticleDto> ReplaceItem { get; set; }
        [Parameter] public EventCallback RenameTopic { get; set; }
        [Parameter] public int activeIndex { get; set; }
        [Parameter] public EventCallback UpdateEvent { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }
        public List<TopicSlot> ReplaceTopicsSlot { get; set; }
        [Parameter]
        public DailyTopic selectedTopic { get; set; }
        public DailyLivingArticleDto selectedItem { get; set; }
        [Parameter]
        public List<EventCategoryModel> Categories { get; set; } = [];
        [Parameter] public List<DailyLivingArticleDto> articles { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Parameter]
        public bool IsLoadingEvent { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] public IDailyLivingService DailyService { get; set; }
        [Inject] protected ILogger<FeaturedEventSlotsBase> Logger { get; set; }
        private string UserId => CurrentUserId.ToString();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            AuthorizedPage();
        }
        private bool shouldReinitializeSortable = false;


        protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (articles != null && articles.Any())
        {
            ReplaceTopicsSlot = articles
                .Select(article => new TopicSlot
                {
                    SlotNumber = article.SlotNumber,
                    Article = article
                })
                .ToList();
        }
        else
        {
            ReplaceTopicsSlot = new();
        }

        // Flag JS reinitialization
        shouldReinitializeSortable = true;
    }


        protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || shouldReinitializeSortable)
        {
            await JS.InvokeVoidAsync("initializeSortable", ".featured-table", DotNetObjectReference.Create(this));
            shouldReinitializeSortable = false;
        }
    }
        protected async Task OnAddItemClicked()
        {
            await AddItem.InvokeAsync(selectedItem);
        }
        protected async Task ReplaceArticle(DailyLivingArticleDto article) 
        {
            await ReplaceItem.InvokeAsync(article);
        }
        protected async Task RenameTopicaOnClick()
        {
            await RenameTopic.InvokeAsync();
        }
        protected async Task UpdateEventOnClick()
        {
            await UpdateEvent.InvokeAsync();
        }
        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
{
    var newSlotOrder = newOrder.Select(int.Parse).ToList();

    var eventMap = ReplaceTopicsSlot
        .Where(s => s.Article != null && !string.IsNullOrWhiteSpace(s.Article.Id))
        .ToDictionary(s => s.SlotNumber, s => s.Article!.Id);

    var slotAssignments = newSlotOrder.Select((slotNumber, index) =>
    {
        if (eventMap.TryGetValue(slotNumber, out var stringId) &&
            Guid.TryParse(stringId, out var parsedGuid) &&
            parsedGuid != Guid.Empty)
        {
            return new DailySlotAssignment
            {
                SlotNumber = index + 1,
                DailyId = parsedGuid
            };
        }

        return null;
    })
    .Where(sa => sa != null)
    .ToList()!;
    var request = new DailySlotAssignmentRequest
    {
        TopicId = selectedTopic != null ? Guid.Parse(selectedTopic.Id) : Guid.Empty,
        SlotAssignments = slotAssignments,
        UserId = UserId
    };
    var response = await DailyService.ReorderFeaturedSlots(request);

    if (response.IsSuccessStatusCode)
    {
        Snackbar.Add("Slots reordered successfully.", Severity.Success);
    }
    else
    {
        Snackbar.Add("Failed to reorder slots", Severity.Error);
        Logger.LogError("Reorder API failed: {StatusCode}", response.StatusCode);
    }
}

    }
}
