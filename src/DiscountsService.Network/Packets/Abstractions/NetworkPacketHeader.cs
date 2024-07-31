using ProtoBuf;

namespace DiscountsService.Network.Packets.Abstractions;

[ProtoContract]
public class NetworkPacketHeader
{
    [ProtoMember(1)] public NetworkPacketType Type { get; set; }
    [ProtoMember(1)] public ushort Version { get; set; }
    
    public static readonly int Size = 2 + 2;
    
    public static NetworkPacketHeader Deserialize(ReadOnlyMemory<byte> buffer)
    {
        return Serializer.Deserialize<NetworkPacketHeader>(buffer);
    }
}
