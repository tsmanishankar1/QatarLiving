using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.CompilerServices;

    namespace QLN.Web.Shared.Models
    {
        public class CompanyModel : INotifyPropertyChanged
        {
            private int _verticalId;
            private string _businessName = string.Empty;
            private string _country = string.Empty;
            private string _city = string.Empty;
            private string _branchLocations = string.Empty;
            private string _phoneNumber = string.Empty;
            private string _whatsAppNumber = string.Empty;
            private string _email = string.Empty;
            private string _websiteUrl = string.Empty;
            private string _facebookUrl = string.Empty;
            private string _instagramUrl = string.Empty;
            private string _startDay = string.Empty;
            private string _endDay = string.Empty;
            private string _startHour = string.Empty;
            private string _endHour = string.Empty;
            private string _natureOfBusiness = string.Empty;
            private string _companySize = string.Empty;
            private string _companyType = string.Empty;
            private string _userDesignation = string.Empty;
            private string _businessDescription = string.Empty;
            private string _crNumber = string.Empty;
            private string _crDocumentPath = string.Empty;
            public string? LogoBase64 { get; set; }
            public string? DocumentBase64 { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public int VerticalId
            {
                get => _verticalId;
                set
                {
                    if (_verticalId != value)
                    {
                        _verticalId = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Required(ErrorMessage = "Business Name is required")]
            [StringLength(100, ErrorMessage = "Business Name cannot exceed 100 characters")]
            public string BusinessName
            {
                get => _businessName;
                set
                {
                    if (_businessName != value)
                    {
                        _businessName = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Required(ErrorMessage = "Country is required")]
            [StringLength(56, ErrorMessage = "Country name cannot exceed 56 characters")] // Max length of country names
            public string Country
            {
                get => _country;
                set
                {
                    if (_country != value)
                    {
                        _country = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Required(ErrorMessage = "City is required")]
            [StringLength(85, ErrorMessage = "City name cannot exceed 85 characters")]
            public string City
            {
                get => _city;
                set
                {
                    if (_city != value)
                    {
                        _city = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(500, ErrorMessage = "Branch Locations info is too long")]
            public string BranchLocations
            {
                get => _branchLocations;
                set
                {
                    if (_branchLocations != value)
                    {
                        _branchLocations = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Required(ErrorMessage = "Phone Number is required")]
            [Phone(ErrorMessage = "Invalid Phone Number")]
            [StringLength(15, ErrorMessage = "Phone Number cannot exceed 15 digits")]
            public string PhoneNumber
            {
                get => _phoneNumber;
                set
                {
                    if (_phoneNumber != value)
                    {
                        _phoneNumber = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Phone(ErrorMessage = "Invalid WhatsApp Number")]
            [StringLength(15, ErrorMessage = "WhatsApp Number cannot exceed 15 digits")]
            public string WhatsAppNumber
            {
                get => _whatsAppNumber;
                set
                {
                    if (_whatsAppNumber != value)
                    {
                        _whatsAppNumber = value;
                        OnPropertyChanged();
                    }
                }
            }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid Email Address")]
            [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
            public string Email
            {
                get => _email;
                set
                {
                    if (_email != value)
                    {
                        _email = value;
                        OnPropertyChanged();
                    }
                }
            }
            [Required(ErrorMessage = "Website is required")]
            [Url(ErrorMessage = "Invalid Website URL")]
            [StringLength(255, ErrorMessage = "Website URL cannot exceed 255 characters")]
            public string WebsiteUrl
            {
                get => _websiteUrl;
                set
                {
                    if (_websiteUrl != value)
                    {
                        _websiteUrl = value;
                        OnPropertyChanged();
                    }
                }
            }
            [Required(ErrorMessage = "Facebook URL is required")]
            [Url(ErrorMessage = "Invalid Facebook URL")]
            [StringLength(255, ErrorMessage = "Facebook URL cannot exceed 255 characters")]
            public string FacebookUrl
            {
                get => _facebookUrl;
                set
                {
                    if (_facebookUrl != value)
                    {
                        _facebookUrl = value;
                        OnPropertyChanged();
                    }
                }
            }
            [Required(ErrorMessage = "Instagram URL is required")]

            [Url(ErrorMessage = "Invalid Instagram URL")]
            [StringLength(255, ErrorMessage = "Instagram URL cannot exceed 255 characters")]
            public string InstagramUrl
            {
                get => _instagramUrl;
                set
                {
                    if (_instagramUrl != value)
                    {
                        _instagramUrl = value;
                        OnPropertyChanged();
                    }
                }
            }

            [RegularExpression(@"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)$", ErrorMessage = "Invalid Start Day")]
            public string StartDay
            {
                get => _startDay;
                set
                {
                    if (_startDay != value)
                    {
                        _startDay = value;
                        OnPropertyChanged();
                    }
                }
            }

            [RegularExpression(@"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)$", ErrorMessage = "Invalid End Day")]
            public string EndDay
            {
                get => _endDay;
                set
                {
                    if (_endDay != value)
                    {
                        _endDay = value;
                        OnPropertyChanged();
                    }
                }
            }

            [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "Invalid Start Hour format (HH:mm)")]
            public string StartHour
            {
                get => _startHour;
                set
                {
                    if (_startHour != value)
                    {
                        _startHour = value;
                        OnPropertyChanged();
                    }
                }
            }

            [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "Invalid End Hour format (HH:mm)")]
            public string EndHour
            {
                get => _endHour;
                set
                {
                    if (_endHour != value)
                    {
                        _endHour = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(50, ErrorMessage = "Nature of Business cannot exceed 50 characters")]
            public string NatureOfBusiness
            {
                get => _natureOfBusiness;
                set
                {
                    if (_natureOfBusiness != value)
                    {
                        _natureOfBusiness = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(50, ErrorMessage = "Company Size cannot exceed 50 characters")]
            public string CompanySize
            {
                get => _companySize;
                set
                {
                    if (_companySize != value)
                    {
                        _companySize = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(100, ErrorMessage = "Company Type cannot exceed 100 characters")]
            public string CompanyType
            {
                get => _companyType;
                set
                {
                    if (_companyType != value)
                    {
                        _companyType = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(100, ErrorMessage = "User Designation cannot exceed 100 characters")]
            public string UserDesignation
            {
                get => _userDesignation;
                set
                {
                    if (_userDesignation != value)
                    {
                        _userDesignation = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(1000, ErrorMessage = "Business Description cannot exceed 1000 characters")]
            public string BusinessDescription
            {
                get => _businessDescription;
                set
                {
                    if (_businessDescription != value)
                    {
                        _businessDescription = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(50, ErrorMessage = "CR Number cannot exceed 50 characters")]
            public string CRNumber
            {
                get => _crNumber;
                set
                {
                    if (_crNumber != value)
                    {
                        _crNumber = value;
                        OnPropertyChanged();
                    }
                }
            }

            [StringLength(500, ErrorMessage = "CR Document Path cannot exceed 500 characters")]
            public string CRDocumentPath
            {
                get => _crDocumentPath;
                set
                {
                    if (_crDocumentPath != value)
                    {
                        _crDocumentPath = value;
                        OnPropertyChanged();
                    }
                }
            }

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}