using System;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class PaginatedEventResponse

    {
        public List<EventDTOV2> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PerPage { get; set; }

        public int FeaturedCount { get; set; }

        public int FeaturedInCurrentPage { get; set; }

    }
}
 