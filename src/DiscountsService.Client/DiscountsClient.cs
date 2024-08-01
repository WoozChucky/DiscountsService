using System.Net;
using System.Net.Sockets;
using DiscountsService.Infrastructure.Threading;
using DiscountsService.Network.Packets;
using DiscountsService.Network.Packets.Abstractions;
using ProtoBuf;

namespace DiscountsService.Client;

public delegate void GenerateDiscountsHandler(object sender, GenerateDiscountResponsePacket packet);
public delegate void UseDiscountHandler(object sender, UseDiscountResponsePacket packet);

public class DiscountsClient
{
    public event GenerateDiscountsHandler? GenerateDiscountResponse;
    public event UseDiscountHandler? UseDiscountResponse;
    
    private readonly CancellationTokenSource _cts;
    private readonly Socket _socket;
    private readonly IPEndPoint _endPoint;
    private readonly RingBuffer<Packet> _receivedPacketBuffer;
    private readonly RingBuffer<NetworkPacket> _sendPacketBuffer;
    
    private Stream _stream;

    public DiscountsClient(string host, ushort port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;
        _cts = new CancellationTokenSource();
        _endPoint = new IPEndPoint(IPAddress.Parse(host), port);
        
        _receivedPacketBuffer = new RingBuffer<Packet>(100);
        _sendPacketBuffer = new RingBuffer<NetworkPacket>(100);
    }

    public async Task ConnectAsync()
    {
        await _socket.ConnectAsync(_endPoint);
        
        _stream = new NetworkStream(_socket, true);
        
#pragma warning disable CS4014
        Task.Run(ProcessPacketsAsync);
        Task.Run(HandleCommunications);
        Task.Run(ProcessReceivedPackets);
#pragma warning restore CS4014
    }
    
    public void Disconnect()
    {
        _cts.Cancel();
        _socket.Disconnect(false);
    }
    
    private async Task ProcessPacketsAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var packet = await _sendPacketBuffer.DequeueAsync(_cts.Token);

                if (packet is null)
                {
                    continue;
                }
                
                await SendQueuedPacketAsync(packet);
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private void HandleCommunications()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var packet = Serializer.DeserializeWithLengthPrefix<NetworkPacket>(_stream, PrefixStyle.Base128);
                
                if (packet == null)
                {
                    Console.WriteLine("Received null packet in network thread");
                    continue;
                }
                
                var innerPacket = GetInnerPacket(packet);
                
                _receivedPacketBuffer.Enqueue(innerPacket);
            }
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine(e);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async void ProcessReceivedPackets()
    {
        try
        {
            async void Send(Action action)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
            
            while (!_cts.IsCancellationRequested)
            {
                var packet = await _receivedPacketBuffer.DequeueAsync(_cts.Token);

                if (packet is null)
                {
                    Console.WriteLine("Received packet in packet processor thread");
                    continue;
                }

                switch (packet)
                {
                    case GenerateDiscountResponsePacket p:
                        Send(() => GenerateDiscountResponse?.Invoke(this, p));
                        break;
                    case UseDiscountResponsePacket p:
                        Send(() => UseDiscountResponse?.Invoke(this, p));
                        break;
                }
                
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private async Task SendQueuedPacketAsync(NetworkPacket packet)
    {
        Serializer.SerializeWithLengthPrefix(_stream, packet, PrefixStyle.Base128);
        await _stream.FlushAsync();
    }
    
    private Packet GetInnerPacket(NetworkPacket packet)
    {
        return packet.Header.Type switch
        {
            // Handshake
            NetworkPacketType.GenerateDiscountsResponse => Serializer.Deserialize<GenerateDiscountResponsePacket>(packet.Payload.AsMemory()),
            NetworkPacketType.UseDiscountResponse => Serializer.Deserialize<UseDiscountResponsePacket>(packet.Payload.AsMemory()),
            
            _ => throw new InvalidOperationException("Unknown packet type " + packet.Header.Type)
        };
    }

    public void SendPacket(NetworkPacket packet)
    {
        _sendPacketBuffer.Enqueue(packet);
    }
}
