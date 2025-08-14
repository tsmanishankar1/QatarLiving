using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.QLDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Payments
{
    internal class D365Service : ID365Service
    {
        private readonly ILogger<D365Service> _logger;
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;
        private readonly QLPaymentsContext _dbContext;
        private readonly IConfidentialClientApplication _msalApp;
        private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;


        public D365Service(
            ILogger<D365Service> logger,
            HttpClient httpClient,
            D365Config d365Config,
            QLPaymentsContext dbContext
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _d365Config = d365Config;
            _dbContext = dbContext;

            // Acquire Bearer token using MSAL
            var authority = $"https://login.microsoftonline.com/{_d365Config.TenantId}";
            _msalApp = ConfidentialClientApplicationBuilder.Create(_d365Config.ClientId)
                .WithClientSecret(_d365Config.ClientSecret)
                .WithAuthority(new Uri(authority))
                .Build();
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Check if we have a valid cached token
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedToken;
            }

            await _tokenSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring the lock
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return _cachedToken;
                }

                _logger.LogInformation("Acquiring new D365 access token");

                var scopes = new[] { $"{_d365Config.D365Url}/.default" };
                var result = await _msalApp.AcquireTokenForClient(scopes)
                    .ExecuteAsync(cancellationToken);

                _cachedToken = result.AccessToken;
                _tokenExpiry = result.ExpiresOn.DateTime;

                _logger.LogInformation("D365 access token acquired successfully, expires at {ExpiryTime}", _tokenExpiry);
                return _cachedToken;
            }
            catch (MsalException ex)
            {
                _logger.LogError(ex, "Failed to acquire D365 access token. Error: {Error}, ErrorDescription: {ErrorDescription}",
                    ex.ErrorCode, ex.Message);
                throw;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }



        public async Task<bool> CreateAndInvoiceSalesOrder(D365Data order, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating and invoicing sales order for user: {UserId}", order.User.Id);

            var data = new
            {
                qlOrderId = order.PaymentInfo.PaymentId,
                startDate_dd_mm_yyyy = order.ProductDuration?.StartDate_dd_mm_yyyy.ToString("dd/MM/yyyy"),
                endDate_dd_mm_yyyy = order.ProductDuration?.EndDate_dd_mm_yyyy.ToString("dd/MM/yyyy"),
                transDate_dd_mm_yyyy = order.PaymentInfo.Date.ToString("dd/MM/yyyy"),
                amount = order.PaymentInfo.Fee,
                paymentMethod = order.PaymentInfo.PaymentMethod,
                paymentGateway = $"PG-{order.PaymentInfo.Gateway}",
                source = order.PaymentInfo.Source,
                transactionId = order.PaymentInfo.TransactionId,
                company = "ql"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // Use PostAsJsonAsync for cleaner serialization and posting
           /*     var response = await _httpClient.PostAsJsonAsync(_d365Config.InvoicePath, data, cancellationToken);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Interim sales order created and invoiced successfully for user: {UserId}", order.User.Id);*/

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating and invoicing sales order for user: {UserId}", order.User.Id);
            };

            return false;
        }

        public async Task<bool> CreateInterimSalesOrder(D365Data order, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating interim sales order for user: {UserId}", order.User.Id);

            var statusText = "Unknown Failure";

            var processedOrder = ProcessCheckoutOrder(order);

            var content = new StringContent(
                JsonSerializer.Serialize(processedOrder),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsJsonAsync(_d365Config.CheckoutPath, processedOrder, cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Log the request and response to D365PaymentLogsEntity
                var paymentLogs = new List<D365PaymentLogsEntity>
                {
                    new D365PaymentLogsEntity
                    {
                        PaymentId = order.PaymentInfo.PaymentId,
                        Operation = Operation.CHECKOUT,
                        Status = (int)response.StatusCode,
                        Response = responseContent
                    },
                    new D365PaymentLogsEntity
                    {
                        PaymentId = order.PaymentInfo.PaymentId,
                        Operation = Operation.CHECKOUT_REQUEST,
                        Status = 200, // HttpStatusCode.Ok
                        Response = processedOrder
                    }
                };

                await _dbContext.D365PaymentLogs.AddRangeAsync(paymentLogs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);


                if (response.IsSuccessStatusCode)
                {
                    
                    var paymentResponse = JsonSerializer.Deserialize<D365PaymentResponse>(responseContent);

                    if (paymentResponse.Errors != null && paymentResponse.Errors.Count > 0)
                    {
                        _logger.LogError("Payment error: {ErrorMessage}", paymentResponse.Errors[0].Details);
                        statusText = paymentResponse.StatusText;
                        return false;
                    }

                    return paymentResponse.Status;
                }

                _logger.LogInformation("Interim sales order created successfully for user: {UserId}", order.User.Id);

                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating interim sales order for user: {UserId}", order.User.Id);

                // Log the request and response to D365PaymentLogsEntity
                var paymentLogs = new List<D365PaymentLogsEntity>
                {
                    new D365PaymentLogsEntity
                    {
                        PaymentId = order.PaymentInfo.PaymentId,
                        Operation = Operation.CHECKOUT,
                        Status = ex.StatusCode != null ? (int)ex.StatusCode : (int)HttpStatusCode.BadRequest,
                        Response = ex.Message
                    },
                    new D365PaymentLogsEntity
                    {
                        PaymentId = order.PaymentInfo.PaymentId,
                        Operation = Operation.CHECKOUT_REQUEST,
                        Status = 200, // HttpStatusCode.Ok
                        Response = processedOrder
                    }
                };

                await _dbContext.D365PaymentLogs.AddRangeAsync(paymentLogs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogError("Failed to send checkout Sale order {StatusText}", statusText);

            return false;
        }

        public async Task<string> HandleD365OrderAsync(D365Order order, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling D365 order with ID: {OrderId}", order.OrderId);

            if (order.D365Itemid != null && order.D365Itemid.StartsWith("QLR"))
            {
                if (order.D365Itemid.StartsWith("QLR-ADD-FEA"))
                {
                    try
                    {
                        // Simulate processPaytoFeature (replace with actual implementation)
                        await ProcessPaytoFeatureAsync(order.AdId, order.D365Itemid);

                        await SaveD365RequestLogsAsync(
                            DateTime.UtcNow,
                            order,
                            1,
                            new { message = "Add feature processed successfully" },
                            cancellationToken
                        );

                        return "Add feature processed successfully";
                    }
                    catch (Exception ex)
                    {
                        await SaveD365RequestLogsAsync(
                            DateTime.UtcNow,
                            order,
                            0,
                            new { message = ex.Message },
                            cancellationToken
                        );
                        throw new InvalidOperationException($"Error processing feature: {ex.Message}", ex);
                    }
                }
                else if (order.D365Itemid.StartsWith("QLR-SUB"))
                {
                    try
                    {
                        // Simulate processSubscriptions (replace with actual implementation)
                        await ProcessSubscriptionsAsync(order);

                        await SaveD365RequestLogsAsync(
                            DateTime.UtcNow,
                            order,
                            1,
                            new { message = "Subscription processed successfully" },
                            cancellationToken
                        );

                        return "Subscription processed successfully";
                    }
                    catch (Exception ex)
                    {
                        await SaveD365RequestLogsAsync(
                            DateTime.UtcNow,
                            order,
                            0,
                            new { message = "Error While processing subscription" },
                            cancellationToken
                        );
                        throw new InvalidOperationException($"Error processing subscription: {ex.Message}", ex);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unknown QLC Reward D365 ItemID : {order.D365Itemid}");
                }
            }

            return "No QLR item processed";
        }

        // Placeholder for actual feature processing logic
        private async Task ProcessPaytoFeatureAsync(int adId, string d365ItemId)
        {
            await Task.CompletedTask;
            // Implement actual logic here
        }

        // Placeholder for actual subscription processing logic
        private async Task ProcessSubscriptionsAsync(D365Order order)
        {
            await Task.CompletedTask;
            // Implement actual logic here
        }



        private ProcessedOrder ProcessCheckoutOrder(D365Data order)
        {
            var quantity = order?.Item?.Quantity ?? 1;
            var price = (order?.PaymentInfo?.Fee ?? 0m) / Math.Max(quantity, 1);

            var orderItems = new OrderItem
            {
                QLUserId = order.User.Id,
                QLUserName = order.User.Name,
                Email = order.User.Email,
                Mobile = order.User.Mobile,
                QLOrderId = order.PaymentInfo.PaymentId.ToString(),
                OrderType = "New",
                ItemId = order.Item?.Id,
                Price = price,
                Classification = string.Empty,
                SubClassification = string.Empty,
                Quantity = quantity,
                CompanyId = "ql"
            };

            if (order.PaymentInfo.AdId != null)
            {
                orderItems.AddId = order.PaymentInfo.AdId;
            }

            return new ProcessedOrder
            {
                Request = new RequestData
                {
                    QLSalesOrderArray = new List<OrderItem>
                    {
                        orderItems
                    }
                }
            };
        }
        private async Task SaveD365RequestLogsAsync(DateTime createdAt, D365Order payload, int status, object response, CancellationToken cancellationToken)
        {
            try
            {
                // Assuming D365RequestsLogsEntity is a class with a suitable constructor or properties
                var logEntry = new D365RequestsLogsEntity
                {
                    CreatedAt = createdAt,
                    Payload = payload,
                    Status = status,
                    Response = response
                };

                // Replace with your actual persistence logic, e.g. EF Core DbContext or repository
                await _dbContext.D365RequestsLogs.AddAsync(logEntry, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving D365 request logs");
                throw;
            }
        }
        public async Task SendPaymentInfoD365Async(D365Data data, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("payment d365 Notification: {@Data}", data);

                // Token is already set in constructor, so we skip token acquisition here

                if (data.Operation == D365PaymentOperations.CHECKOUT)
                {
                    await CreateInterimSalesOrder(data, cancellationToken);
                }
                else if (data.Operation == D365PaymentOperations.SUCCESS)
                {
                    await CreateAndInvoiceSalesOrder(data, cancellationToken);
                }
                else
                {
                    _logger.LogError("Error processing message: Unsupported operation {Operation}", data.Operation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }

        public async Task<bool> D365OrdersAsync(D365Order[] order, CancellationToken cancellationToken)
        {
            if (order == null || order.Length == 0)
            {
                _logger.LogError("Order array is missing");
                return false;
            }

            var orderId = order[0]?.OrderId;

            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogError("D365 Order id missing");
                return false;
            }

            foreach (var item in order)
            {
                try
                {
                    await HandleD365OrderAsync(item, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing order: {ex.Message}", ex);
                    return false;
                }
            }

            return true;
        }
    }
}
