
namespace QLN.Common.DTO_s
{
    public class ClassifiedsSearchResponse
    {
        public string? VerticalName { get; set; }
        public List<ClassifiedsIndex>? ClassifiedsItems { get; set; }
        public object? ServicesItems { get; set; }
        public object? MasterItems { get; set; }
        public int TotalCount { get; set; } 
    }  
}