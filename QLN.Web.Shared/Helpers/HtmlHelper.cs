using System.Text.RegularExpressions;

namespace QLN.Web.Shared.Helpers
{
    public partial class HtmlHelper
    {
        [GeneratedRegex(@"<[^>]+>|&nbsp;")]
        private static partial Regex HtmlRegex1();

        [GeneratedRegex(@"\s{2,}")]
        private static partial Regex HtmlRegex2();

        public static string StripHTML(string input)
        {
            try
            {
                string noHTML = HtmlRegex1().Replace(input, string.Empty).Trim();
                string noHTMLNormalised = HtmlRegex2().Replace(noHTML, " ");
                return noHTMLNormalised;
            }
            catch
            {
                return input;
            }
        }
    }
}
