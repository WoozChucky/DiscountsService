using DiscountsService.Network.Packets.Abstractions;
using DiscountsService.Network.Packets.Attributes;
using ProtoBuf;

namespace DiscountsService.Network.Packets;

[ProtoContract]
public class UseDiscountResponsePacket : Packet
{
    public static readonly ushort PacketVersion = 1;
    public static readonly NetworkPacketType PacketType = NetworkPacketType.UseDiscountResponse;

    [ProtoMember(1)] public required bool Result { get; init; }

    public static NetworkPacket Create(bool result)
    {
        using var ms = new MemoryStream();

        var packet = new UseDiscountResponsePacket
        {
            Result = result
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
