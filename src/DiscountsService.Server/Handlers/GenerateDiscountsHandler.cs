using System.Collections.Concurrent;
using DiscountsService.Network.Packets;

namespace DiscountsService.Server.Handlers;

public class GenerateDiscountsHandler : IDiscountsPacketHandler<GenerateDiscountsRequestPacket>
{
    public Task ExecuteAsync(DiscountsPacketContext<GenerateDiscountsRequestPacket> ctx, CancellationToken token = default)
    {

        var numberOfDiscountCodesToGenerate = ctx.Packet.Count; // 0 to 2 million max 
        var lengthOfDiscountCode = ctx.Packet.Length; // imagining this is 7 to 8
        
        var discountCodes = new List<string>();
        
        for (var i = 0; i < numberOfDiscountCodesToGenerate; i++)
        {
            var discountCode = Guid.NewGuid().ToString()[..lengthOfDiscountCode];
            discountCodes.Add(discountCode);
        }
        
        return Task.CompletedTask;
    }
}


public class DiscountCodeGeneratorService
{
    // In the future will be a database system integration
    private readonly ConcurrentDictionary<string, byte> _discountCodes = new();
    
    private static readonly Random Random = new Random();
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int BatchSize = 1000; // Process in batches to control resource usage
    
    public async Task<bool> GenerateAsync(ushort count, byte length)
    {
        var batches = Partition(count, BatchSize);

        var tasks = batches.Select(batch => Task.Run(() => GenerateBatch(batch, length)));
        await Task.WhenAll(tasks);

        return true;
    }
    
    private void GenerateBatch(int count, byte length)
    {
        for (var i = 0; i < count; i++)
        {
            string discountCode;
            do
            {
                discountCode = GenerateRandomCode(length);
            } while (!StoreDiscountCode(discountCode));
        }
    }
    
    private bool StoreDiscountCode(string discountCode)
    {
        // In the future will be a database call
        return _discountCodes.TryAdd(discountCode, 0);
    }
    
    private bool GenerateDiscountCode(byte length, out string discountCode)
    {
        var provisionalDiscountCode = GenerateRandomCode(length);
        
        if (_discountCodes.ContainsKey(provisionalDiscountCode))
        {
            discountCode = string.Empty;
            return false;
        }
        
        _discountCodes.TryAdd(provisionalDiscountCode, 0);
        discountCode = provisionalDiscountCode;
        
        return true;
    }
    
    private static string GenerateRandomCode(int length)
    {
        var code = new char[length];
        lock (Random)
        {
            for (var i = 0; i < length; i++)
            {
                code[i] = Characters[Random.Next(Characters.Length)];
            }
        }
        return new string(code);
    }
    
    private static IEnumerable<int> Partition(int total, int size)
    {
        for (var i = 0; i < total; i += size)
        {
            yield return Math.Min(size, total - i);
        }
    }
}
