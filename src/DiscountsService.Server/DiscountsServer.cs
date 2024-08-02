using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using DiscountsService.Infrastructure.Configuration;
using DiscountsService.Infrastructure.Networking;
using Microsoft.Extensions.Options;

namespace DiscountsService.Server;

public class DiscountsServer(
    IServiceProvider serviceProvider,
    IPacketManager packetManager,
    ILoggerFactory loggerFactory,
    IOptions<HostingConfiguration> hostingOptions)
    : ServerBase<DiscountsConnection>(packetManager, loggerFactory.CreateLogger<DiscountsServer>(), serviceProvider, hostingOptions)
{
    private new ImmutableArray<IDiscountsConnection> Connections =>
        [..base.Connections.Values.Cast<DiscountsConnection>()];
    
    private readonly ConcurrentDictionary<Type, (PropertyInfo packetProperty, PropertyInfo connectionProperty)> _propertyCache = new();
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Here for example we could secure the communications by using a certificate, that load would happen here
        // so that connections could then access the certificate to authenticate themselves
        
        RegisterNewConnectionListener(OnNewConnection);
        
        return Task.CompletedTask;
    }
    
    protected override Task OnStoppingAsync(CancellationToken stoppingToken)
    {
        foreach (var connection in Connections)
        {
            connection.Close();
        }
        return Task.CompletedTask;
    }
    
    private bool OnNewConnection(IConnection connection)
    {
        // Additional validation could be done here, for example checking if the connection is allowed, if the server
        // if full, etc.
        return true;
    }
    
    protected override object GetContextPacket(IConnection connection, object? packet, Type packetType)
    {
        // Check if the cache contains the property accessors for the given packet type
        if (!_propertyCache.TryGetValue(packetType, out var cachedProperties))
        {
            // Cache miss: Reflect the properties
            var contextPacketProperty = typeof(DiscountsPacketContext<>).MakeGenericType(packetType)
                .GetProperty(nameof(DiscountsPacketContext<object>.Packet))!;
            var contextConnectionProperty = typeof(DiscountsPacketContext<>).MakeGenericType(packetType)
                .GetProperty(nameof(DiscountsPacketContext<object>.Connection))!;

            // Cache the reflected properties
            cachedProperties = (contextPacketProperty, contextConnectionProperty);
            _propertyCache[packetType] = cachedProperties;
        }

        // Create a new context instance
        var context = Activator.CreateInstance(typeof(DiscountsPacketContext<>).MakeGenericType(packetType))!;
    
        // Set the packet and connection properties
        cachedProperties.packetProperty.SetValue(context, packet);
        cachedProperties.connectionProperty.SetValue(context, connection);
    
        return context;
    }
}
