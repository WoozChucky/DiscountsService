using DiscountsService.Network.Packets.Abstractions;
using DiscountsService.Network.Packets.Attributes;
using ProtoBuf;

namespace DiscountsService.Network.Packets;

[ProtoContract]
[Packet(Type = NetworkPacketType.UseDiscountRequest)]
public class UseDiscountRequestPacket : Packet
{
    public static readonly ushort PacketVersion = 1;
    public static readonly NetworkPacketType PacketType = NetworkPacketType.UseDiscountRequest;

    [ProtoMember(1)] public required string Code { get; init; }

    public static NetworkPacket Create(string code)
    {
        using var ms = new MemoryStream();

        var packet = new UseDiscountRequestPacket
        {
            Code = code,
        };
        
        Serializer.Serialize(ms, packet);

        return new NetworkPacket
        {
            Header = new NetworkPacketHeader
            {
                Type = PacketType,
                Version = PacketVersion
            },
            Payload = ms.ToArray()
        };
    }
}
