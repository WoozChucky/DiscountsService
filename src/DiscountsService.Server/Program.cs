using DiscountsService.Hosting;

namespace DiscountsService.Server;

public class Program
{
    private static async Task Main(string[] args)
    {
        var hostBuilder = DiscountsHostBuilder.CreateHost(args);
        hostBuilder.Services.AddHostedService<DiscountsServer>();
        
        var host = hostBuilder.Build();
        
        // db stuff here
        
        await DiscountsHostBuilder.RunAsync<Program>(host);
    }
}
