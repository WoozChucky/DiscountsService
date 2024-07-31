using System.Reflection;
using DiscountsService.Infrastructure.Configuration;
using DiscountsService.Infrastructure.Networking;
using DiscountsService.Network;
using DiscountsService.Network.Packets.Abstractions;
using DiscountsService.Network.Packets.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DiscountsService.Hosting.Extensions;

public static class ServiceCollectionExtensions
{
    private const string MessageTemplate = "[{Timestamp:HH:mm:ss.fff}][{ThreadId}][{Level:u3}]{Message:lj} {NewLine:1}{Exception:1}";
    
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCustomLogging(configuration);
        services.AddOptions<HostingConfiguration>().BindConfiguration("Hosting");
        
        services.AddSingleton<IPacketManager>(provider =>
        {
            var packetTypes = typeof(Packet).Assembly.GetExportedTypes().Where(type =>
            {
                var packetAttribute = type.GetCustomAttribute<PacketAttribute>();
                var hasPacketAttribute = packetAttribute != null;
                if (!hasPacketAttribute) return false;
                return type.IsClass &&
                       type.GetFields(BindingFlags.Public | BindingFlags.Static)
                           .Any(field => field.FieldType == typeof(NetworkPacketType));
            }).ToArray();
            
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic)
                .SelectMany(x => x.ExportedTypes)
                .Where(x =>
                    x.IsAssignableTo(typeof(IPacketHandler)) &&
                    x is {IsClass: true, IsAbstract: false, IsInterface: false})
                .OrderBy(x => x.FullName)
                .ToArray();
            
            return ActivatorUtilities.CreateInstance<PacketManager>(provider, [packetTypes, handlerTypes]);
        });
        
        services.AddSingleton<IPacketReader, PacketReader>(provider =>
        {
            var packetTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic)
                .SelectMany(x => x.ExportedTypes.Where(type =>
            {
                var packetAttribute = type.GetCustomAttribute<PacketAttribute>();
                var hasPacketAttribute = packetAttribute != null;
                if (!hasPacketAttribute) return false;
                return type.IsClass &&
                       type.GetFields(BindingFlags.Public | BindingFlags.Static)
                           .Any(field => field.FieldType == typeof(NetworkPacketType));
            })).ToArray();
            
            var bufferSize = configuration.GetValue("Networking:BufferSize", 4096);
            
            return ActivatorUtilities.CreateInstance<PacketReader>(provider, [bufferSize, packetTypes]);
        });
        
        return services;
    }
    
    private static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new LoggerConfiguration();

        // add minimum log level for the instances
        config.MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Warning);

        // add destructuring for entities
        config.Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumCollectionCount(10)
            .Destructure.ToMaximumStringLength(100);

        // add environment variable
        config.Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName();

        // add process information
        config.Enrich.WithProcessId()
            .Enrich.WithProcessName();
        
        config.Enrich.WithThreadId();

        config.Enrich.FromLogContext();

        // add exception information
        config.Enrich.WithExceptionData();
        
        // sink to console
        config.WriteTo.Console(outputTemplate: MessageTemplate);

        config.ReadFrom.Configuration(configuration);

        // finally, create the logger
        services.AddLogging(x =>
        {
            x.ClearProviders();
            x.AddSerilog(config.CreateLogger());
        });
        return services;
    }
}
