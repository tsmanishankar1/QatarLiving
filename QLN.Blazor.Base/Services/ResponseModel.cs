namespace QLN.Blazor.Base.Models // Or any other appropriate namespace
{
    public class ResponseModel<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int StatusCode { get; set; }
    }
}
