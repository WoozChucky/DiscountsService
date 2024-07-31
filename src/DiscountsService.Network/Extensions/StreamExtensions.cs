using DiscountsService.Network.Packets;
using DiscountsService.Network.Packets.Abstractions;
using ProtoBuf;

namespace DiscountsService.Network.Extensions;

public static class StreamExtensions
{
    public static Task WriteAsync(this Stream stream, NetworkPacket packet)
    {
        Serializer.SerializeWithLengthPrefix(stream, packet, PrefixStyle.Base128);
        return Task.CompletedTask;
    }
}
