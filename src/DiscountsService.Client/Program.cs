// See https://aka.ms/new-console-template for more information

using DiscountsService.Client;
using DiscountsService.Network.Packets;

Console.WriteLine("Hello, World!");

var client = new DiscountsClient("127.0.0.1", 21500);
client.GenerateDiscountResponse += (sender, packet) =>
{
    Console.WriteLine($"Received `discount code generation` response: {packet.Result}");
};
client.UseDiscountResponse += (sender, packet) =>
{
    Console.WriteLine($"Received `use discount code` response: {packet.Result}");
};

await client.ConnectAsync();

var running = true;

while (running)
{
    ShowMenu();
    var input = Console.ReadLine();

    if (input == null)
    {
        Console.WriteLine("Invalid command.");
        continue;
    }
    
    var parts = input.Split(' ');
    
    if (parts.Length == 0)
    {
        Console.WriteLine("Invalid command.");
        continue;
    }
    
    var validCommand = int.TryParse(parts[0], out var command);
    if (!validCommand)
    {
        Console.WriteLine("Invalid command.");
        continue;
    }

    switch (command)
    {
        case 1:
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Invalid number of arguments.");
                continue;
            }

            if (!ushort.TryParse(parts[1], out var count))
            {
                Console.WriteLine("Invalid count.");
                continue;
            }

            if (!byte.TryParse(parts[2], out var length))
            {
                Console.WriteLine("Invalid length.");
                continue;
            }

            var req = GenerateDiscountsRequestPacket.Create(count, length);
            
            client.SendPacket(req);
            break;
        }
        case 2:
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid number of arguments.");
                continue;
            }

            var req = UseDiscountRequestPacket.Create(parts[1]);
            
            client.SendPacket(req);
            break;
        }
        case 3:
        {
            running = false;
            break;
        }
    }
}

client.Disconnect();

void ShowMenu()
{
    Console.WriteLine("1. Generate discount. <amount> <length>");
    Console.WriteLine("2. Use code. <code>");
    Console.WriteLine("3. Exit");
}
