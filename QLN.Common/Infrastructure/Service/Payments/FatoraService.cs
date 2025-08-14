using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.Subscriptions;
using System.Text.Json;

namespace QLN.Common.Infrastructure.Service.Payments
{
    public class FatoraService : IFatoraService
    {
        private readonly FatoraConfig _fatoraConfig;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FatoraService> _logger;

        public FatoraService(
            IOptions<FatoraConfig> fatoraConfig, 
            HttpClient httpClient,
            ILogger<FatoraService> logger
            )
        {
            _fatoraConfig = fatoraConfig.Value;
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<PaymentResponse> CreatePaymentAsync(ExternalPaymentRequest request, string username,string productCode, Vertical vertical, SubVertical? subVertical, string? email, string? mobile, string? platform, CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentDetails = BuildPaymentCheckoutPayload(request, username,productCode, vertical, subVertical, email, mobile, platform);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_fatoraConfig.ApiUrl}/checkout")
                {
                    Content = new StringContent(JsonSerializer.Serialize(paymentDetails), System.Text.Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Add("api_key", _fatoraConfig.ApiKey);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return paymentResponse ?? new PaymentResponse { Status = "ERROR", Error = new FaturaPaymentError { Message = "Empty response from Fatura" } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message ?? "Error has been encountered while payment", ex);
            }

            return new PaymentResponse
            {
                Status = "ERROR",
                Error = new FaturaPaymentError { Message = "An error occurred while processing the payment." }
            };
        }
        public async Task<FatoraVerificationResponse> VerifyPayment(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_fatoraConfig.ApiUrl}/verify")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { order_id = orderId }), System.Text.Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Add("api_key", _fatoraConfig.ApiKey);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var verificationResponse = JsonSerializer.Deserialize<FatoraVerificationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return verificationResponse ?? new FatoraVerificationResponse
                {
                    Status = "ERROR",
                    Error = new PaymentVerificationFailureResponse
                    {
                        ErrorCode = "EMPTY_RESPONSE",
                        Description = "Empty response from Fatura"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while verifying payment for orderId: {OrderId}", orderId);
            }

            return new FatoraVerificationResponse
            {
                Status = "ERROR",
                Error = new PaymentVerificationFailureResponse
                {
                    ErrorCode = "VERIFICATION_ERROR",
                    Description = "An error occurred while verifying the payment."
                }
            };
        }

        private FatoraPaymentRequest BuildPaymentCheckoutPayload(ExternalPaymentRequest request, string username, string productCode, Vertical vertical, SubVertical? subVertical, string? email, string? mobile, string? platform = "web")
        {

            var query = $"?platform={Uri.EscapeDataString(platform ?? "web")}&vertical={Uri.EscapeDataString(vertical.ToString())}&subvertical={Uri.EscapeDataString(subVertical?.ToString() ?? string.Empty)}&product_code={Uri.EscapeDataString(productCode)}";

            var successUrl = $"{_fatoraConfig.BaseUrl}/{_fatoraConfig.SuccessUrl}{query}";
            var failureUrl = $"{_fatoraConfig.BaseUrl}/{_fatoraConfig.FailureUrl}{query}";

            return new FatoraPaymentRequest
            {
                Amount = request.Amount ?? 0,
                Currency = "QAR",
                OrderId = request.OrderId,
                Client = new FaturaClientInfo
                {
                    Name = username,
                    Email = email ?? null,
                    Phone = mobile ?? null
                },
                Language = "ar",
                SuccessUrl = successUrl,
                FailureUrl = failureUrl
            };
        }
    }
}
