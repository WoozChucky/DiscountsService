namespace DiscountsService.Infrastructure.Configuration;

public class HostingConfiguration
{
    public string Host { get; set; } = string.Empty;
    public ushort Port { get; set; }
    public ushort ConnectionMaxQueuedPackets { get; set; }
}
