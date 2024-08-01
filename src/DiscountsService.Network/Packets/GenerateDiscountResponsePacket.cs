using DiscountsService.Network.Packets.Abstractions;
using ProtoBuf;

namespace DiscountsService.Network.Packets;

[ProtoContract]
public class GenerateDiscountResponsePacket : Packet
{
    public static readonly ushort PacketVersion = 1;
    public static readonly NetworkPacketType PacketType = NetworkPacketType.GenerateDiscountsResponse;

    [ProtoMember(1)] public required bool Result { get; init; }

    public static NetworkPacket Create(bool result)
    {
        using var ms = new MemoryStream();

        var packet = new GenerateDiscountResponsePacket
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
