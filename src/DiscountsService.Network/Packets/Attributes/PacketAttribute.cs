using DiscountsService.Network.Packets.Abstractions;

namespace DiscountsService.Network.Packets.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class PacketAttribute : Attribute
{
    public NetworkPacketType Type { get; set; } = NetworkPacketType.Invalid;
}
