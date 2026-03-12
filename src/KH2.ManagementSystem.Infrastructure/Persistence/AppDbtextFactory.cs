using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KH2.ManagementSystem.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = BuildConfiguration().GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        foreach (var basePath in GetCandidateBasePaths())
        {
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                continue;
            }

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        throw new InvalidOperationException(
            "Unable to locate appsettings.json for design-time DbContext creation.");
    }

    private static IEnumerable<string> GetCandidateBasePaths()
    {
        yield return Directory.GetCurrentDirectory();
        yield return Path.Combine(Directory.GetCurrentDirectory(), "src", "KH2.ManagementSystem.Api");
        yield return AppContext.BaseDirectory;
        yield return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "KH2.ManagementSystem.Api"));
    }
}
