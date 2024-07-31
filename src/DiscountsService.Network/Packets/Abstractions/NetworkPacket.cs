using ProtoBuf;

namespace DiscountsService.Network.Packets.Abstractions;

[ProtoContract]
public class NetworkPacket
{
    [ProtoMember(1)] public NetworkPacketHeader Header { get; set; } = new();
    [ProtoMember(2)] public byte[] Payload { get; set; } = [];

    public int Size => NetworkPacketHeader.Size + Payload?.Length ?? 0;
    
    public static NetworkPacket Deserialize(ReadOnlyMemory<byte> buffer)
    {
        return Serializer.Deserialize<NetworkPacket>(buffer);
    }
}

// Placeholder for actual inherited packet classes
public class Packet { }
