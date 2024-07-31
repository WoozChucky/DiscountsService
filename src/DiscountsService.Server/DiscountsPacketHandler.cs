using DiscountsService.Infrastructure.Networking;

namespace DiscountsService.Server;

public interface IDiscountsPacketHandler<T> : IPacketHandler
{
    Task ExecuteAsync(DiscountsPacketContext<T> ctx, CancellationToken token = default);
}
