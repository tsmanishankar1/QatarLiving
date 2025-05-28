using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostListBase :ComponentBase
    {
        protected List<PostItem> posts = new()
    {
        new PostItem("Family Residence Visa status stuck \"Under Review\".", "#"),
        new PostItem("A student raises the Palestinian flag at the graduation ceremony of the 2025 class of Doha University of Science and Technology (UDST) a short while ago 🇵🇸🎓", "#"),
        new PostItem("FFs Trump Qatar is safe! Your motorcade is causing inconvenience.", "#"),
        new PostItem("A Strange Experience on the Train. Was I Overthinking or Was She Just Rude?", "#"),
        new PostItem("Qatar’s $400m \"Gift\" To Trump?", "#")
    };

        protected record PostItem(string Title, string Url);
    }
}
