namespace DiscountsService.Infrastructure.Networking;

public readonly struct PacketInfo(Type packetType, Type? packetHandlerType = null)
{
    public Type PacketType { get; } = packetType;
    public Type? PacketHandlerType { get; } = packetHandlerType;
}
