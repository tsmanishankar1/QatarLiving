using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public D365Service(
            ILogger<D365Service> logger,
            HttpClient httpClient,
            D365Config d365Config
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _d365Config = d365Config;

            // Acquire Bearer token using MSAL
            var authority = $"https://login.microsoftonline.com/{_d365Config.TenantId}";
            var app = ConfidentialClientApplicationBuilder.Create(_d365Config.ClientId)
                .WithClientSecret(_d365Config.ClientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            var scopes = new[] { $"{_d365Config.D365Url}/.default" };
            var tokenResult = app.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
        }

        public async Task<bool> CreateAndInvoiceSalesOrder(D365Data order)
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
                var response = await _httpClient.PostAsJsonAsync(_d365Config.InvoicePath, data);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Interim sales order created and invoiced successfully for user: {UserId}", order.User.Id);

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating and invoicing sales order for user: {UserId}", order.User.Id);
            };

            return false;
        }

        public async Task<bool> CreateInterimSalesOrder(D365Data order)
        {

            _logger.LogInformation("Creating interim sales order for user: {UserId}", order.User.Id);

            var processedOrder = ProcessCheckoutOrder(order);

            var content = new StringContent(
                JsonSerializer.Serialize(processedOrder),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // Use PostAsJsonAsync for cleaner serialization and posting
                var response = await _httpClient.PostAsJsonAsync(_d365Config.CheckoutPath, processedOrder);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Interim sales order created successfully for user: {UserId}", order.User.Id);

                return true;

            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating interim sales order for user: {UserId}", order.User.Id);
            };

            return false;

        }

        public async Task<string> HandleD365OrderAsync(D365Order order)
        {
            _logger.LogInformation("Handling D365 order with ID: {OrderId}", order.OrderId);


            throw new NotImplementedException();
        }



        private ProcessedOrder ProcessCheckoutOrder(D365Data order)
        {
            var quantity = order?.Item?.Quantity ?? 1;
            var price = (order?.PaymentInfo?.Fee ?? 0m) / Math.Max(quantity, 1);

            var orderItems = new OrderItem
            {
                QLUserId = order.User.Id.ToString(),
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

    }
}
