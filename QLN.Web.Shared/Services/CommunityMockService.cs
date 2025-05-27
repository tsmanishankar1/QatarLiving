using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public class CommunityMockService : ICommunityService
    {
        private List<PostModel> PostList = new()
        {
            new PostModel
            {
                Id = "1",
                Category = "Advice & Help",
                Title = "Anyone wanna buy 5x Seated Travis Scott Tickets?",
                ImageUrl = "images/content/Post1.svg",
                Author = "Ismat Zerin",
                Time = DateTime.Now.AddHours(-2),
                LikeCount = 3,
                CommentCount = 12
            },
            new PostModel
            {
                Id = "1",
                Category = "Visa and Permits",
                Title = "Family Residence Visa status stuck",
                BodyPreview = "Looking for some advice or similar experiences. 15 April – Applied for Family Residence Visa 16 April – Uploaded missing document and resubmitted Status remained “Under Process” for 3 weeks 5 May – Visited Duhail Immigration office but was told: “Private companies come after 2 weeks” and they didn’t allow me in 7 ...",
                Author = "Ismat Zerin",
                Time = DateTime.Now.AddHours(-2),
                LikeCount = 3,
                CommentCount = 12
            },
            new PostModel
            {
                Id = "1",
                Category = "Visa and Permits",
                Title = "Family Residence Visa status stuck",
                ImageUrl = "images/content/Post2.svg",
                BodyPreview = "Looking for some advice or similar experiences. 15 April – Applied for Family Residence Visa 16 April – Uploaded missing document and resubmitted Status remained “Under Process” for 3 weeks 5 May – Visited Duhail Immigration office but was told: “Private companies come after 2 weeks” and they didn’t allow me in 7 ...",
                Author = "Ismat Zerin",
                Time = DateTime.Now.AddHours(-2),
                LikeCount = 3,
                CommentCount = 12
            }
        };

        public async Task<IEnumerable<PostModel>> GetAllAsync()
        {

            return PostList;
        }

    }
}