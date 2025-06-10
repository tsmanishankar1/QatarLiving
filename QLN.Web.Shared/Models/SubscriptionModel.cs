using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace QLN.Web.Shared.Models
{
 
        public class SubscriptionModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string _subscriptionName;
            [Required(ErrorMessage = "Subscription name is required")]
            public string SubscriptionName
            {
                get => _subscriptionName;
                set
                {
                    if (_subscriptionName != value)
                    {
                        _subscriptionName = value;
                        OnPropertyChanged();
                    }
                }
            }

            private decimal? _price;
            [Required(ErrorMessage = "Price is required")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
            public decimal? Price
            {
                get => _price;
                set
                {
                    if (_price != value)
                    {
                        _price = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _currency = "QAR";
            [Required(ErrorMessage = "Currency is required")]
            public string Currency
            {
                get => _currency;
                set
                {
                    if (_currency != value)
                    {
                        _currency = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _duration;
            [Required(ErrorMessage = "Duration is required")]
            public string Duration
            {
                get => _duration;
                set
                {
                    if (_duration != value)
                    {
                        _duration = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _verticalType;
            [Required(ErrorMessage = "Vertical type is required")]
            public string VerticalType
            {
                get => _verticalType;
                set
                {
                    if (_verticalType != value)
                    {
                        _verticalType = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _subCategory;
            [Required(ErrorMessage = "Sub category is required")]
            public string SubCategory
            {
                get => _subCategory;
                set
                {
                    if (_subCategory != value)
                    {
                        _subCategory = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _description;
            public string Description
            {
                get => _description;
                set
                {
                    if (_description != value)
                    {
                        _description = value;
                        OnPropertyChanged();
                    }
                }
            }
        public List<SubscriptionPlan> Subscriptions { get; set; } = new();
       
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
     
    }

}
