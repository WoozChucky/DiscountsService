using System.Reflection;
using DiscountsService.Network.Packets.Abstractions;
using Microsoft.Extensions.Logging;

namespace DiscountsService.Infrastructure.Networking;

/// <summary>
/// Stores meta information about packets. This information should be only used for deserialization.
/// </summary>
public interface IPacketManager
{
    /// <summary>
    /// Try to get a packet info by an actual packet
    /// </summary>
    /// <param name="packet"></param>
    /// <param name="info"></param>
    /// <returns>True if found</returns>
    bool TryGetPacketInfo(NetworkPacket packet, out PacketInfo info);
}

public class PacketManager : IPacketManager
{
    private readonly Dictionary<NetworkPacketType, PacketInfo> _infos = new();

    public PacketManager(ILoggerFactory loggerFactory, IEnumerable<Type> packetTypes, Type[]? packetHandlerTypes = null)
    {
        var logger = loggerFactory.CreateLogger<PacketManager>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        
        foreach (var packetType in packetTypes)
        {
            var networkPacketTypeInfo = packetType.GetFields(flags)
                .FirstOrDefault(field => field.FieldType == typeof(NetworkPacketType));
            if (networkPacketTypeInfo == null)
            {
                logger.LogWarning("Packet type {PacketType} does not have a NetworkPacketType field", packetType);
                continue;
            }
            
            var networkPacketType = (NetworkPacketType) networkPacketTypeInfo!.GetValue(null)!;
            
            var packetHandlerType = packetHandlerTypes?
                .LastOrDefault(x =>
                    x is { IsAbstract: false, IsInterface: false } &&
                    x
                        .GetInterfaces()
                        .Any(i => i.IsGenericType && i.GenericTypeArguments.First() == packetType)
                );

            if (packetHandlerType == null)
            {
                logger.LogWarning("Packet {PacketType} does not have a handler", packetType);
                continue;
            }
            
            _infos.Add(networkPacketType, new PacketInfo(packetType, packetHandlerType));
            
            logger.LogDebug("Registered packet {Header} with handler {HandlerType}", networkPacketType, packetHandlerType);
        }
    }

    public bool TryGetPacketInfo(NetworkPacket packet, out PacketInfo packetInfo)
    {
        return _infos.TryGetValue(packet.Header.Type, out packetInfo);
    }
}
