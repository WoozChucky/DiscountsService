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
    public DiscountsConnection(ILogger logger, TcpClient tcpClient, IServerBase server, IPacketReader packetReader, ushort maxQueuedPackets) : base(logger, tcpClient, server, packetReader, maxQueuedPackets)
    {
    }

    protected override void OnHandshakeFinished()
    {
        throw new NotImplementedException();
    }

    protected override Task<Stream> GetStream(TcpClient client)
    {
        throw new NotImplementedException();
    }

    protected override Task OnClose(bool expected = true)
    {
        throw new NotImplementedException();
    }

    protected override Task OnReceive(NetworkPacket packet, Packet? payload)
    {
        throw new NotImplementedException();
    }
}
