using System;
using System.Collections.Generic;
using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.SuccessModal;
using System.Text.Json;
using System.Net;


namespace QLN.ContentBO.WebUI.Components.Banner
{
    public class CreateBannerBase : QLComponentBase
    {
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        protected BannerDTO bannerModel = new();
        [Parameter]
        public Guid? BannerTypeId { get; set; }
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
        protected string? _BannerError;
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
        protected List<BannerPageLocationDto> FilteredGroups =>
        bannerPageTypes
            .Select(group => new BannerPageLocationDto
            {
                Id = group.Id,
                VerticalId = group.VerticalId,
                SubVerticalId = group.SubVerticalId,
                BannerPageName = group.BannerPageName,
                bannertypes = group.bannertypes
                    .Where(x => string.IsNullOrEmpty(bannerModel.BannerSize) || x.Dimensions == bannerModel.BannerSize)
                    .ToList()
            })
            .Where(g => g.bannertypes.Any())
            .ToList();


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
            if (BannerTypeId is Guid id)
            {
                _selectedBannerIds.Add(id);
                UpdateDisplayText();
            }


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
                Snackbar.Add("Desktop Image Uploaded Successfully", severity: Severity.Success);

            }
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
                Snackbar.Add("Mobile Image Uploaded Successfully", severity: Severity.Success);
            }
        }
        protected async void OnCancelClicked()
        {
            await ShowConfirmation(
            "Discard Banner",
            "Are you sure you want to Discard this Banner?",
            "Discard", async () => ClearForm());
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
            _DateError = null;
            _BannerError = null;
            bannerModel.BannerTypeIds = _selectedBannerTypeRequests;
            if (bannerModel.BannerTypeIds == null || !bannerModel.BannerTypeIds.Any())
            {
                _BannerError = "Select Atleast One banner Location";
                Snackbar.Add("Select Atleast One banner Location", severity: Severity.Error);
                return;
            }
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
                var response = await bannerService.CreateBanner(bannerModel);
                if (response != null && response.IsSuccessStatusCode)
                {
                    ClearForm();
                    await ShowSuccessModal("Banner Created successfully!");
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
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerTypes");
                return [];
            }
        }
        private void ClearForm()
        {
            bannerModel = new BannerDTO
            {
                Id = Guid.Empty,
                Status = true,
                slotId = null,
                BannerTypeIds = new List<BannerTypeRequest>(),
                BannerTypeId = string.Empty,
                AnalyticsTrackingId = string.Empty,
                AltText = string.Empty,
                LinkUrl = string.Empty,
                Duration = 5,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                BannerSize = string.Empty,
                IsDesktopAvailability = null,
                IsMobileAvailability = null,
                DesktopImage = null,
                MobileImage = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            StateHasChanged();
        }
        protected async Task ShowConfirmation(string title, string description, string buttonTitle, Func<Task> onConfirmedAction)
        {
            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle", buttonTitle },
            { "OnConfirmed", EventCallback.Factory.Create(this, onConfirmedAction) }
        };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog.ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
    
    }
}
