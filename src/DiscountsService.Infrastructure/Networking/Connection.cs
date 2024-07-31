using System.Net.Sockets;
using System.Text.Json;
using DiscountsService.Infrastructure.Threading;
using DiscountsService.Network;
using DiscountsService.Network.Extensions;
using DiscountsService.Network.Packets.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscountsService.Infrastructure.Networking;

public interface IConnection
{
    Guid Id { get; }
    Task? ExecuteTask { get; }
    string RemoteEndPoint { get; }
    
    void Close(bool expected = true);
    void Send(NetworkPacket packet);
    Task StartAsync(CancellationToken token = default);
}

public abstract class Connection : BackgroundService, IConnection
{
    public Guid Id { get; private set; }
    public string RemoteEndPoint { get; private set; }

    protected readonly ILogger Logger;
    protected readonly IServerBase Server;
    protected CancellationTokenSource? CancellationTokenSource;
    
    private readonly RingBuffer<NetworkPacket> _packetsToSend;
    private readonly IPacketReader _packetReader;
    
    private TcpClient? _client;
    private Stream? _stream;
    private bool _closed = false;
    
    protected Connection(ILogger logger, TcpClient tcpClient, IServerBase server, IPacketReader packetReader, ushort maxQueuedPackets)
    {
        Id = Guid.NewGuid();
        Logger = logger;
        Server = server;
        _client = tcpClient;
        _packetReader = packetReader;
        _packetsToSend = new RingBuffer<NetworkPacket>(maxQueuedPackets);
        _packetsToSend.BufferFull += OnPacketQueueFull;
    }

    protected void Init(TcpClient client)
    {
        _client = client;
    }
    
    protected bool IsConnected => _client?.Connected == true;
    
    protected abstract void OnHandshakeFinished();
    protected abstract Task<Stream> GetStream(TcpClient client);
    protected abstract Task OnClose(bool expected = true);
    protected abstract Task OnReceive(NetworkPacket packet, Packet? payload);
    
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_client is null)
        {
            Logger.LogCritical("Cannot execute when client is null");
            return;
        }

        Logger.LogInformation("New connection from {RemoteEndPoint}", _client.Client.RemoteEndPoint?.ToString());

        _stream = await GetStream(_client);
        
        OnHandshakeFinished();

        try
        {
            await foreach (var packet in _packetReader.EnumerateAsync(_stream, stoppingToken))
            {
                Logger.LogDebug("IN: {Type} => {Data}", packet.Header.Type, JsonSerializer.Serialize(packet));
                
                var payload = _packetReader.Read(packet);

                await OnReceive(packet, payload);
            }
        }
        catch (IOException e)
        {
            Logger.LogDebug(e, "Connection was closed. Probably by the other party");
            Close(false);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to read from stream");
            Close(false);
        }

        Close(false);
    }
    
    public void Close(bool expected = true)
    {
        if (_closed) return;
        CancellationTokenSource?.Cancel();
        _client?.Close();
        _closed = true;
        OnClose(expected);
    }

    public void Send(NetworkPacket packet)
    {
        _packetsToSend.Enqueue(packet);
    }
    
    private async Task SendPacketsWhenAvailable()
    {
        if (_client?.Connected != true)
        {
            Logger.LogWarning("Tried to send data to a closed connection");
            return;
        }

        while (CancellationTokenSource?.IsCancellationRequested != true)
        {
            try
            {
                var packet = await _packetsToSend.DequeueAsync(CancellationTokenSource!.Token).ConfigureAwait(false);
                if (packet != null)
                {
                    try
                    {
                        if (_stream is null)
                        {
                            CancellationTokenSource?.Cancel();
                            Logger.LogCritical("Stream unexpectedly became null. This shouldn't happen");
                            break;
                        }
                        
                        await _stream.WriteAsync(packet).ConfigureAwait(false);
                        await _stream.FlushAsync().ConfigureAwait(false);
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            break;
                        }
                        Logger.LogError(e, "Failed to send packet");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed to send packet");
                    }
                    Logger.LogTrace("OUT: {Type} => {Packet}", packet.Header.Type, JsonSerializer.Serialize(packet));
                }
                else
                {
                    await Task.Delay(1).ConfigureAwait(false); // wait at least 1ms
                }
            }
            catch (SocketException)
            {
                // connection closed. Ignore
                break;
            }
        }
    }
    
    private void OnPacketQueueFull(NetworkPacket packet)
    {
        // TODO: Here multiple things could be done differently, such as wait for the queue to have space to enqueue again, drop the packet, etc
        // for now, we just drop the packet since this is a PoC
        Logger.LogWarning("Packet queue is full. Dropping packet {Packet}", packet.Header.Type);
    }
}
