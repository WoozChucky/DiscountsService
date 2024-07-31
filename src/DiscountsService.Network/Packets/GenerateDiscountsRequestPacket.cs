using DiscountsService.Network.Packets.Abstractions;
using DiscountsService.Network.Packets.Attributes;
using ProtoBuf;

namespace DiscountsService.Network.Packets;

[Packet(Type = NetworkPacketType.GenerateDiscountsRequest)]
public class GenerateDiscountsRequestPacket : Packet
{
    public static readonly ushort PacketVersion = 1;
    public static readonly NetworkPacketType PacketType = NetworkPacketType.GenerateDiscountsRequest;

    [ProtoMember(1)] public required ushort Count { get; init; }
    [ProtoMember(2)] public required byte Length { get; init; }

    public static NetworkPacket Create(ushort count, byte length)
    {
        using var ms = new MemoryStream();

        var packet = new GenerateDiscountsRequestPacket
        {
            Count = count,
            Length = length
        };
        
        Serializer.Serialize(ms, packet);
        
        var result = new NetworkPacket
        {
            Header = new NetworkPacketHeader
            {
                Type = PacketType,
                Version = PacketVersion
            },
            // Payload is of type byte[]
            Payload = ms.ToArray()
        };

        return result;
    }
}
