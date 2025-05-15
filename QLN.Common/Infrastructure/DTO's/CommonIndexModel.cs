using QLN.Common.Infrastructure.DTO_s;

namespace QLN.SearchService.Models
{
    public class CommonIndexModel
    {
        public string VerticalName { get; set; } = string.Empty;
        public ClassifiedIndexDto? Classified { get; set; }

    }
}
