using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsFo
{
    public class StoresFoDto
    {
    }

    public class StoresDashboardHeader
    {
        public Guid? SubscriptionId { get; set; }
        public string? UserId { get; set; } = null;
        public string? UserName { get; set; } = null;
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; } = null;
        public string? SubscriptionType { get; set; } = null;
        public string? CompanyLogo { get; set; } = null;
        public int Status { get; set; }
        public int CompanyVerificationStatus { get; set; }
        public string? XMLFeed { get; set; } = null;
        public string? UploadFeed { get; set; } = null;
        public DateTime StartDate { get; set; } = default;
        public DateTime EndDate { get; set; }
        public DateTime StartDateForApi =>
    StartDate == default ? new DateTime(1000, 1, 1) : StartDate;
        public DateTime EndDateForApi =>
        EndDate == default ? new DateTime(1000, 1, 1) : EndDate;
    }
    public class StoresDashboardHeaderDto
    {
        public string? SubscriptionId { get; set; } 
        public string? UserId { get; set; } 
        public string? UserName { get; set; } = null;
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; } = null;
        public string? SubscriptionType { get; set; } = null;
        public string? CompanyLogo { get; set; } = null;
        public string? Status { get; set; } = null;
        public string? CompanyVerificationStatus { get; set; } = null;
        public string? XMLFeed { get; set; } = null;
        public string? UploadFeed { get; set; } = null;
        public DateTime StartDate { get; set; }=default;
        public DateTime EndDate { get; set; }
        public DateTime StartDateForApi =>
    StartDate == default ? new DateTime(1000, 1, 1) : StartDate;
        public DateTime EndDateForApi =>
        EndDate == default ? new DateTime(1000, 1, 1) : EndDate;
    }
    public class StoresDashboardSummary
    {
        public Guid? SubscriptionId { get; set; }
        public string? CompanyName { get; set; } = null;
        public Guid? CompanyId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public int ProductCount { get; set; } = 0;
     
    }
    public class StoresDashboardSummaryDto
    {
        public string? CompanyId { get; set; } = null;
        public string? CompanyName { get; set; } = null;
        public string? SubscriptionId { get; set; } = null;
        public string? SubscriptionType { get; set; } = null;
        public int InventoryTotal { get; set; } = 0;
        public int Inventory { get; set; } = 0;
        public int Impressions { get; set; } = 0;
        public int Views { get; set; } = 0;
        public int WebLeads { get; set; } = 0;
        public int Calls { get; set; } = 0;
        public int Emails { get; set; } = 0;
        
    }

    public class StoresDashboardManagementDto
    {
        public string? SyncStatus { get; set; } = null;
        public DateTime LastSyncedAt { get; set; } = default;
        public int ItemsFetched { get; set; } = 0;
        public int FailedFetches { get; set; } = 0;
        public DateTime LastSyncedAtForApi =>
    LastSyncedAt == default ? new DateTime(1000, 1, 1) : LastSyncedAt;
    }
}
