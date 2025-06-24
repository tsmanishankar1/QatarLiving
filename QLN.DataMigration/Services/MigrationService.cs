namespace QLN.DataMigration.Services
{
    using Dapr.Client;
    using Microsoft.Extensions.Logging;
    using QLN.Common.Infrastructure.Constants;
    using QLN.DataMigration.Models;
    using System.Threading.Tasks;

    public class MigrationService : IMigrationService
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<MigrationService> _logger;

        public MigrationService(DaprClient daprClient, ILogger<MigrationService> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        public async Task SaveCategoriesAsync(ItemsCategories itemsCategories)
        {
            foreach (var item in itemsCategories.Models)
            {
                await _daprClient.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, item.Id.ToString(), item);
                _logger.LogInformation($"Saving {item.Name} with ID {item.Id} to state");
            }

            _logger.LogInformation("Completed saving all state");
        }

        public async Task SaveMigrationItemsAsync(MigrationItems migrationItems)
        {
            foreach (var item in migrationItems.Items)
            {
                var newGuid = Guid.NewGuid().ToString();

                await _daprClient.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, newGuid, item);

                _logger.LogInformation($"Saving {item.Title} with ID {newGuid} to state");
            }
            _logger.LogInformation("Completed saving all items to state");
        }
    }
}
