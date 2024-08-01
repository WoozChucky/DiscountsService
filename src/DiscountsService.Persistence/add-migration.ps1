# Prompt the user for the migration name
$migrationName = Read-Host -Prompt "Enter the migration name"

# Define the command
$command = "dotnet ef migrations add $migrationName --context DiscountsDbContext --output-dir Migrations --startup-project ../DiscountsService.Server -- --Database:ConnectionString ""Server=localhost;Database=discounts;Uid=root;Pwd=123;"""

# Execute the command
Invoke-Expression $command
