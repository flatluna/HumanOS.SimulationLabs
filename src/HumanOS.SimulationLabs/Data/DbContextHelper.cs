using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Data;

public static class DbContextHelper
{
    public const string DefaultAzureSqlConnectionString =
        "Server=tcp:flatsqlserver.database.windows.net,1433;Initial Catalog=HumanOSDev;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";

    public static SimulationLabsDbContext CreateDbContext(string? customConnectionString = null)
    {
        var connectionString = customConnectionString
            ?? Environment.GetEnvironmentVariable("HumanOSDatabase")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__HumanOSDatabase")
            ?? DefaultAzureSqlConnectionString;

        var sqlConnection = new SqlConnection(connectionString);

        if (!connectionString.Contains("Password", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var credential = new DefaultAzureCredential();
                var token = credential.GetToken(new TokenRequestContext(["https://database.windows.net/.default"]));
                sqlConnection.AccessToken = token.Token;
            }
            catch
            {
                // Fall back to connection-string configured authentication
            }
        }

        var optionsBuilder = new DbContextOptionsBuilder<SimulationLabsDbContext>();
        optionsBuilder.UseSqlServer(sqlConnection, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(SimulationLabsDbContext).Assembly.FullName);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        return new SimulationLabsDbContext(optionsBuilder.Options);
    }
}
