using Dapr.Actors;
using Dapr.Actors.Client;
using Google.Protobuf.WellKnownTypes;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IAddonService;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;
using System.Collections.Concurrent;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Backend.API.Service.AddonService
{
    public class ExternalAddonService : IAddonService
    {
        private readonly ILogger<ExternalAddonService> _logger;

        // Dictionary to store consistent actor ID
        private static readonly ConcurrentDictionary<string, Guid> _addonIds = new();
        private readonly ConcurrentDictionary<Guid, byte> _addonPaymentIds = new();
        public ExternalAddonService(ILogger<ExternalAddonService> logger)
        {
            _logger = logger;

            // Ensure at least one consistent addon ID is registered
            _addonIds.TryAdd("default", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }

        private IAddonActor GetActorProxy(Guid id)
        {
            return ActorProxy.Create<IAddonActor>(new ActorId(id.ToString()), "AddonActor");
        }

        private async Task<AddonDataDto> GetOrCreateAddonDataAsync(CancellationToken cancellationToken = default)
        {
            var addonId = _addonIds["default"];
            var actor = GetActorProxy(addonId);
            var data = await actor.GetAddonDataAsync(cancellationToken);

            if (data == null)
            {
                data = new AddonDataDto
                {
                    Id = addonId,
                    LastUpdated = DateTime.UtcNow,
                    Quantities = new List<Quantities>(),
                    Currencies = new List<Currency>(),
                    QuantitiesCurrencies = new List<UnitCurrency>()
                };
                await actor.SetAddonDataAsync(data, cancellationToken);
                _logger.LogInformation("Created new addon data with ID: {AddonId}", addonId);
            }

            return data;
        }

        private async Task SaveAddonDataAsync(AddonDataDto data, CancellationToken cancellationToken = default)
        {
            data.LastUpdated = DateTime.UtcNow;
            var actor = GetActorProxy(data.Id);
            await actor.SetAddonDataAsync(data, cancellationToken);
            _logger.LogDebug("Saved addon data with ID: {AddonId}", data.Id);
        }

        // Quantities methods
      
       public async Task<IEnumerable<QuantityResponse>> GetAllQuantitiesAsync()
        {
            var data = await GetOrCreateAddonDataAsync();

            var response = data.Quantities?
                .Select(q => new QuantityResponse
                {
                    QuantitiesId = q.QuantitiesId,
                    QuantitiesName = q.QuantitiesName
                }).ToList() ?? new List<QuantityResponse>();

            _logger.LogInformation("Retrieved {Count} quantities (excluding CreatedAt)", response.Count);

            return response;
        }

        public async Task<Quantities> CreateQuantityAsync(CreateQuantityRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var data = await GetOrCreateAddonDataAsync();

            var quantity = new Quantities
            {
                QuantitiesId = Guid.NewGuid(),
                QuantitiesName = request.QuantitiesName,
                CreatedAt = DateTime.UtcNow
            };

            data.Quantities ??= new List<Quantities>();
            data.Quantities.Add(quantity);

            await SaveAddonDataAsync(data);

            _logger.LogInformation("Created quantity with ID: {QuantitiesId}, Name: {QuantitiesName}",
                quantity.QuantitiesId, quantity.QuantitiesName);

            return quantity;
        }

        // Currencies methods
      

        public async Task<Currency> CreateCurrencyAsync(CreateCurrencyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var data = await GetOrCreateAddonDataAsync();

            var currency = new Currency
            {
                CurrencyId = Guid.NewGuid(),
                CurrencyName = request.CurrencyName,
                CreatedAt = DateTime.UtcNow
            };

            data.Currencies ??= new List<Currency>();
            data.Currencies.Add(currency);

            await SaveAddonDataAsync(data);

            _logger.LogInformation("Created currency with ID: {CurrencyId}, Name: {CurrencyName}",
                currency.CurrencyId, currency.CurrencyName);

            return currency;
        }

        // UnitCurrency methods
        public async Task<UnitCurrency> CreatequantityCurrencyAsync(CreateUnitCurrencyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var data = await GetOrCreateAddonDataAsync();

            var unitCurrency = new UnitCurrency
            {
                Id = Guid.NewGuid(),
                QuantityId = request.QuantityId,
                CurrencyId = request.CurrencyId,
                Duration = (DurationType)request.durationId,
                CreatedAt = DateTime.UtcNow
            };

            data.QuantitiesCurrencies ??= new List<UnitCurrency>();
            data.QuantitiesCurrencies.Add(unitCurrency);

            await SaveAddonDataAsync(data);

            _logger.LogInformation("Created unit currency with ID: {Id}, QuantityId: {QuantityId}, CurrencyId: {CurrencyId}",
                unitCurrency.Id, unitCurrency.QuantityId, unitCurrency.CurrencyId);

            return unitCurrency;
        }

        public async Task<IEnumerable<UnitCurrencyResponse>> GetByquantityIdAsync(Guid unitId)
        {
            var data = await GetOrCreateAddonDataAsync();

            var result = data.QuantitiesCurrencies?
                .Where(uc => uc.QuantityId == unitId)
                .Select(uc => new UnitCurrencyResponse
                {
                    Id = uc.Id,
                    QuantityId = uc.QuantityId,
                    QuantityName = data.Quantities.FirstOrDefault(q => q.QuantitiesId == uc.QuantityId)?.QuantitiesName,
                    CurrencyId = uc.CurrencyId,
                    CurrencyName = data.Currencies.FirstOrDefault(c => c.CurrencyId == uc.CurrencyId)?.CurrencyName,
                    durationId =  (int)uc.Duration,
                    durationName = System.Enum.GetName(typeof(DurationType), uc.Duration) ?? "Unknown"
                }).ToList() ?? new List<UnitCurrencyResponse>();

            _logger.LogInformation("Retrieved {Count} unit currencies for unit ID: {UnitId}", result.Count, unitId);

            return result;
        }

        public async Task<Guid> CreateAddonPaymentsAsync(
     PaymentAddonRequestDto request,
     Guid userId,
     CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var id = Guid.NewGuid();
            var startDate = DateTime.UtcNow;

            // Get addon data from the default actor (not from request.AddonId)
            var addonData = await GetOrCreateAddonDataAsync(cancellationToken);

            // Find the UnitCurrency that matches the given AddonId
            var unitCurrency = addonData.QuantitiesCurrencies
                .FirstOrDefault(x => x.Id == request.AddonId);

            if (unitCurrency == null)
                throw new Exception($"UnitCurrency not found for Addon ID: {request.AddonId}");

            // Calculate end date using duration
            var endDate = GetEndDateByAddonDuration(startDate, unitCurrency.Duration);

            var dto = new AddonPaymentDto
            {
                Id = id,
                AddonId = request.AddonId,
                VerticalId = request.VerticalId,
                CardNumber = request.CardDetails.CardNumber,
                ExpiryMonth = request.CardDetails.ExpiryMonth,
                ExpiryYear = request.CardDetails.ExpiryYear,
                Cvv = request.CardDetails.Cvv,
                CardHolderName = request.CardDetails.CardHolderName,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                LastUpdated = DateTime.UtcNow,
                IsExpired = false
            };

            var actor = GetAddonPaymentActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);

            if (result)
            {
                _addonPaymentIds.TryAdd(dto.Id, 0);
                _logger.LogInformation("Addon payment transaction created with ID: {TransactionId}", dto.Id);
                return dto.Id;
            }

            throw new Exception("Addon payment transaction creation failed.");
        }

        private async Task<AddonDataDto?> GetAddonDataAsync(Guid addonId)
        {
            var actor = ActorProxy.Create<IAddonActor>(new ActorId(addonId.ToString()), "AddonActor");
            return await actor.GetAddonDataAsync(); 
        }



        private DateTime GetEndDateByAddonDuration(DateTime startDate, DurationType duration)
        {
            return duration switch
            {
                DurationType.ThreeMonths => startDate.AddMonths(3),
                DurationType.SixMonths => startDate.AddMonths(6),
                DurationType.OneYear => startDate.AddYears(1),
                DurationType.TwoMinutes => startDate.AddMinutes(2),
                _ => throw new ArgumentException($"Unsupported DurationType: {duration}")
            };
        }

        private IAddonPaymentActor GetAddonPaymentActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty", nameof(id));

            return ActorProxy.Create<IAddonPaymentActor>(
                new ActorId(id.ToString()),
                "AddonPaymentActor");
        }


    }
}
