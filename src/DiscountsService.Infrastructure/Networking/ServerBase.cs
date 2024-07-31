using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DiscountsService.Infrastructure.Configuration;
using DiscountsService.Network.Packets;
using DiscountsService.Network.Packets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscountsService.Infrastructure.Networking;

public interface IServerBase
{
    Task RemoveConnection(IConnection connection);
    Task CallListener(IConnection connection, NetworkPacket packet, Packet? payload);
    void CallConnectionListener(IConnection connection);
}

public abstract class ServerBase<T> : BackgroundService, IServerBase where T : IConnection
{
    protected class PacketHandlerCache
    {
        public MethodInfo ExecuteMethod { get; set; }
        public Func<IServiceProvider, object> HandlerFactory { get; set; }
    }
    
    protected ushort Port { get; }
    protected TcpListener Listener { get; }
    
    protected IPacketManager PacketManager { get; }
    protected readonly Dictionary<Type, PacketHandlerCache> HandlerCache = new();
    
    protected readonly ConcurrentDictionary<Guid, IConnection> Connections = new();
    
    private readonly ILogger _logger;
    
    private readonly List<Func<IConnection, bool>> _connectionListeners = new();
    private readonly CancellationTokenSource _stoppingToken = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly HostingConfiguration _hostingConfiguration;

    protected ServerBase(
        IPacketManager packetManager, 
        ILogger logger, 
        IServiceProvider serviceProvider, 
        IOptions<HostingConfiguration> hostingOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostingConfiguration = hostingOptions.Value;
        PacketManager = packetManager;
        Port = _hostingConfiguration.Port;

        var localAddr = IPAddress.Parse(_hostingConfiguration.Host);
        Listener = new TcpListener(localAddr, Port);
        Listener.Server.NoDelay = true; // disable Nagle's algorithm
        
        _logger.LogInformation("Initialize tcp server listening on {IP}:{Port}", localAddr, Port);
    }

    protected abstract object GetContextPacket(IConnection connection, object? packet, Type packetType);
    protected abstract Task OnStoppingAsync(CancellationToken stoppingToken);

    public Task RemoveConnection(IConnection connection)
    {
        Connections.Remove(connection.Id, out _);
        
        return Task.CompletedTask;
    }
    
    public override Task StartAsync(CancellationToken token)
    {
        base.StartAsync(token);
        _logger.LogInformation("Start listening for connections...");

        Listener.Start();
        Listener.BeginAcceptTcpClient(OnClientAccepted, Listener);

        return Task.CompletedTask;
    }

    private async void OnClientAccepted(IAsyncResult ar)
    {
        var listener = (TcpListener) ar.AsyncState!;
        var client = listener.EndAcceptTcpClient(ar);

        // will dispose once connection finished executing (canceled or disconnect)
        await using var scope = _serviceProvider.CreateAsyncScope();

        // cannot inject tcp client here
        var connection = ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider, [client, this, _hostingConfiguration.ConnectionMaxQueuedPackets]);
        Connections.TryAdd(connection.Id, connection);

        // accept new connections on another thread
        Listener.BeginAcceptTcpClient(OnClientAccepted, Listener);

        await connection.StartAsync(_stoppingToken.Token);
        await connection.ExecuteTask!.ConfigureAwait(false);
    }

    public void ForAllConnections(Action<IConnection> callback)
    {
        foreach (var (_, connection) in Connections)
        {
            callback(connection);
        }
    }

    public void RegisterNewConnectionListener(Func<IConnection, bool> listener)
    {
        _connectionListeners.Add(listener);
    }

    public async Task CallListener(IConnection connection, NetworkPacket packet, Packet? payload)
    {
        if (!PacketManager.TryGetPacketInfo(packet, out var details) || details.PacketHandlerType is null)
        {
            _logger.LogWarning("Could not find a handler for packet {PacketType}", packet.Header.Type);
            return;
        }
        
        if (!HandlerCache.TryGetValue(details.PacketType, out var handlerCache))
        {
            // Cache reflection information
            var handlerExecuteMethod = details.PacketHandlerType.GetMethod("ExecuteAsync")
                                       ?? throw new InvalidOperationException($"Method 'ExecuteAsync' not found in {details.PacketHandlerType}");

            // Create factory delegate for packet handler instances
            var objectFactory = ActivatorUtilities.CreateFactory(details.PacketHandlerType, []);
            
            // Wrap ObjectFactory in a Func<IServiceProvider, object>
            object HandlerFactory(IServiceProvider sp) => objectFactory(sp, null);

            handlerCache = new PacketHandlerCache
            {
                ExecuteMethod = handlerExecuteMethod,
                HandlerFactory = HandlerFactory
            };

            HandlerCache[details.PacketType] = handlerCache;
        }
        
        var context = GetContextPacket(connection, payload, details.PacketType);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            var packetHandler = handlerCache.HandlerFactory(scope.ServiceProvider);
            await ((Task) handlerCache.ExecuteMethod.Invoke(packetHandler, new[] {context, _stoppingToken.Token})!).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute packet handler");
            connection.Close();
        }
    }

    public void CallConnectionListener(IConnection connection)
    {
        foreach (var listener in _connectionListeners) listener(connection);
    }

    protected void StartListening()
    {
        Listener.Start();
        Listener.BeginAcceptTcpClient(OnClientAccepted, Listener);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await OnStoppingAsync(cancellationToken);
        await _stoppingToken.CancelAsync();
        await base.StopAsync(cancellationToken);
        _stoppingToken.Dispose();
    }
}
