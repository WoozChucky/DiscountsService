using DiscountsService.Persistence.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscountsService.Persistence.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string databaseSection = "Database")
    {
        services.AddOptions<DatabaseConfiguration>()
            .BindConfiguration(databaseSection);
        
        services.AddScoped<DiscountsDbContext>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var options = provider.GetRequiredService<IOptionsSnapshot<DatabaseConfiguration>>();
            return new DiscountsDbContext(loggerFactory, options);
        });

        return services;
    }
    
}
