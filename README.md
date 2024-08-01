# DiscountsService

## Key points
- The service is using raw tcp sockets for communication
- I emphasized the tcp server infrastructure to be reusable and easy to extend for other possible applications communicating in the same manner.
  - The server is using a `Command` pattern to handle incoming packets.
  - .NET's service provider is being used to inject dependencies into the packet handlers by creating an async scope for each incoming packet.
  - The packet handlers are stateless and can be easily replaced or added.
- For performance and simplicity, packets are being serialized/deserialized using `Protobuf`
  - A simple BinaryWriter/Reader could have been used since most packets are fairly simple, but Protobuf is more flexible and easier to maintain hence the choice.
- For the storage EntityFramework with MySQL is being used.
  - If performance was a key consideration for this service in a production scenario, probably something like Dapper would have been more suitable.
- The discount codes generation is using an optimistic approach, meaning that it will not check if the code already exists, when trying to save. Because the code is unique, the application will handle that and try to regenerate the duplicate codes.
  - The algorithm itself is very simple and consists of generating a random alphanumeric string of the desired length.
  - I tried the approach of checking if the code already exists, but it was not worth the performance hit of hammering the database.
  - If this piece of code was in fact in a hot path of the application, I would consider using different techniques to avoid the database hit, like for example using a pool of preloaded codes (altough if the aplication was inteded to be replicated this would no longer be a valid option unless the pool was in some distributed memory cache).

## Requirements
- .NET 8.0 SDK
- Docker

## How to run
- The service is using docker-compose to run the MySQL database.
  - In the root directory of the solution run `docker-compose up -d` to start the database.
  - Keep in mind that it's expecting the port 3306 to be available on the host machine.
- Server
  - In the directory `src/DiscountsService.Server` run `dotnet run` to start the server.
  - By default it listens on port 25000 on all network interfaces (can be changed in appsettings.json).
- Client
  - In the directory `src/DiscountsService.Client` run `dotnet run` to start the client.
  - The client will automatically connect to the server on it's default port.
  - A very simple menu will then show in the console where you can see the commands available and interact with the server.
    - Generate discount code: `1 <number of codes> <code length>`
      - Example: `1 10 7` 
    - Use discount code: `2 <code>`
      - Example: `2 12345679` 
    - Exit: `3`

## Additional notes
- The client is very simple and there's almost no error handling.
