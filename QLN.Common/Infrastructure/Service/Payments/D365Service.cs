using Amazon.S3.Model;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Common.Infrastructure.Service.Payments
{
    internal class D365Service : ID365Service
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<D365Service> _logger;
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;
        private readonly QLPaymentsContext _dbContext;
        private readonly QLSubscriptionContext _subscriptionDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IV2SubscriptionService _subscriptionService;
        private readonly IClassifiedService _classifiedService;
        private readonly IDrupalUserService _drupalService;
        private readonly IConfidentialClientApplication _msalApp;
        private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public D365Service(
            DaprClient daprClient,
            ILogger<D365Service> logger,
            HttpClient httpClient,
            D365Config d365Config,
            QLPaymentsContext dbContext,
            QLSubscriptionContext qLSubscriptionContext,
            UserManager<ApplicationUser> userManager,
            IV2SubscriptionService subscriptionService,
            IClassifiedService classifiedService,
            IDrupalUserService drupalService
            )
        {
            _daprClient = daprClient;
            _logger = logger;
            _httpClient = httpClient;
            _d365Config = d365Config;
            _dbContext = dbContext;
            _subscriptionDbContext = qLSubscriptionContext;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _classifiedService = classifiedService;
            _drupalService = drupalService;

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
            _logger.LogInformation("Creating and invoicing sales order for payment: {PaymentId}", order.PaymentInfo.PaymentId);

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == order.PaymentInfo.PaymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogError("Payment not found for ID: {PaymentId}", order.PaymentInfo.PaymentId);
                return false;
            }

            var d365OrderId = GenerateD365OrderId(payment);

            // Create D365 items for each product with the same order ID
            var d365Items = payment.Products.Select((product, index) => new
            {
                qlOrderId = d365OrderId,
                itemId = $"{payment.PaymentId}-{index + 1}",
                productCode = product.ProductCode,
                productType = product.ProductType.ToString(),
                startDate_dd_mm_yyyy = order.ProductDuration?.StartDate.ToString("dd/MM/yyyy"),
                endDate_dd_mm_yyyy = order.ProductDuration?.EndDate.ToString("dd/MM/yyyy"),
                transDate_dd_mm_yyyy = order.PaymentInfo.Date.ToString("dd/MM/yyyy"),
                amount = product.Price,
                quantity = 1, // Default to 1 for now
                vertical = payment.Vertical.ToString(),
                subVertical = payment.SubVertical?.ToString(),
                adId = payment.AdId,
                paymentMethod = order.PaymentInfo.PaymentMethod,
                paymentGateway = $"PG-{order.PaymentInfo.Gateway}",
                source = order.PaymentInfo.Source,
                transactionId = order.PaymentInfo.TransactionId,
                company = "ql"
            }).ToList();

            var bulkData = new
            {
                orderId = d365OrderId,
                totalAmount = payment.Fee,
                items = d365Items
            };

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(bulkData),
                    Encoding.UTF8,
                    "application/json"
                );

                // Use PostAsJsonAsync for cleaner serialization and posting
                /*var response = await _httpClient.PostAsJsonAsync(_d365Config.InvoicePath, bulkData, cancellationToken);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Bulk sales order created and invoiced successfully for order: {OrderId}", d365OrderId);*/

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating and invoicing bulk sales order for order: {OrderId}", d365OrderId);
                await SendD365ErrorEmail(d365OrderId, ex.Message, cancellationToken);
                return false;
            }
        }

        public async Task<bool> CreateInterimSalesOrder(D365Data order, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating interim sales order for payment: {PaymentId}", order.PaymentInfo.PaymentId);

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == order.PaymentInfo.PaymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogError("Payment not found for ID: {PaymentId}", order.PaymentInfo.PaymentId);
                return false;
            }

            var statusText = "Unknown Failure";
            var processedOrder = ProcessMultiProductCheckoutOrder(payment, order);

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
                        Response = JsonSerializer.Serialize(processedOrder)
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

                var d365OrderId = GenerateD365OrderId(payment);
                _logger.LogInformation("Interim sales order created successfully for order: {OrderId}", d365OrderId);

                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error creating interim sales order for payment: {PaymentId}", order.PaymentInfo.PaymentId);

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
                        Response = JsonSerializer.Serialize(processedOrder)
                    }
                };

                await _dbContext.D365PaymentLogs.AddRangeAsync(paymentLogs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await SendD365ErrorEmail(null, ex.Message, cancellationToken);
            }

            _logger.LogError("Failed to send checkout Sale order {StatusText}", statusText);

            await SendD365ErrorEmail(order.PaymentInfo.PaymentId.ToString(), $"Failed to send checkout Sale order {statusText}", cancellationToken);

            return false;
        }

        public async Task<List<string>> D365OrdersAsync(D365Order[] orders, CancellationToken cancellationToken)
        {
            var results = new List<string>();

            if (orders == null || orders.Length == 0)
            {
                _logger.LogError("Order array is missing");
                results.Add("Order array is missing");
                return results;
            }


            // Group orders by OrderId to handle multiple items with same order ID
            var orderGroups = orders.GroupBy(o => o.OrderId).ToList();

            _logger.LogInformation("Processing {GroupCount} order groups with {TotalOrders} total items",
                orderGroups.Count, orders.Length);

            foreach (var orderGroup in orderGroups)
            {
                try
                {
                    var orderId = orderGroup.Key;
                    var orderItems = orderGroup.ToList();

                    _logger.LogInformation("Processing order group {OrderId} with {ItemCount} items",
                        orderId, orderItems.Count);

                    // Process each item in the order group
                    foreach (var item in orderItems)
                    {
                        var result = await HandleD365OrderItemAsync(item, cancellationToken);
                        results.Add(result);
                    }

                    await SaveD365RequestLogsAsync(
                        DateTime.UtcNow,
                        orderItems.First(),
                        1,
                        new { message = $"Order group {orderId} processed successfully with {orderItems.Count} items" },
                        cancellationToken // dont send cancellation token else no log gets written
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order group: {OrderId}", orderGroup.Key);

                    await SaveD365RequestLogsAsync(
                        DateTime.UtcNow,
                        orderGroup.First(),
                        0,
                        new { message = $"Error processing order group: {ex.Message}" }
                        //cancellationToken // dont send cancellation token else no log gets written
                    );

                    await SendD365ErrorEmail(orderGroup.Key, $"Error processing order group: {ex.Message}", cancellationToken);

                    results.Add($"Error processing order group {orderGroup.Key}: {ex.Message}");
                }
            }

            return results;
        }

        public async Task<string> HandleD365OrderAsync(D365Order order, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling D365 order with ID: {OrderId}", order.OrderId);

            if (order.D365Itemid == null)
            {
                await SendD365ErrorEmail(order.OrderId, $"D365 ItemID is null for order: '{order.OrderId}'", cancellationToken);
                return "D365 ItemID is null";
            }
            // still check this as it is useful to check this format
            var orderStrings = order.D365Itemid.Split('-');

            if (orderStrings.Length < 3)
            {
                return $"Invalid D365 ItemID format: {order.D365Itemid}";
            }

            // Find the corresponding product in subscriptionDbContext
            var product = _subscriptionDbContext.Products.FirstOrDefault(p => p.ProductCode == order.D365Itemid);

            if (product == null)
            {
                _logger.LogWarning("Product not found for product code: {ProductCode}", order.D365Itemid);

                await SendD365ErrorEmail(order.OrderId, $"Product not found for product code: order.D365Itemid", cancellationToken);
                // cannot buy something which doesnt exist
                return $"Product not found for product code {order.D365Itemid}";
            }

            switch (product.ProductType)
            {
                // Always order these based on which ones are more likely to happen often
                case ProductType.PUBLISH:
                    return await ProcessPayToPublish(order, product.Vertical, cancellationToken);
                case ProductType.SUBSCRIPTION:
                    return await ProcessSubscription(order, product.Vertical, cancellationToken);
                case ProductType.ADDON_FEATURE:
                    return await ProcessAddonFeature(order, product.Vertical, cancellationToken);
                case ProductType.ADDON_PROMOTE:
                    return await ProcessAddonPromote(order, product.Vertical, cancellationToken);
                case ProductType.ADDON_REFRESH:
                    return await ProcessAddonRefresh(order, product.Vertical, cancellationToken);
                default:
                    await SendD365ErrorEmail(order.OrderId, $"Unknown D365 ItemID : {order.D365Itemid}", cancellationToken);
                    return $"Unknown D365 ItemID : {order.D365Itemid}";
            }
        }

        private async Task<string> HandleD365OrderItemAsync(D365Order orderItem, CancellationToken cancellationToken)
        {
            if (orderItem.D365Itemid == null)
            {
                await SendD365ErrorEmail(orderItem.OrderId, $"D365 ItemID is null for order: '{orderItem.OrderId}'", cancellationToken);
                return "D365 ItemID is null";
            }

            if (orderItem.QLUserId == "0")
            {
                await SendD365ErrorEmail(orderItem.OrderId, $"D365 QLUserId is null for order: '{orderItem.OrderId}'", cancellationToken);
                return "D365 QLUserId is 0";
            }

            _logger.LogInformation("Handling D365 order item with ID: {ItemId} for order: {OrderId}",
                orderItem.D365Itemid, orderItem.OrderId);


            // Use the existing HandleD365OrderAsync logic
            return await HandleD365OrderAsync(orderItem, cancellationToken);
        }

        private string GenerateD365OrderId(PaymentEntity payment)
        {
            // Determine prefix based on first product code or vertical
            var prefix = "QLN"; // Default

            if (payment.Products.Any())
            {
                var firstProductCode = payment.Products.First().ProductCode;
                if (firstProductCode.StartsWith("QLC"))
                {
                    prefix = "QLC";
                }
                else if (firstProductCode.StartsWith("QLS"))
                {
                    prefix = "QLS";
                }
            }
            else
            {
                // Fallback to vertical
                prefix = payment.Vertical switch
                {
                    Vertical.Classifieds => "QLC",
                    Vertical.Services => "QLS",
                    _ => "QLN"
                };
            }

            return $"{prefix}-{payment.PaymentId}";
        }

        //private int ExtractPaymentIdFromOrderId(string orderId)
        //{
        //    // Extract payment ID from formats like "QLC-123" or "QLS-456"
        //    var parts = orderId.Split('-');
        //    if (parts.Length == 2 && int.TryParse(parts[1], out var paymentId))
        //    {
        //        return paymentId;
        //    }
        //    return 0;
        //}

        private ProcessedOrder ProcessMultiProductCheckoutOrder(PaymentEntity payment, D365Data order)
        {
            var d365OrderId = GenerateD365OrderId(payment);

            var orderItems = payment.Products.Select((product, index) => new OrderItem
            {
                QLUserId = payment.PaidByUid,
                QLUserName = order.User.Name,
                Email = order.User.Email,
                Mobile = order.User.Mobile,
                QLOrderId = d365OrderId, 
                OrderType = "New",
                ItemId = product.ProductCode,
                Price = product.Price,
                Classification = payment.Vertical.ToString(),
                SubClassification = payment.SubVertical?.ToString() ?? string.Empty,
                Quantity = 1, // Default to 1 for now
                CompanyId = "ql",
                AddId = payment.AdId
            }).ToList();

            return new ProcessedOrder
            {
                Request = new RequestData
                {
                    QLSalesOrderArray = orderItems
                }
            };
        }

        // Keep the legacy method for backward compatibility
        //private ProcessedOrder ProcessCheckoutOrder(D365Data order)
        //{
        //    var quantity = order?.Item?.Quantity ?? 1;
        //    var price = (order?.PaymentInfo?.Fee ?? 0m) / Math.Max(quantity, 1);

        //    var orderItems = new OrderItem
        //    {
        //        QLUserId = order.User.Id,
        //        QLUserName = order.User.Name,
        //        Email = order.User.Email,
        //        Mobile = order.User.Mobile,
        //        QLOrderId = order.PaymentInfo.PaymentId.ToString(),
        //        OrderType = "New",
        //        ItemId = order.Item?.Id,
        //        Price = price,
        //        Classification = string.Empty,
        //        SubClassification = string.Empty,
        //        Quantity = quantity,
        //        CompanyId = "ql"
        //    };

        //    if (order.PaymentInfo.AdId != null)
        //    {
        //        orderItems.AddId = order.PaymentInfo.AdId;
        //    }

        //    return new ProcessedOrder
        //    {
        //        Request = new RequestData
        //        {
        //            QLSalesOrderArray = new List<OrderItem>
        //            {
        //                orderItems
        //            }
        //        }
        //    };
        //}

        private async Task<string> ProcessSubscription(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            try
            {
                // Simulate ProcessSubscriptions (replace with actual implementation)
                var result = await ProcessSubscriptionsAsync(order, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Subscription processed successfully" },
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    0,
                    new { message = "Error while processing subscription" }
                //cancellationToken // dont send cancellation token else no log gets written
                );

                await SendD365ErrorEmail(order.OrderId, $"Error while processing subscription: '{order.OrderId}'", cancellationToken);
                return $"Error processing subscription: {ex.Message}";
            }
        }

        private async Task<string> ProcessAddonFeature(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (decimal.TryParse(order.Price, out var price) && price <= 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"Price must be greater than zero for addon feature processing for order '{order.OrderId}'", cancellationToken);
                return "Price must be greater than zero for addon feature processing.";
            }

            if(order.AdId == 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"AdId must be provided for addon feature processing for order '{order.OrderId}'", cancellationToken);
                return "AdId must be provided for addon feature processing.";
            }

            try
            {
                // Simulate ProcessPaytoFeature (replace with actual implementation)
                var result = await ProcessPaytoFeatureAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add feature processed successfully" },
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    0,
                    new { message = ex.Message }
                    //cancellationToken // dont send cancellation token else no log gets written
                );
                await SendD365ErrorEmail(order.OrderId, $"Error processing feature: {ex.Message}", cancellationToken);
                return $"Error processing feature: {ex.Message}";
            }
        }

        private async Task<string> ProcessAddonPromote(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (decimal.TryParse(order.Price, out var price) && price <= 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"Price must be greater than zero for addon promote processing for order '{order.OrderId}'", cancellationToken);
                return "Price must be greater than zero for addon promote processing.";
            }

            if (order.AdId == 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"AdId must be provided for addon promote processing for order '{order.OrderId}'", cancellationToken);
                return "AdId must be provided for addon promote processing.";
            }

            try
            {
                // Simulate ProcessPaytoPromote (replace with actual implementation)
                var result = await ProcessPaytoPromoteAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add promote processed successfully" },
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    0,
                    new { message = ex.Message }
                    //cancellationToken // dont send cancellation token else no log gets written
                );
                await SendD365ErrorEmail(order.OrderId, $"Error processing promote: {ex.Message}", cancellationToken);
                return $"Error processing promote: {ex.Message}";
            }
        }

        private async Task<string> ProcessAddonRefresh(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (decimal.TryParse(order.Price, out var price) && price <= 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"Price must be greater than zero for addon refresh processing for order '{order.OrderId}'", cancellationToken);
                return "Price must be greater than zero for addon refresh processing.";
            }

            if (order.AdId == 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"AdId must be provided for addon refresh processing for order '{order.OrderId}'", cancellationToken);
                return "AdId must be provided for addon refresh processing.";
            }

            try
            {
                // Simulate ProcessPaytoRefresh (replace with actual implementation)
                var result = await ProcessAddonRefreshAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add refresh processed successfully" },
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    0,
                    new { message = ex.Message }
                    //cancellationToken // dont send cancellation token else no log gets written
                );
                await SendD365ErrorEmail(order.OrderId, $"Error processing refresh: {ex.Message}", cancellationToken);
                return $"Error processing refresh: {ex.Message}";
            }
        }

        private async Task<string> ProcessPayToPublish(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            try
            {
                
                var result = await ProcessPayToPublishAsync(order, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Pay to publish processed successfully" },
                    cancellationToken
                );

                return result;
            }
            catch (Exception ex)
            {
                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    0,
                    new { message = ex.Message }
                    //cancellationToken // dont send cancellation token else no log gets written
                );
                await SendD365ErrorEmail(order.OrderId, $"Error processing pay to publish: {ex.Message}", cancellationToken);
                return $"Error processing pay to publish: {ex.Message}";
            }
        }

        // Updated to use new PaymentEntity structure
        private async Task<string> ProcessPaytoFeatureAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} not found.", cancellationToken);
                return $"Advert with ID {adId} not found.";
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} does not have a valid SubscriptionId.", cancellationToken);
                return $"Advert with ID {adId} does not have a valid SubscriptionId.";
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                await SendD365ErrorEmail(null, $"Pay to Feature Error: User with ID {advert.UserId} not found for AdId {adId}.", cancellationToken);
                return $"User with ID {advert.UserId} not found for AdId {adId}.";
            }

            try
            {
                var paymentEntity = new PaymentEntity
                {
                    Gateway = Gateway.D365,
                    Date = DateTime.UtcNow,
                    Fee = price,
                    PaidByUid = user.Id.ToString(),
                    PaymentMethod = PaymentMethod.Cash,
                    Source = Source.D365,
                    Status = PaymentStatus.Success,
                    Vertical = vertical,
                    AdId = adId,
                    TriggeredSource = TriggeredSource.D365,
                    Products = new List<ProductDetails>
                {
                    new ProductDetails
                    {
                        ProductType = ProductType.ADDON_FEATURE,
                        ProductCode = d365ItemId,
                        Price = price
                    }
                }
                };

                // TODO: This maybe needs to be wrapped in a try catch
                var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // create the pay to feature and reference the payment Id
                var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
                {
                    UserId = user.LegacyUid.ToString(),
                    ProductCode = d365ItemId,
                    PaymentId = paymentEntity.PaymentId,
                    SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
                }, cancellationToken);

                // Update payment entity with addon ID
                paymentEntity.UserAddonIds = new List<Guid> { addonId };
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error processing pay to feature: {ex.Message}";
            }

            return "Ad Feature processed successfully";
        }

        private async Task<string> ProcessPaytoPromoteAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} not found.", cancellationToken);
                return $"Advert with ID {adId} not found.";
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} does not have a valid SubscriptionId.", cancellationToken);
                return $"Advert with ID {adId} does not have a valid SubscriptionId.";
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                await SendD365ErrorEmail(null, $"Pay to Promote Error: User with ID {advert.UserId} not found for AdId {adId}.", cancellationToken);
                return $"User with ID {advert.UserId} not found for AdId {adId}.";
            }

            try
            {
                var paymentEntity = new PaymentEntity
                {
                    Gateway = Gateway.D365,
                    Date = DateTime.UtcNow,
                    Fee = price,
                    PaidByUid = user.Id.ToString(),
                    PaymentMethod = PaymentMethod.Cash,
                    Source = Source.D365,
                    Status = PaymentStatus.Success,
                    Vertical = vertical,
                    AdId = adId,
                    TriggeredSource = TriggeredSource.D365,
                    Products = new List<ProductDetails>
                {
                    new ProductDetails
                    {
                        ProductType = ProductType.ADDON_PROMOTE,
                        ProductCode = d365ItemId,
                        Price = price
                    }
                }
                };

                // TODO: This maybe needs to be wrapped in a try catch
                var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // create the pay to promote and reference the payment Id
                var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
                {
                    UserId = user.LegacyUid.ToString(),
                    ProductCode = d365ItemId,
                    PaymentId = paymentEntity.PaymentId,
                    SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
                }, cancellationToken);

                // Update payment entity with addon ID
                paymentEntity.UserAddonIds = new List<Guid> { addonId };
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                 return $"Error processing pay to promote: {ex.Message}";
            }

            return "Ad Promote processed successfully";
        }

        private async Task<string> ProcessAddonRefreshAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} not found.", cancellationToken);
                return $"Advert with ID {adId} not found.";
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                await SendD365ErrorEmail(null, $"Advert with ID {adId} does not have a valid SubscriptionId.", cancellationToken);
                return $"Advert with ID {adId} does not have a valid SubscriptionId.";
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                await SendD365ErrorEmail(null, $"Refresh Error: User with ID {advert.UserId} not found for AdId {adId}.", cancellationToken);
                return $"User with ID {advert.UserId} not found for AdId {adId}.";
            }

            try
            {
                var paymentEntity = new PaymentEntity
                {
                    Gateway = Gateway.D365,
                    Date = DateTime.UtcNow,
                    Fee = price,
                    PaidByUid = user.Id.ToString(),
                    PaymentMethod = PaymentMethod.Cash,
                    Source = Source.D365,
                    Status = PaymentStatus.Success,
                    Vertical = vertical,
                    AdId = adId,
                    TriggeredSource = TriggeredSource.D365,
                    Products = new List<ProductDetails>
                {
                    new ProductDetails
                    {
                        ProductType = ProductType.ADDON_REFRESH,
                        ProductCode = d365ItemId,
                        Price = price
                    }
                }
                };

                // TODO: This maybe needs to be wrapped in a try catch
                var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // create the pay to refresh and reference the payment Id
                var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
                {
                    UserId = user.LegacyUid.ToString(),
                    ProductCode = d365ItemId,
                    PaymentId = paymentEntity.PaymentId,
                    SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
                }, cancellationToken);

                // Update payment entity with addon ID
                paymentEntity.UserAddonIds = new List<Guid> { addonId };
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error processing refresh: {ex.Message}";
            }

            return "Ad Refresh processed successfully";
        }

        private async Task<string> ProcessSubscriptionsAsync(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (decimal.TryParse(order.Price, out var price) && price <= 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"Price must be greater than zero for pay to publish processing for order '{order.OrderId}'", cancellationToken);
                return "Price must be greater than zero for pay to publish processing.";
            }

            if(!int.TryParse(order.QLUserId, out var qlUserId))
            {
                await SendD365ErrorEmail(order.OrderId, $"Invalid QLUserId '{order.QLUserId}' for order '{order.OrderId}'", cancellationToken);
                return $"Invalid QLUserId '{order.QLUserId}' for order '{order.OrderId}'";
            }

            if (string.IsNullOrEmpty(order.Email))
            {
                await SendD365ErrorEmail(null, $"Email is required to find or create user for QLUserId '{order.QLUserId}' with QLUsername '{order.QLUsername}'", cancellationToken);
                return $"Email is required to find or create user for QLUserId '{order.QLUserId}' with QLUsername '{order.QLUsername}'";
            }

            try
            {

                var user = await FindOrCreateUser(qlUserId, order.QLUsername, order.Email, order.Mobile, cancellationToken);

                if (user == null)
                {
                    await SendD365ErrorEmail(null, $"Subscription Error: User with ID {order.QLUserId} not found for OrderId {order.OrderId}.", cancellationToken);
                    return $"User with ID {order.QLUserId} not found for OrderId {order.OrderId}.";
                }

                var paymentEntity = new PaymentEntity
                {
                    Gateway = Gateway.D365,
                    Date = DateTime.UtcNow,
                    Fee = price,
                    PaidByUid = user.Id.ToString(),
                    PaymentMethod = PaymentMethod.Cash,
                    Source = Source.D365,
                    Status = PaymentStatus.Success,
                    Vertical = vertical,
                    TriggeredSource = TriggeredSource.D365,
                    Products = new List<ProductDetails>
                {
                    new ProductDetails
                    {
                        ProductType = ProductType.SUBSCRIPTION,
                        ProductCode = order.D365Itemid,
                        Price = price
                    }
                }
                };

                // TODO: This maybe needs to be wrapped in a try catch
                var payment = _dbContext.Payments.Add(paymentEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // create the subscription and reference the payment Id
                var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(new V2SubscriptionPurchaseRequestDto
                {
                    UserId = user.LegacyUid.ToString(),
                    ProductCode = order.D365Itemid,
                    PaymentId = paymentEntity.PaymentId,
                }, cancellationToken);

                // Update payment entity with subscription ID
                paymentEntity.UserSubscriptionId = subscriptionId;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error processing subscription: {ex.Message}";
            }

            return "Subscription processed successfully";
        }

        private async Task<string> ProcessPayToPublishAsync(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {

            if(decimal.TryParse(order.Price, out var price) && price <= 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"Price must be greater than zero for pay to publish processing for order '{order.OrderId}'", cancellationToken);
                return "Price must be greater than zero for pay to publish processing.";
            }

            if (order.AdId == 0)
            {
                await SendD365ErrorEmail(order.OrderId, $"AdId must be provided for pay to publish processing for order '{order.OrderId}'", cancellationToken);
                return "AdId must be provided for pay to publish processing.";
            }

            if (!int.TryParse(order.QLUserId, out var qlUserId))
            {
                await SendD365ErrorEmail(order.OrderId, $"Invalid QLUserId '{order.QLUserId}' for order '{order.OrderId}'", cancellationToken);
                return $"Invalid QLUserId '{order.QLUserId}' for order '{order.OrderId}'";
            }

            if (string.IsNullOrEmpty(order.Email))
            {
                await SendD365ErrorEmail(null, $"Email is required to find or create user for QLUserId '{order.QLUserId}' with QLUsername '{order.QLUsername}'", cancellationToken);
                return $"Email is required to find or create user for QLUserId '{order.QLUserId}' with QLUsername '{order.QLUsername}'";
            }

            var advert = await _classifiedService.GetItemAdById(order.AdId, cancellationToken);

            if (advert == null)
            {
                await SendD365ErrorEmail(order.OrderId, $"Advert with ID {order.AdId} not found.", cancellationToken);
                return $"Advert with ID {order.AdId} not found.";
            }

            try
            {
                var user = await FindOrCreateUser(qlUserId, order.QLUsername, order.Email, order.Mobile, cancellationToken);

                if (user == null)
                {
                    await SendD365ErrorEmail(null, $"Pay to Publish Error: User with ID {order.QLUserId} not found for AdId {order.AdId}.", cancellationToken);
                    return $"User with ID {order.QLUserId} not found for AdId {order.AdId}.";
                }

                var paymentEntity = new PaymentEntity
                {
                    Gateway = Gateway.D365,
                    Date = DateTime.UtcNow,
                    Fee = price,
                    PaidByUid = user.Id.ToString(),
                    PaymentMethod = PaymentMethod.Cash,
                    Source = Source.D365,
                    Status = PaymentStatus.Success,
                    Vertical = vertical,
                    AdId = order.AdId,
                    TriggeredSource = TriggeredSource.D365,
                    Products = new List<ProductDetails>
                {
                    new ProductDetails
                    {
                        ProductType = ProductType.PUBLISH,
                        ProductCode = order.D365Itemid,
                        Price = price
                    }
                }
                };

                // TODO: This maybe needs to be wrapped in a try catch
                var payment = _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // create the pay to publish and reference the payment Id
                var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(new V2SubscriptionPurchaseRequestDto
                {
                    UserId = user.LegacyUid.ToString(),
                    ProductCode = order.D365Itemid,
                    PaymentId = paymentEntity.PaymentId,
                    AdId = order.AdId,
                }, cancellationToken);

                // Update payment entity with subscription ID
                paymentEntity.UserSubscriptionId = subscriptionId;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error processing pay to publish: {ex.Message}";
            }

            return "Pay to Publish processed successfully";
        }

        private async Task<ApplicationUser?> FindOrCreateUser(long userId, string userName, string email, string mobile, CancellationToken cancellationToken)
        {
            
            // look for the user using their legacy user ID
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUid == userId, cancellationToken);

            if (user == null)
            {
                // try and see if we can find the user by email
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user == null)
            {
                // try and see if we can find the user by userName
                user = await _userManager.FindByNameAsync(userName);
            }

            // if we definitely do not have this user in our DB, so create him
            if (user == null)
            {
                // now go and see if this user is on Drupal

                var drupalUser = await _drupalService.GetUserInfoFromDrupalAsync(email, cancellationToken);

                if (drupalUser == null) return null;

                if(int.TryParse(drupalUser.User.Status, out var userStatus) && userStatus == 1)
                {
                    // Create new user
                    var randomPassword = GenerateRandomPassword();

                    user = new ApplicationUser
                    {
                        UserName = userName ?? drupalUser.User.Username,
                        Email = email,
                        PhoneNumber = mobile ?? drupalUser.User.Phone,
                        FirstName = userName ?? drupalUser.User.Username,
                        LastName = null,
                        LegacyUid = userId != 0 ? userId : (long.TryParse(drupalUser.User.Uid, out var drupalUserId) ? drupalUserId : 0),
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        TwoFactorEnabled = false,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        LanguagePreferences = "en", // Default language,
                    };

                    var createResult = await _userManager.CreateAsync(user, randomPassword);

                    if (!createResult.Succeeded)
                    {
                        var errors = createResult.Errors
                            .GroupBy(e => e.Code)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                        await SendD365ErrorEmail(null, $"Error creating DB user '{userName}': {string.Join(", ", errors.SelectMany(e => e.Value))}", cancellationToken);
                        throw new RegistrationValidationException(errors);
                    }

                }
            }

            // if we still do not have a user, then something went wrong
            return user;
        }

        private async Task SaveD365RequestLogsAsync(DateTime createdAt, D365Order payload, int status, object response, CancellationToken cancellationToken = default)
        {

            try
            {
                // Assuming D365RequestsLogsEntity is a class with a suitable constructor or properties
                var logEntry = new D365RequestsLogsEntity
                {
                    CreatedAt = createdAt,
                    Payload = JsonSerializer.Serialize(payload),
                    Status = status,
                    Response = JsonSerializer.Serialize(response)
                };

                // Replace with your actual persistence logic, e.g. EF Core DbContext or repository
                await _dbContext.D365RequestsLogs.AddAsync(logEntry, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving D365 request logs");
                await SendD365ErrorEmail(null, $"Error saving D365 request logs: {ex.Message}", cancellationToken);
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
                    await SendD365ErrorEmail(null, $"Error processing D365 payment notification: Unsupported operation {data.Operation}", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await SendD365ErrorEmail(null, $"Error processing D365 payment notification: {ex.Message}", cancellationToken);
            }
        }



        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private async Task SendD365ErrorEmail(string? orderId, string? errorMessage, CancellationToken cancellationToken)
        {
            var recipients = new List<RecipientDto>();

            foreach(var recipient in _d365Config.D365ErrorEmails)
            {
                recipients.Add(new RecipientDto
                {
                    Name = "D365 Errors",
                    Email = recipient
                });
            }

            var subject = string.IsNullOrEmpty(orderId) ? "D365Service Error (no orderId)" : $"D365 Error: Failure to process D365 Order '{orderId}'";
            var mainError = string.IsNullOrEmpty(errorMessage) ? "No error message provided." : errorMessage;


            var emailDataSend = new NotificationEntity
            {
                Destinations = new List<string> { "email" },
                Recipients = recipients,
                Subject = subject,
                Plaintext = $"{subject}.\n\nError: {errorMessage}\n\nThanks,\nQL Team",
                Html = $@"
                                <p>{subject}.</p>
                                <p>Error: <b>{errorMessage}</b></p>
                                <p>Thanks,<br/>QL Team</p>",
            };

            await _daprClient.PublishEventAsync("pubsub", "notifications-email", emailDataSend, cancellationToken);
        }
    }
}