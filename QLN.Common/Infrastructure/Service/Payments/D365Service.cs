using Google.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService;
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
        private readonly ILogger<D365Service> _logger;
        private readonly HttpClient _httpClient;
        private readonly D365Config _d365Config;
        private readonly QLPaymentsContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IV2SubscriptionService _subscriptionService;
        private readonly IClassifiedService _classifiedService;
        private readonly IConfidentialClientApplication _msalApp;
        private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;


        public D365Service(
            ILogger<D365Service> logger,
            HttpClient httpClient,
            D365Config d365Config,
            QLPaymentsContext dbContext,
            UserManager<ApplicationUser> userManager,
            IV2SubscriptionService subscriptionService,
            IClassifiedService classifiedService
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _d365Config = d365Config;
            _dbContext = dbContext;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _classifiedService = classifiedService;

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

            if (order.D365Itemid == null)
            {
                throw new InvalidOperationException("D365 ItemID is null");
            }

            if(order.QLUserId == 0)
            {
                throw new InvalidOperationException("D365 QLUserId is 0");
            }

            var orderStrings = order.D365Itemid.Split('-');

            if (orderStrings.Length < 3)
            {
                throw new InvalidOperationException($"Invalid D365 ItemID format: {order.D365Itemid}");
            }

            var verticalCheck = orderStrings[0];
            var productCheck = orderStrings[1];
            var subProductCheck = orderStrings[2];

            switch (verticalCheck)
            {
                // check if this is for Classifieds
                case "QLC":
                    switch (productCheck)
                    {
                        // Always order these based on which ones are more likely to happen often
                        case "P2P":
                            switch (subProductCheck)
                            {
                                case "ADD":
                                    return await ProcessAddonFeature(order, Vertical.Classifieds, cancellationToken);
                                case "REF":
                                    return await ProcessAddonRefresh(order, Vertical.Classifieds, cancellationToken);
                                default:
                                    return await ProcessPayToPublish(order, Vertical.Classifieds, cancellationToken);
                            }
                        case "SUB":
                            return await ProcessSubscription(order, Vertical.Classifieds, cancellationToken);
                        case "ADD":
                            return await ProcessAddonFeature(order, Vertical.Classifieds, cancellationToken);
                        default:
                            throw new InvalidOperationException($"Unknown QLS D365 ItemID : {order.D365Itemid}");
                    }
                // check if this is for Services
                case "QLS":
                    switch (productCheck)
                    {
                        // Always order these based on which ones are more likely to happen often
                        case "P2P":
                            switch (subProductCheck)
                            {
                                case "ADD":
                                    return await ProcessAddonFeature(order, Vertical.Services, cancellationToken);
                                case "PRO":
                                    return await ProcessAddonPromote(order, Vertical.Services, cancellationToken);
                                default:
                                    return await ProcessPayToPublish(order, Vertical.Services, cancellationToken);
                            }
                        case "SUB":
                            return await ProcessSubscription(order, Vertical.Services, cancellationToken);
                        default:
                            switch (subProductCheck)
                            {
                                default:
                                    throw new InvalidOperationException($"Unknown QLS D365 ItemID : {order.D365Itemid}");
                            }
                    }
                default:
                    break;
            }

            return "No order item matched";
        }

        private async Task<string> ProcessSubscription(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            try
            {
                // Simulate ProcessSubscriptions (replace with actual implementation)
                await ProcessSubscriptionsAsync(order, vertical, cancellationToken);

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

        private async Task<string> ProcessAddonFeature(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {

            if(order.Price == null || order.Price <= 0)
            {
                throw new InvalidOperationException("Price must be greater than zero for addon feature processing.");
            }

            decimal price = order.Price.HasValue ? order.Price.Value : 0;

            try
                {
                    // Simulate ProcessPaytoFeature (replace with actual implementation)
                    await ProcessPaytoFeatureAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                    await SaveD365RequestLogsAsync(
                        DateTime.UtcNow,
                        order,
                        1,
                        new { message = "Add feature processed successfully" },
                        cancellationToken
                    );

                    return "Ad Feature processed successfully";
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

        private async Task<string> ProcessAddonPromote(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (order.Price == null || order.Price <= 0)
            {
                throw new InvalidOperationException("Price must be greater than zero for addon promote processing.");
            }

            decimal price = order.Price.HasValue ? order.Price.Value : 0;

            try
            {
                // Simulate ProcessPaytoPromote (replace with actual implementation)
                await ProcessPaytoPromoteAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add feature processed successfully" },
                    cancellationToken
                );

                return "Ad Promote processed successfully";
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

        private async Task<string> ProcessAddonRefresh(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            if (order.Price == null || order.Price <= 0)
            {
                throw new InvalidOperationException("Price must be greater than zero for addon refresh processing.");
            }

            decimal price = order.Price.HasValue ? order.Price.Value : 0;

            try
            {
                // Simulate ProcessPaytoPromote (replace with actual implementation)
                await ProcessAddonRefreshAsync(order.AdId, order.D365Itemid, price, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add feature processed successfully" },
                    cancellationToken
                );

                return "Ad Promote processed successfully";
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

        private async Task<string> ProcessPayToPublish(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            try
            {
                // Simulate ProcessPayToPublish (replace with actual implementation)
                await ProcessPayToPublishAsync(order, vertical, cancellationToken);

                await SaveD365RequestLogsAsync(
                    DateTime.UtcNow,
                    order,
                    1,
                    new { message = "Add feature processed successfully" },
                    cancellationToken
                );

                return "Pay to Publish processed successfully";
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

        // Placeholder for actual feature processing logic
        private async Task ProcessPaytoFeatureAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            // this flow assumes a user definitely exists and has created an ad,
            // but that the ad must be associated to the pay to feature process
            // 1) Get the advert from the Classifieds service
            // 2) find the username of the ad
            // 3) Lookup the user from userManager
            // 4) Create an order in the payments table
            // 5) Create a pay to feature
            // 6) Update the user object with the purchase of a pay to feature
            // 7) Save all changes
            // Implement actual logic here
            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                throw new InvalidOperationException($"Advert with ID {adId} not found.");
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                throw new InvalidOperationException($"Advert with ID {adId} does not have a valid SubscriptionId.");
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {advert.UserId} not found.");
            }

            var paymentEntity = new PaymentEntity
            {
                Gateway = Gateway.D365,
                Date = DateTime.UtcNow,
                Fee = price,
                PaidByUid = user.Id.ToString(), // using the userId of our system not the legacy one
                PaymentMethod = PaymentMethod.Cash, // need to check this ?
                ProductType = ProductType.ADDON_FEATURE,
                Source = Source.D365,
                Status = PaymentStatus.Success,
                Vertical = vertical,
                TriggeredSource = TriggeredSource.D365
            };

            var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // create the pay to promote and reference the payment Id
            var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
            {
                UserId = user.Id.ToString(),
                ProductCode = d365ItemId,
                PaymentId = paymentEntity.PaymentId,
                SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
            });

            // User Entry should somehow be updated with this purchase, likely through a pub/sub on the subscription actor

            return;
        }

        // Placeholder for actual promote processing logic
        private async Task ProcessPaytoPromoteAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            // this flow assumes a user definitely exists and has created an ad,
            // but that the ad must be associated to the pay to feature process
            // 1) Get the advert from the Classifieds service
            // 2) find the username of the ad
            // 3) Lookup the user from userManager
            // 4) Create an order in the payments table
            // 5) Create a pay to promote
            // 6) Update the user object with the purchase of a pay to promote
            // 7) Save all changes
            // Implement actual logic here

            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                throw new InvalidOperationException($"Advert with ID {adId} not found.");
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                throw new InvalidOperationException($"Advert with ID {adId} does not have a valid SubscriptionId.");
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {advert.UserId} not found.");
            }

            var paymentEntity = new PaymentEntity
            {
                Gateway = Gateway.D365,
                Date = DateTime.UtcNow,
                Fee = price,
                PaidByUid = user.Id.ToString(), // using the userId of our system not the legacy one
                PaymentMethod = PaymentMethod.Cash, // need to check this ?
                ProductType = ProductType.ADDON_PROMOTE,
                Source = Source.D365,
                Status = PaymentStatus.Success,
                Vertical = vertical,
                TriggeredSource = TriggeredSource.D365
            };

            var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // create the pay to promote and reference the payment Id
            var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
            {
                UserId = user.Id.ToString(),
                ProductCode = d365ItemId,
                PaymentId = paymentEntity.PaymentId,
                SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
            });

            // User Entry should somehow be updated with this purchase, likely through a pub/sub on the subscription actor

            return;
        }

        // Placeholder for actual promote processing logic
        private async Task ProcessAddonRefreshAsync(int adId, string d365ItemId, decimal price, Vertical vertical, CancellationToken cancellationToken)
        {
            // this flow assumes a user definitely exists and has created an ad,
            // but that the ad must be associated to the pay to feature process
            // 1) Get the advert from the Classifieds service
            // 2) find the username of the ad
            // 3) Lookup the user from userManager
            // 4) Create an order in the payments table
            // 5) Create a pay to promote
            // 6) Update the user object with the purchase of a pay to promote
            // 7) Save all changes
            // Implement actual logic here

            var advert = await _classifiedService.GetItemAdById(adId, cancellationToken);

            if (advert == null)
            {
                throw new InvalidOperationException($"Advert with ID {adId} not found.");
            }

            if (advert.SubscriptionId == null || advert.SubscriptionId == Guid.Empty)
            {
                throw new InvalidOperationException($"Advert with ID {adId} does not have a valid SubscriptionId.");
            }

            var user = await _userManager.FindByIdAsync(advert.UserId);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {advert.UserId} not found.");
            }

            var paymentEntity = new PaymentEntity
            {
                Gateway = Gateway.D365,
                Date = DateTime.UtcNow,
                Fee = price,
                PaidByUid = user.Id.ToString(), // using the userId of our system not the legacy one
                PaymentMethod = PaymentMethod.Cash, // need to check this ?
                ProductType = ProductType.ADDON_REFRESH,
                Source = Source.D365,
                Status = PaymentStatus.Success,
                Vertical = vertical,
                TriggeredSource = TriggeredSource.D365
            };

            var payment = await _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // create the pay to promote and reference the payment Id
            var addonId = await _subscriptionService.PurchaseAddonAsync(new V2UserAddonPurchaseRequestDto
            {
                UserId = user.Id.ToString(),
                ProductCode = d365ItemId,
                PaymentId = paymentEntity.PaymentId,
                SubscriptionId = advert.SubscriptionId != null ? advert.SubscriptionId.Value : Guid.Empty
            });

            // User Entry should somehow be updated with this purchase, likely through a pub/sub on the subscription actor

            return;
        }

        // Placeholder for actual subscription processing logic
        private async Task ProcessSubscriptionsAsync(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            // this process assumes that a user may exist, but if they do not then proceed
            // to create a user based on the provided information being an email address
            // and mobile number
            // 1) try FindOrCreateUser method
            // 2) Create an order in the payments table - seems the existing system doesnt do this
            // 3) Create a subscription
            // 4) Update the user with the subscription ID
            // 5) Save all changes
            // Implement actual logic here
            var user = await FindOrCreateUser(order.QLUserId, order.QLUsername, order.Email, order.Mobile, cancellationToken);

            var paymentEntity = new PaymentEntity
            {
                Gateway = Gateway.D365,
                Date = DateTime.UtcNow,
                Fee = order.Price ?? 0,
                PaidByUid = user.Id.ToString(), // using the userId of our system not the legacy one
                PaymentMethod = PaymentMethod.Cash, // need to check this ?
                ProductType = ProductType.SUBSCRIPTION,
                Source = Source.D365,
                Status = PaymentStatus.Success,
                Vertical = vertical,
                TriggeredSource = TriggeredSource.D365
            };

            var payment = _dbContext.Payments.Add(paymentEntity);
            _dbContext.SaveChanges();

            // create the subscription and reference the payment Id

            var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(new V2SubscriptionPurchaseRequestDto
            {
                UserId = user.Id.ToString(),
                ProductCode = order.D365Itemid,
                PaymentId = paymentEntity.PaymentId,
            });

            // Not sure if we should do this here or rather as a pub/sub from the subscription actor itself (preferred)
            //var subscription = new UserSubscription {
            //    DisplayName = order.D365Itemid,
            //    Id = subscriptionId,
            //    UserId = user.Id,
            //    ProductCode = order.D365Itemid,
            //    StartDate = order.StartDate ?? DateTime.UtcNow,
            //    EndDate = order.EndDate ?? DateTime.UtcNow.AddYears(1), // Assuming a default duration of 1 year
            //    ProductName = order.SalesType ?? "Default Subscription",
            //    Vertical = vertical,
            //};

            //if (user.Subscriptions != null)
            //{
            //    user.Subscriptions.Add(subscription);
            //} else
            //{
            //    user.Subscriptions = new List<UserSubscription>
            //    {
            //        subscription
            //    };
            //}

            //await _userManager.UpdateAsync(user);

            return;
        }

        // Placeholder for actual pay to publish processing logic
        private async Task ProcessPayToPublishAsync(D365Order order, Vertical vertical, CancellationToken cancellationToken)
        {
            // this process assumes that a user may exist, but if they do not then proceed
            // to create a user based on the provided information being an email address
            // and mobile number
            // 1) try FindOrCreateUser method
            // 2) Create an order in the payments table
            // 3) Create a subscription
            // 4) Update the user with the subscription ID
            // 5) Save all changes
            // Implement actual logic here
            var user = await FindOrCreateUser(order.QLUserId, order.QLUsername, order.Email, order.Mobile, cancellationToken);

            var paymentEntity = new PaymentEntity
            {
                Gateway = Gateway.D365,
                Date = DateTime.UtcNow,
                Fee = order.Price ?? 0,
                PaidByUid = user.Id.ToString(), // using the userId of our system not the legacy one
                PaymentMethod = PaymentMethod.Cash, // need to check this ?
                ProductType = ProductType.PUBLISH,
                Source = Source.D365,
                Status = PaymentStatus.Success,
                Vertical = vertical,
                TriggeredSource = TriggeredSource.D365
            };

            var payment = _dbContext.Payments.AddAsync(paymentEntity, cancellationToken);
            _dbContext.SaveChanges();

            // create the pay to publish and reference the payment Id

            var subscriptionId = await _subscriptionService.PurchaseSubscriptionAsync(new V2SubscriptionPurchaseRequestDto
            {
                UserId = user.Id.ToString(),
                ProductCode = order.D365Itemid,
                PaymentId = paymentEntity.PaymentId,
            });

            // Not sure if we should do this here or rather as a pub/sub from the subscription actor itself (preferred)
            //var subscription = new UserSubscription
            //{
            //    DisplayName = order.D365Itemid,
            //    Id = subscriptionId,
            //    UserId = user.Id,
            //    ProductCode = order.D365Itemid,
            //    StartDate = order.StartDate ?? DateTime.UtcNow,
            //    EndDate = order.EndDate ?? DateTime.UtcNow.AddYears(1), // Assuming a default duration of 1 year
            //    ProductName = order.SalesType ?? "Default Subscription",
            //    Vertical = vertical,
            //};

            //if (user.Subscriptions != null)
            //{
            //    user.Subscriptions.Add(subscription);
            //}
            //else
            //{
            //    user.Subscriptions = new List<UserSubscription>
            //    {
            //        subscription
            //    };
            //}

            //await _userManager.UpdateAsync(user);

            return;
        }

        private async Task<ApplicationUser> FindOrCreateUser(long userId, string userName, string email, string mobile, CancellationToken cancellationToken)
        {
            // look for the user using their legacy user ID
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUid == userId, cancellationToken);

            if(user == null)
            {
                // try and see if we can find the user by email
                user = await _userManager.FindByEmailAsync(email);
            }

            if(user == null)
            {
                // try and see if we can find the user by userName
                user = await _userManager.FindByNameAsync(userName);
            }

            // if we definitely do not have this user in our DB, so create him
            if (user == null)
            {
                // Create new user
                var randomPassword = GenerateRandomPassword();

                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    PhoneNumber = mobile,
                    FirstName = userName,
                    LastName = null,
                    LegacyUid = userId,
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
                    throw new RegistrationValidationException(errors);
                }
            }

            return user;
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

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
