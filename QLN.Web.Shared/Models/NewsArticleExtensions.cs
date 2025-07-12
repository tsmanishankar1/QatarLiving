using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public static class NewsArticleExtensions
    {
        public static ContentPost ToContentPost(this NewsArticleDTO article)
        {
            // Convert to GMT+1
            var gmtPlus1 = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); 
            var publishedDate = TimeZoneInfo.ConvertTimeFromUtc(article.PublishedDate == default ? article.CreatedAt : article.PublishedDate, gmtPlus1);

            return new ContentPost
            {
                Id = article.Id,
                User_id = Guid.TryParse(article.UserId, out var uid) ? uid : Guid.Empty,
                IsActive = article.IsActive,
                CreatedBy = Guid.TryParse(article.CreatedBy, out var cb) ? cb : Guid.Empty,
                CreatedAt = article.CreatedAt,
                UpdatedBy = Guid.TryParse(article.UpdatedBy, out var ub) ? ub : null,
                UpdatedAt = article.UpdatedAt,

                Nid = article.Id.ToString(),
                DateCreated = publishedDate.ToString("MMMM d, yyyy 'at' h:mm tt 'GMT+1'", CultureInfo.InvariantCulture),
                ImageUrl = article.CoverImageUrl ?? string.Empty,
                UserName = article.authorName,
                ForumId = string.Empty,
                Title = article.Title,
                Description = article.Content,
                Category = string.Empty,
                Slug = article.Slug,
                Comments = []
            };
        }
    }
}
