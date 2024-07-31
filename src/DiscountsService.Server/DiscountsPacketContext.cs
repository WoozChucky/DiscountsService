namespace DiscountsService.Server;

public struct DiscountsPacketContext<TPacket>
{
    public TPacket Packet {get; set;}
    public IDiscountsConnection Connection {get; set;}
}
