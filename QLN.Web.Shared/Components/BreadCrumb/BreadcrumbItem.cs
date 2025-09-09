namespace QLN.Web.Shared.Components.BreadCrumb
{
    public class BreadcrumbItem
    {
        public BreadcrumbItem() { }

        public BreadcrumbItem(string label, string? url, string? iconClass = null, bool isComplete = false, bool isLast = false)
        {
            Label = label;
            Url = url;
            IconClass = iconClass ?? "fa fa-angle-right";
            IsComplete = isComplete;
            IsLast = isLast;
        }

        public string Label { get; set; }
        public string? Url { get; set; }
        public string IconClass { get; set; } = "fa fa-angle-right";
        public bool IsComplete { get; set; }
        public bool IsLast { get; set; }
    }
}