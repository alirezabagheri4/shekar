using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// درجِ کاتالوگ کاملِ خودروی دیوار (brand_model) در VehicleModels از روی اسکریپتِ embedded.
    /// هر ردیف با <c>Notes='divar-catalog'</c> نشان‌دار است تا Down دقیق باشد و ردیف‌های
    /// دستی/رجیسترِ خودکار دست‌نخورده بمانند.
    /// </summary>
    public partial class SeedDivarVehicleModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Assembly asm = typeof(SeedDivarVehicleModels).Assembly;
            string resourceName = asm.GetManifestResourceNames()
                .Single(n => n.EndsWith("DivarVehicleModels.sql", System.StringComparison.Ordinal));

            using Stream stream = asm.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            string sql = reader.ReadToEnd();

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM dbo.VehicleModels WHERE Notes = N'divar-catalog';");
        }
    }
}
