using DiscountsService.Hosting;
using DiscountsService.Persistence;
using DiscountsService.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DiscountsService.Server;

public class Program
{
    private static async Task Main(string[] args)
    {
        var hostBuilder = DiscountsHostBuilder.CreateHost(args);
        hostBuilder.Services.AddHostedService<DiscountsServer>();
        hostBuilder.Services.AddDatabase();
        
        var host = hostBuilder.Build();
        
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DiscountsDbContext>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Migrating database if necessary...");
            await db.Database.MigrateAsync();
        }
        
        await DiscountsHostBuilder.RunAsync<Program>(host);
    }
}
