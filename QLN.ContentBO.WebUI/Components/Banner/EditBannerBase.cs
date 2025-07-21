using System;
using System.Collections.Generic;
using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components.SuccessModal;
using System.Text.Json;
using System.Net;


namespace QLN.ContentBO.WebUI.Components.Banner
{
    public class EditBannerBase : QLComponentBase
    {
        [Parameter]
        public Guid Id { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
         [Inject] protected NavigationManager Navigation { get; set; }
        protected BannerDTO bannerModel = new();
        [Inject] IBannerService bannerService { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        protected List<string> BannerSizes = new()
        {
    "340 x 384",
    "300 x 250",
    "1170 x 150",
    "1200 x 250",
    "1210 x 150",
    "1170 x 250",
    "970 x 250"
    };
        protected string? selectedPage;
        protected string? _DateError;
        protected string? _DesktopImageError;
        protected string? _MobileImageError;
        protected string? _AvailabilityError;
        protected DateTime? _startDate;
        protected DateTime? _endDate;
        protected string? selectedTypeValue;
        protected TimeSpan? _startHour = new TimeSpan(8, 0, 0);
        protected TimeSpan? _endHour = new TimeSpan(17, 0, 0);
        public List<BannerType> bannerTypes { get; set; } = new();
        public List<BannerPageLocationDto> bannerPageTypes { get; set; } = new();
        protected MudMenu _menu;
        protected string _displayText = "";
        protected HashSet<Guid> _selectedBannerIds = new();
        protected List<BannerTypeRequest> _selectedBannerTypeRequests = new();
        protected bool IsChecked(BannerLocationDto item) => _selectedBannerIds.Contains(item.Id);
        protected void OnCheckedChanged(BannerLocationDto item, bool isChecked, int? verticalId, int? subVerticalId, Guid pageId)
        {
            if (!verticalId.HasValue)
                return;
            var request = new BannerTypeRequest
            {
                BannerTypeId = item.Id,
                VerticalId = (Vertical)verticalId.Value,
                SubVerticalId = subVerticalId.HasValue ? (SubVertical?)subVerticalId.Value : null,
                PageId = pageId
            };

            if (isChecked)
            {
                if (!_selectedBannerIds.Contains(item.Id))
                    _selectedBannerIds.Add(item.Id);

                if (!_selectedBannerTypeRequests.Any(r =>
                    r.BannerTypeId == request.BannerTypeId &&
                    r.PageId == request.PageId &&
                    r.VerticalId == request.VerticalId &&
                    r.SubVerticalId == request.SubVerticalId))
                {
                    _selectedBannerTypeRequests.Add(request);
                }
            }
            else
            {
                _selectedBannerIds.Remove(item.Id);

                _selectedBannerTypeRequests.RemoveAll(r =>
                    r.BannerTypeId == request.BannerTypeId &&
                    r.PageId == request.PageId &&
                    r.VerticalId == request.VerticalId &&
                    r.SubVerticalId == request.SubVerticalId);
            }

            UpdateDisplayText();
        }
        protected void UpdateDisplayText()
        {
            var selectedNames = bannerPageTypes
                .SelectMany(g => g.bannertypes)
                .Where(b => _selectedBannerIds.Contains(b.Id))
                .Select(b => b.BannerTypeName);

            _displayText = string.Join(", ", selectedNames);
        }

        public class GroupedOption
        {
            public string Title { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
        }
        protected IEnumerable<Guid> _options
        {
            get => _selectedOptions;
            set => _selectedOptions = value.ToList();
        }

        protected List<Guid> _selectedOptions = new();

        protected List<GroupedOption> _groupedOptions = new()
{
    new GroupedOption
    {
        Title = "VEHICLES",
        Options = new List<string>
        {
            "Landing Hero (1170 x 250)",
            "Detail Side (300 x 250)",
            "Detail Hero (1170 x 150)",
            "Search Hero (1170 x 150)",
            "Payment Hero (1170 x 150)"
        }
    },
    new GroupedOption
    {
        Title = "PROPERTIES",
        Options = new List<string>
        {
            "Banner Top (970 x 90)",
            "Sidebar Ad (300 x 600)"
        }
    }
};

        protected List<string> _selectedValues = new();

        protected HashSet<string> selectedOptions = new()
    {
        "Search Hero (1170 x 150)",
        "Payment Hero (1170 x 150)"
    };
        protected bool IsLoading = false;
        public enum Status
        {
            Active,
            Inactive
        }
        public Status CurrentStatus { get; set; } = Status.Active;

        protected override async Task OnInitializedAsync()
        {
            bannerTypes = await GetBannerTypes();
            bannerPageTypes = bannerTypes
                .Where(bt => bt.Pages != null)
                .SelectMany(bt => bt.Pages!)
                .ToList();
            bannerModel = await GetBannerById();
             _selectedBannerTypeRequests = GetBannerTypeRequestsByBannerTypeId(bannerPageTypes, bannerModel.BannerTypeId);
            var selectedNames = bannerPageTypes
                .SelectMany(g => g.bannertypes)
                .Where(b => _selectedBannerTypeRequests.Any(r => r.BannerTypeId == b.Id))
                .Select(b => b.BannerTypeName);
            _displayText = string.Join(", ", selectedNames);
            ConvertDateOnlyToDateTime(bannerModel.StartDate, bannerModel.EndDate);
        }
        
        protected void ConvertDateOnlyToDateTime(DateOnly? startDate, DateOnly? endDate)
        {
            _startDate = startDate?.ToDateTime(TimeOnly.MinValue);
            _endDate = endDate?.ToDateTime(TimeOnly.MinValue);
        }

        private async Task ShowSuccessModal(string title)
        {
            var parameters = new DialogParameters
            {
                { nameof(SuccessModalBase.Title), title },
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true
            };

            await DialogService.ShowAsync<SuccessModal.SuccessModal>("", parameters, options);
        }


        protected async Task HandleDesktopImageChanged(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                bannerModel.DesktopImage = $"data:{file.ContentType};base64,{base64}";
            }
        }
        public List<BannerTypeRequest> GetBannerTypeRequestsByBannerTypeId(List<BannerPageLocationDto> bannerPages, string targetBannerTypeIdStr)
        {
            var bannerTypeRequests = new List<BannerTypeRequest>();

            if (!Guid.TryParse(targetBannerTypeIdStr, out Guid targetBannerTypeId))
            {
                return bannerTypeRequests;
            }

            foreach (var page in bannerPages)
            {
                foreach (var bannerType in page.bannertypes)
                {
                    if (bannerType.Id == targetBannerTypeId)
                    {
                        var bannerTypeRequest = new BannerTypeRequest
                        {
                            BannerTypeId = bannerType.Id,
                            VerticalId = (Vertical)(page.VerticalId ?? 0),
                            SubVerticalId = page.SubVerticalId.HasValue ? (SubVertical?)page.SubVerticalId.Value : null,
                            PageId = page.Id
                        };

                        bannerTypeRequests.Add(bannerTypeRequest);
                        return bannerTypeRequests;
                    }
                }
            }

            return bannerTypeRequests;
        }


        protected async Task HandleMobileImageChanged(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                bannerModel.MobileImage = $"data:{file.ContentType};base64,{base64}";
            }
        }
        protected async void OnCancelClicked()
        {
            Navigation.NavigateTo("/manage/banner");
        }


        protected void DeleteImage(string propName)
        {
            if (propName == nameof(bannerModel.DesktopImage))
                bannerModel.DesktopImage = null;
            if (propName == nameof(bannerModel.MobileImage))
                bannerModel.MobileImage = null;
        }


        protected async Task HandleValidSubmit()
        {
            _DesktopImageError = null;
            _MobileImageError = null;
            _AvailabilityError = null;
            bannerModel.BannerTypeIds = _selectedBannerTypeRequests;
            if (_startDate == null)
            {
                _DateError = "Start date is required.";
                Snackbar.Add("Start date is required.", severity: Severity.Error);
                return;
            }
            if (_endDate == null)
            {
                _DateError = "End date is required.";
                Snackbar.Add("End date is required.", severity: Severity.Error);
                return;
            }
            if (_startDate != null && _endDate != null && _endDate <= _startDate)
            {
                _DateError = "End date must be after the start date.";
                Snackbar.Add("End date must be after the start date.", severity: Severity.Error);
                return;
            }
            if (bannerModel.IsDesktopAvailability != true && bannerModel.IsMobileAvailability != true)
            {
                _AvailabilityError = "At least one availability option must be selected.";
                Snackbar.Add("At least one availability option must be selected.", severity: Severity.Error);
                return;
            }
            if (bannerModel.IsDesktopAvailability == true && string.IsNullOrWhiteSpace(bannerModel.DesktopImage))
            {
                _DesktopImageError = "Desktop Image is required.";
                Snackbar.Add("Desktop Image is required.", severity: Severity.Error);
                return;
            }
            if (bannerModel.IsMobileAvailability == true && string.IsNullOrWhiteSpace(bannerModel.MobileImage))
            {
                _MobileImageError = "Mobile Image is required.";
                Snackbar.Add("Mobile Image is required.", severity: Severity.Error);
                return;
            }
            if (_startDate.HasValue && _endDate.HasValue)
            {
                var startDateOnly = DateOnly.FromDateTime(_startDate.Value);
                var endDateOnly = DateOnly.FromDateTime(_endDate.Value);
                bannerModel.StartDate = startDateOnly;
                bannerModel.EndDate = endDateOnly;
            }
            try
            {
                IsLoading = true;
                var response = await bannerService.UpdateBanner(bannerModel);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await ShowSuccessModal("Banner Edited successfullly");
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateBanner");
            }
            finally
            {
                IsLoading = false;
            }

        }
        private async Task<List<BannerType>> GetBannerTypes()
        {
            try
            {
                var selectedVertical = 5;
                var apiResponse = await bannerService.GetBannerTypes();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var responseContent = await apiResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var bannerTypes = JsonSerializer.Deserialize<List<BannerType>>(responseContent, options) ?? [];
                    foreach (var bannerType in bannerTypes)
                    {
                        if (bannerType.Pages != null)
                        {
                            foreach (var page in bannerType.Pages)
                            {
                                page.VerticalId = (int)bannerType.VerticalId;
                                page.SubVerticalId = (int?)bannerType.SubVerticalId;
                            }
                        }
                    }

                    return bannerTypes;
                }
                else
                {
                    Snackbar.Add("Internal API Error");
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerTypes");
                return [];
            }
        }
        private async Task<BannerDTO> GetBannerById()
        {
            try
            {
                var apiResponse = await bannerService.GetBannerById(Id);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var responseContent = await apiResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<BannerDTO>(responseContent, options) ?? new BannerDTO();
                }
                return new BannerDTO();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerById");
                return new BannerDTO();
            }
        }





    }
}
