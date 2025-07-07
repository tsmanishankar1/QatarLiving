namespace QLN.Backend.API.ServiceConfiguration
{
    public static class SubscriptionDependencyInjection
    {
        public static IServiceCollection SubscriptionConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddTransient<IExternalSubscriptionService, ExternalSubscriptionService>();
            return services;
        }
    }
}
