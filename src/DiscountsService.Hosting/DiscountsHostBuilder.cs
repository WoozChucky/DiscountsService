using System.Reflection;
using DiscountsService.Hosting.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscountsService.Hosting;

public static class DiscountsHostBuilder
{
    public static HostApplicationBuilder CreateHost(string[] args)
    {
        // workaround for https://github.com/dotnet/project-system/issues/3619
        var assemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(assemblyPath))
        {
            // may be null in single file deployment
            Directory.SetCurrentDirectory(Path.GetDirectoryName(assemblyPath)!);
        }

        var host = new HostApplicationBuilder(args);
        host.Services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);
        host.Services.AddCoreServices(host.Configuration);

        return host;
    }

    public static async Task RunAsync<T>(IHost host)
    {
        await host.RunAsync();
    }
}
