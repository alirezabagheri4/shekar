using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shekar.Infrastructure.Persistence;

/// <summary>ساخت DbContext در زمان طراحی برای ابزار migrations (dotnet ef).</summary>
public sealed class ShekarDbContextFactory : IDesignTimeDbContextFactory<ShekarDbContext>
{
    public ShekarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShekarDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=Shekar;Trusted_Connection=True;TrustServerCertificate=True;",
            sql => sql.EnableRetryOnFailure());

        return new ShekarDbContext(optionsBuilder.Options);
    }
}
