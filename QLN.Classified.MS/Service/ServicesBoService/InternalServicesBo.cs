using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using static QLN.Subscriptions.Actor.ActorClass.PayToPublishPaymentActor;

namespace QLN.Classified.MS.Service.ServicesBoService
{
    public class InternalServicesBo : IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalServicesBo> _logger;
        private readonly IPayToPublishService _payToPublishService;
        

        public InternalServicesBo(
            DaprClient dapr,
            ILogger<InternalServicesBo> logger,
            IPayToPublishService payToPublishService)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _payToPublishService = payToPublishService ?? throw new ArgumentNullException(nameof(payToPublishService));
        }

        public async Task<List<ServiceAdSummaryDto>> GetAllServiceBoAds(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.Services.StoreName,
                    ConstantValues.Services.ServicesIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new();

                var serviceAds = new List<ServiceAdSummaryDto>();

                foreach (var key in keys)
                {
                    var serviceAd = await _dapr.GetStateAsync<ServicesDto>(
                        ConstantValues.Services.StoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (serviceAd == null) continue;

                    // Get payment transaction ID using the payment service
                    Guid? paymentTransactionId = null;
                    try
                    {
                        var userPayments = await _payToPublishService.GetPaymentsByUserIdAsync(
                            serviceAd.CreatedBy.ToString(),
                            cancellationToken);

                        // Find payment for this specific user (comparing UserId with CreatedBy only)
                        var matchingPayment = userPayments.FirstOrDefault(payment =>
                            payment.UserId.ToString() == serviceAd.CreatedBy.ToString());

                        if (matchingPayment != null)
                        {
                            paymentTransactionId = matchingPayment.Id;
                            _logger.LogDebug("Payment found for Ad {AdId}, User {UserId}: TransactionId {TransactionId}",
                                serviceAd.Id, serviceAd.CreatedBy, matchingPayment.Id);
                        }
                        else
                        {
                            _logger.LogDebug("No payment found for Ad {AdId}, User {UserId}",
                                serviceAd.Id, serviceAd.CreatedBy);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error retrieving payment details for user {UserId}, ad {AdId}",
                            serviceAd.CreatedBy, serviceAd.Id);
                        paymentTransactionId = null; // Explicitly set to null on error
                    }

                    var summary = new ServiceAdSummaryDto
                    {
                        Id = serviceAd.Id,
                        UserId = serviceAd.CreatedBy,
                        Title = serviceAd.Title,
                        UserName = serviceAd.UserName,
                        CategoryName = serviceAd.CategoryName,
                        L1CategoryName = serviceAd.L1CategoryName,
                        Status = serviceAd.Status,
                        CreatedAt = serviceAd.CreatedAt,
                        PublishedDate = serviceAd.PublishedDate,
                        ExpiryDate = serviceAd.ExpiryDate,
                        PhotoUpload = serviceAd.PhotoUpload,
                        PaymentTransactionId = paymentTransactionId
                    };

                    serviceAds.Add(summary);
                }

                return serviceAds.OrderByDescending(x => x.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw new Exception("Error retrieving service ads", ex);
            }
        }
    }





}
