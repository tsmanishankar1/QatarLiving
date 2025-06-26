using Dapr.Actors.Runtime;
using QLN.Common.Infrastructure.IService.IAddonService;
using static QLN.Common.DTO_s.AddonDto;


namespace QLN.Backend.Actor.ActorClass
{
    public class AddonActor : Dapr.Actors.Runtime.Actor, IAddonActor
    {
        private const string StateKey = "addon-data";
        private readonly ILogger<AddonActor> _logger;

        public AddonActor(ActorHost host, ILogger<AddonActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetAddonDataAsync(AddonDataDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            _logger.LogInformation("[AddonActor {ActorId}] SetAddonDataAsync called", Id);

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            return true;
        }

        public async Task<AddonDataDto?> GetAddonDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[AddonActor {ActorId}] GetAddonDataAsync called", Id);

            var result = await StateManager.TryGetStateAsync<AddonDataDto>(StateKey, cancellationToken);
            return result.HasValue ? result.Value : null;
        }

        protected override Task OnActivateAsync()
        {
            _logger.LogInformation("[AddonActor {ActorId}] Activated", Id);
            return base.OnActivateAsync();
        }
    }
}