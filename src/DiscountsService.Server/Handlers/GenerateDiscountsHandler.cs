using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DiscountsService.Network.Packets;
using DiscountsService.Persistence;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace DiscountsService.Server.Handlers;

public partial class GenerateDiscountsHandler : IDiscountsPacketHandler<GenerateDiscountsRequestPacket>
{
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly DiscountsDbContext _db;
    private readonly ILogger<GenerateDiscountsHandler> _logger;
    private readonly Random _random = new();
    
    private const ushort MinCodes = 1;
    private const ushort MaxCodes = 2_000;
    
    private const byte MinLength = 7;
    private const byte MaxLength = 8;
    
    // 1062 is the error code for a duplicate entry in MySQL provider
    private const int DuplicateEntryErrorCode = 1062;
    
    public GenerateDiscountsHandler(ILoggerFactory loggerFactory, DiscountsDbContext db)
    {
        _logger = loggerFactory.CreateLogger<GenerateDiscountsHandler>();
        _db = db;
    }
    
    public async Task ExecuteAsync(DiscountsPacketContext<GenerateDiscountsRequestPacket> ctx, CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        var numberOfcodes = ctx.Packet.Count;
        var length = ctx.Packet.Length;
        
        if (numberOfcodes is < MinCodes or > MaxCodes)
        {
            ctx.Connection.Send(GenerateDiscountResponsePacket.Create(false));
            return;
        }
        
        if (length is < MinLength or > MaxLength)
        {
            ctx.Connection.Send(GenerateDiscountResponsePacket.Create(false));
            return;
        }
        
        var generatedCodes = GenerateCodes(numberOfcodes, length, token);
        
        while (true)
        {
            if (generatedCodes.Count < numberOfcodes)
            {
                _logger.LogDebug("Failed to generate enough unique codes, trying again");
                var missingCodes = (ushort) (numberOfcodes - generatedCodes.Count);
                generatedCodes.AddRange(GenerateCodes(missingCodes, length, token));
            }
        
            var result = await SaveCodesAsync(generatedCodes, token);
            
            if (result.Success) break;
            
            if (result.DuplicateEntry is not null)
            {
                generatedCodes.Remove(result.DuplicateEntry);
            }
            else
            {
                generatedCodes.Clear();
            }
        }
        
        sw.Stop();
        _logger.LogInformation("Generated and saved {Amount} codes in {Elapsed}ms", generatedCodes.Count, sw.ElapsedMilliseconds);
        
        ctx.Connection.Send(GenerateDiscountResponsePacket.Create(true));
    }
    
    private List<DiscountCode> GenerateCodes(ushort amount, ushort length, CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        var codes = new List<DiscountCode>();
        
        for (var i = 0; i < amount; i++)
        {
            var code = new DiscountCode
            {
                Code = GenerateRandomCode(length),
                Used = false,
                UsedAt = null,
                CreatedAt = DateTime.UtcNow
            };
            
            codes.Add(code);
        }
        
        sw.Stop();
        _logger.LogInformation("Generated {Amount} codes in {Elapsed}ms", codes.Count, sw.ElapsedMilliseconds);
        
        return codes;
    }
    
    private async Task<(bool Success, DiscountCode? DuplicateEntry)> SaveCodesAsync(List<DiscountCode> codes, CancellationToken token = default)
    {
        var sw = Stopwatch.StartNew();
        // We need to detect if there was a unique constraint violation,
        // in case it was we should regenerate the codes and try again
        try
        {
            await _db.DiscountCodes.AddRangeAsync(codes, token);
            
            await _db.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is not MySqlException {Number: DuplicateEntryErrorCode}) return (false, null); // TODO: Const for the error code
            
            var duplicateCode = ExtractDuplicateEntry(ex.Message);
            var duplicateEntry = ex.Entries
                .Select(e => e.Entity as DiscountCode)
                .FirstOrDefault(dc => dc?.Code == duplicateCode);

            return (false, duplicateEntry);
        }
        
        sw.Stop();
        _logger.LogInformation("Saved {Amount} codes in {Elapsed}ms", codes.Count, sw.ElapsedMilliseconds);
        
        return (true, null);
    }

    
    
    private static string? ExtractDuplicateEntry(string errorMessage)
    {
        // Call the source-generated regex method to match the pattern
        var match = DuplicateEntryRegex().Match(errorMessage);

        // Check if the match was successful and contains two captured groups
        if (match is {Success: true, Groups.Count: >= 2})
        {
            // Return the first captured group which contains the duplicate entry
            return match.Groups[1].Value;
        }

        // Return null if no match is found
        return null;
    }
    
    private string GenerateRandomCode(ushort length)
    {
        var code = new char[length];
        
        for (var i = 0; i < length; i++)
        {
            code[i] = Characters[_random.Next(Characters.Length)];
        }
        
        return new string(code);
    }
    
    [GeneratedRegex("'([^']*)'")]
    private static partial Regex DuplicateEntryRegex();
}
