using System.Diagnostics;
using DiscountsService.Network.Packets;
using DiscountsService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiscountsService.Server.Handlers;

public class UseDiscountHandler : IDiscountsPacketHandler<UseDiscountRequestPacket>
{
    private readonly DiscountsDbContext _db;
    private readonly ILogger<UseDiscountHandler> _logger;
    
    public UseDiscountHandler(ILoggerFactory loggerFactory, DiscountsDbContext db)
    {
        _logger = loggerFactory.CreateLogger<UseDiscountHandler>();
        _db = db;
    }
    
    public async Task ExecuteAsync(DiscountsPacketContext<UseDiscountRequestPacket> ctx, CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        var code = ctx.Packet.Code;
        
        if (string.IsNullOrWhiteSpace(code))
        {
            ctx.Connection.Send(UseDiscountResponsePacket.Create(false));
            return;
        }
        
        var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Code == code, token);
        
        if (discount is null)
        {
            ctx.Connection.Send(UseDiscountResponsePacket.Create(false));
            return;
        }
        
        if (discount.Used)
        {
            ctx.Connection.Send(UseDiscountResponsePacket.Create(false));
            return;
        }
        
        discount.Used = true;
        discount.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(token);
        
        ctx.Connection.Send(UseDiscountResponsePacket.Create(true));
        
        _logger.LogInformation("Discount code {Code} used by {RemoteEndPoint} in {ElapsedMilliseconds}ms", code, ctx.Connection.RemoteEndPoint, sw.ElapsedMilliseconds);
    }
}
