using System.Net.Sockets;
using DiscountsService.Infrastructure.Networking;
using DiscountsService.Network;
using DiscountsService.Network.Packets.Abstractions;

namespace DiscountsService.Server;

public interface IDiscountsConnection : IConnection
{
    
}

public class DiscountsConnection : Connection, IDiscountsConnection
{
    public DiscountsConnection(ILoggerFactory loggerFactory, TcpClient tcpClient, IServerBase server, IPacketReader packetReader, ushort maxQueuedPackets) 
        : base(loggerFactory.CreateLogger<DiscountsConnection>(), tcpClient, server, packetReader, maxQueuedPackets)
    {
    }

    protected override void OnHandshakeFinished()
    {
        Server.CallConnectionListener(this);
    }

    protected override Task<Stream> GetStream(TcpClient client)
    {
        //NOTE: In case of TLS we could create the NetworkStream, wrap it in SslStream and then authenticate before returning it
        
        return Task.FromResult<Stream>(new NetworkStream(client.Client, true));
    }

    protected override async Task OnClose(bool expected = true)
    {
        await Server.RemoveConnection(this);
    }

    protected override async Task OnReceive(NetworkPacket packet, Packet? payload)
    {
        await Server.CallListener(this, packet, payload);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
