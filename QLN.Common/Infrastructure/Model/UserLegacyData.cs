using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class UserLegacyData
    {
        public Guid UserId { get; set; }
        public required long Uid { get; set; }
        public required string Status { get; set; }
        public string? Language { get; set; }
        public required string Created { get; set; }
        public required string Access { get; set; }
        public required string Init { get; set; }
        public string? QlnextUserId { get; set; }
        public string? Name { get; set; }
        public string? Alias { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Path { get; set; }
        public string? Image { get; set; }
        public bool IsAdmin { get; set; }
        public List<string>? Permissions { get; set; }
        public List<string>? Roles { get; set; }
        //public List<object> ShowroomInfo { get; set; }
    }

    public class LegacySubscription
    {
        public Guid UserId { get; set; }
        public required long Uid { get; set; }
        public required string ReferenceId { get; set; }
        public required string StartDate { get; set; }
        public required string ExpireDate { get; set; }
        public required string ProductType { get; set; }
        public string? AccessDashboard { get; set; }
        public required string ProductClass { get; set; }
        public List<string>? Categories { get; set; }
        public List<string>? SubscriptionCategories { get; set; }
        public required string Snid { get; set; }
        public required string Status { get; set; }
    }
}
