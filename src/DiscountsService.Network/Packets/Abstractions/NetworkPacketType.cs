namespace DiscountsService.Network.Packets.Abstractions;

public enum NetworkPacketType : ushort
{
    Invalid = 0,
    GenerateDiscountsRequest = 1,
    GenerateDiscountsResponse = 2,
    UseDiscountRequest = 3,
    UseDiscountResponse = 4
}
