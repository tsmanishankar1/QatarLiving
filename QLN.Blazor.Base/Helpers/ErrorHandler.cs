
using QLN.Blazor.Base.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
public static class ErrorHandler
{
    public static ModalData HandleApiError<T>(ResponseModel<T> response)
{
    var modalData = new ModalData
    {
        Open = true,
        Type = "error",
        Header = "Error",
        Body = response?.Message ?? "An unexpected error occurred.",
        ShowLogout = false
    };

    if (response != null)
    {
        var status = response.StatusCode;
        var errorDetail = response.Message?.Trim() ?? "";

        switch (status)
        {
            case 400:
            case 422:
                modalData.Body = errorDetail;
                break;

            case 401:
            case 403:
                modalData.Body = errorDetail;
                modalData.ShowLogout = true;
                break;

            case 409:
                var detailLower = errorDetail.ToLowerInvariant();
                if (detailLower.Contains("a client with this name already exists."))
                    modalData.Body = "A client with this name already exists.";
                else if (detailLower.Contains("a client with this email already exists."))
                    modalData.Body = "A client with this email already exists.";
                else
                    modalData.Body = errorDetail;
                break;

            case 500:
            default:
                modalData.Body = !string.IsNullOrWhiteSpace(errorDetail)
                    ? errorDetail
                    : "An unexpected error occurred.";
                break;
        }
    }
    else
    {
        modalData.Header = "Network Error";
        modalData.Body = "Unable to connect to the server.";
    }

    return modalData;
}
    
        public static async Task<ResponseModel<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
{
    var json = await response.Content.ReadAsStringAsync();

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true 
    };

    var result = JsonSerializer.Deserialize<ResponseModel<T>>(json, options);

    return result;
}


}