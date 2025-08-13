using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
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

        // First you create a payment and build a payment checkout payload, this is provided back to the user as a redirect URL
        // to the payment gateway payment processing page.
        // After the user completes the payment, they are redirected back to your application with a success or failure
        public async Task<PaymentResponse> CreatePaymentAsync(ExternalPaymentRequest request, string username, string? email, string? mobile, string? platform, CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentDetails = BuildPaymentCheckoutPayload(request, username, email, mobile, platform);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_fatoraConfig.ApiUrl}/checkout")
                {
                    Content = new StringContent(JsonSerializer.Serialize(paymentDetails), System.Text.Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Add("x-api-key", _fatoraConfig.ApiKey);

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

        // After the user completes the payment, they are redirected back to your application with a success or failure
        // You then verify the payment by calling the Fatora API with the order ID to check if the payment was successful.
        public async Task<FatoraVerificationResponse> VerifyPayment(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_fatoraConfig.ApiUrl}/verify")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { order_id = orderId }), System.Text.Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Add("x-api-key", _fatoraConfig.ApiKey);

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

        // This method builds the payload for the payment checkout request to Fatora.
        // It includes the amount, currency, order ID, client information, language, and success/failure URLs.
        private FatoraPaymentRequest BuildPaymentCheckoutPayload(ExternalPaymentRequest request, string username, string? email, string? mobile, string? platform = "web")
        {
            // Success documentation https://fatora.io/api/standardCheckout.php#tab-complete-success
            // https://domain.com/payments/success?transaction_id=XXX&order_id=XXX&mode=XXX&response_code=XXX&description=XXX
            //
            // Failure documentation https://fatora.io/api/standardCheckout.php#tab-complete-failure
            // https://domain.com/payments/failure?transaction_id=XXX&order_id=XXX&mode=XXX&response_code=XXX&description=XXX
            // therefore we already have an order_id in the success or failure response, so can rely on this when providing a user with a redirect URL

            var vertical = request.Vertical.ToString();

            var query = $"?platform={platform}";

            // add the vertical to the redirect URL if it is not null or empty
            if (!string.IsNullOrWhiteSpace(vertical))
            {
                query += $"&vertical={vertical}";
            }

            var successUrl = $"{_fatoraConfig.BaseUrl}/{_fatoraConfig.SuccessPath}{query}";
            var failureUrl = $"{_fatoraConfig.BaseUrl}/{_fatoraConfig.FailurePath}{query}";

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
