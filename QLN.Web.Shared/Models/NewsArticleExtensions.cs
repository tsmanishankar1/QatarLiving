using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public static class NewsArticleExtensions
    {
        public static ContentPost ToContentPost(this NewsArticleDTO article)
        {
            return new ContentPost
            {
                Id = article.Id,
                User_id = Guid.TryParse(article.UserId, out var uid) ? uid : Guid.Empty,
                IsActive = article.IsActive,
                CreatedBy = Guid.TryParse(article.CreatedBy, out var cb) ? cb : Guid.Empty,
                CreatedAt = article.CreatedAt,
                UpdatedBy = Guid.TryParse(article.UpdatedBy, out var ub) ? ub : null,
                UpdatedAt = article.UpdatedAt,

                Nid = article.Slug,
                DateCreated = article.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ImageUrl = article.CoverImageUrl ?? string.Empty,
                UserName = article.authorName,
                ForumId = article.Categories.FirstOrDefault()?.CategoryId.ToString(),
                Title = article.Title,
                Description = article.Content,
                Category = article.WriterTag,
                Comments = []
            };
        }
    }
}
