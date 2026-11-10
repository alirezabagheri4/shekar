using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shekar.Domain.Entities;

namespace Shekar.Infrastructure.Persistence.Configurations;

public sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> b)
    {
        b.ToTable("VehicleModels").HasComment("کاتالوگ خودرو + نقدشوندگیِ دستی هر مدل؛ مرجع نرمال‌سازی و سگمنت‌بندی");
        b.HasKey(x => x.Id);
        b.Property(x => x.Brand).HasMaxLength(60).IsRequired().HasComment("برند، مثل «پژو»");
        b.Property(x => x.Model).HasMaxLength(60).IsRequired().HasComment("مدل، مثل «۲۰۷»");
        b.Property(x => x.Trim).HasMaxLength(80).HasComment("تریم/تیپ، مثل «دستی پانوراما»");
        b.Property(x => x.DivarKey).HasMaxLength(120).HasComment("شناسه‌ی قطعیِ مدل در دیوار (brand_model)؛ یکتا؛ مبنای رجیستر خودکار");
        b.Property(x => x.BodyType).HasMaxLength(40).HasComment("نوع بدنه: هاچ‌بک/سدان/شاسی‌بلند");
        b.Property(x => x.LiquidityScore).HasComment("نقدشوندگی از پیش‌تعیین‌شده ۰..۱۰۰ (سرعت فروش)");
        b.Property(x => x.ColorSensitive).HasComment("آیا رنگ برای این مدل همیشه در قیمت مهم است");
        b.Property(x => x.IsActive).HasComment("آیا مدل فعال است");
        b.Property(x => x.Notes).HasMaxLength(300).HasComment("یادداشت");

        var aliasesConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<IReadOnlyList<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        var aliasesComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, c) => (a ?? new List<string>()).SequenceEqual(c ?? new List<string>()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());
        b.Property(x => x.Aliases).HasConversion(aliasesConverter).Metadata.SetValueComparer(aliasesComparer);
        b.Property(x => x.Aliases).HasColumnType("nvarchar(max)").HasComment("نام‌های مستعار برای تطبیق متن آگهی (JSON)");

        b.HasIndex(x => new { x.Brand, x.Model, x.Trim });
        // نگاشتِ قطعی از شناسه‌ی منبع؛ یکتا میان مقادیر غیرتهی.
        b.HasIndex(x => x.DivarKey).IsUnique().HasFilter("[DivarKey] IS NOT NULL");
    }
}

public sealed class CarListingConfiguration : IEntityTypeConfiguration<CarListing>
{
    public void Configure(EntityTypeBuilder<CarListing> b)
    {
        b.ToTable("CarListings").HasComment("آگهی‌های خام کراول‌شده (دیوار/باما) پیش از اسکورینگ");
        b.HasKey(x => x.Id);
        b.Property(x => x.Source).HasConversion<byte>().HasComment("منبع: 1=دیوار، 2=باما");
        b.Property(x => x.ExternalId).HasMaxLength(80).IsRequired().HasComment("شناسه‌ی آگهی در منبع (یکتا با Source)");
        b.Property(x => x.Url).HasMaxLength(500).HasComment("آدرس آگهی در منبع");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان آگهی");
        b.Property(x => x.Description).HasColumnType("nvarchar(max)").HasComment("توضیحات آگهی");
        b.Property(x => x.VehicleModelId).HasComment("مدل خودرو پس از نرمال‌سازی؛ NULL تا تطبیق");
        b.Property(x => x.RawBrand).HasMaxLength(60).HasComment("برند خام");
        b.Property(x => x.RawModel).HasMaxLength(60).HasComment("مدل خام");
        b.Property(x => x.RawTrim).HasMaxLength(80).HasComment("تریم خام");
        b.Property(x => x.SourceModelKey).HasMaxLength(120).HasComment("شناسه‌ی قطعیِ مدل در منبع (brand_model دیوار)؛ مبنای نگاشت/رجیستر خودکار");
        b.Property(x => x.ModelYear).HasComment("سال مدل");
        b.Property(x => x.MileageKm).HasComment("کارکرد (کیلومتر)؛ NULL=صفر/نامشخص");
        b.Property(x => x.Color).HasMaxLength(40).HasComment("رنگ خودرو");
        b.Property(x => x.BodyCondition).HasConversion<byte>().HasComment("وضعیت بدنه: 0=نامشخص،1=سالم،2=رنگ،3=تعویض،4=تصادفی");
        b.Property(x => x.PriceToman).HasComment("قیمت آگهی (تومان)");
        b.Property(x => x.CityId).HasComment("شهر نگاشت‌شده؛ NULL=نامشخص");
        b.Property(x => x.RawLocation).HasMaxLength(120).HasComment("شهر/محله‌ی خام");
        b.Property(x => x.AdvertiserPhone).HasMaxLength(15).HasComment("شماره آگهی‌دهنده (فیلتر بلاک)");
        b.Property(x => x.ImageCount).HasComment("تعداد عکس (شرط ذخیره: >۰)");
        b.Property(x => x.FirstImageUrl).HasMaxLength(500).HasComment("آدرس اولین عکس");
        b.Property(x => x.PublishedAt).HasComment("زمان انتشار در منبع (فیلتر ۲۴ ساعت)");
        b.Property(x => x.FetchedAt).HasComment("زمان اولین دریافت");
        b.Property(x => x.LastSeenAt).HasComment("آخرین دیده‌شدن در کراول (تشخیص حذف)");
        b.Property(x => x.State).HasConversion<byte>().HasComment("وضعیت: 1=فعال، 2=حذف‌شده");
        b.Property(x => x.RemovedAt).HasComment("زمان تشخیص حذف");
        b.Property(x => x.IsScored).HasComment("آیا اسکورینگ پردازش کرده");
        b.Property(x => x.ContentHash).HasMaxLength(64).HasComment("اثرانگشت محتوا (تشخیص تکرار نزدیک)");
        b.Property(x => x.RawJson).HasColumnType("nvarchar(max)").HasComment("بدنه‌ی خام آگهی (JSON)");

        b.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.SetNull);

        // جلوگیری از تکرار آگهی از یک منبع:
        b.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
        // صف اسکورینگ آگهی‌های فعالِ پردازش‌نشده:
        b.HasIndex(x => new { x.State, x.IsScored });
        // لیست روزانه‌ی شهر:
        b.HasIndex(x => new { x.CityId, x.PublishedAt, x.State });
        b.HasIndex(x => x.AdvertiserPhone);
    }
}

public sealed class MarketSegmentStatConfiguration : IEntityTypeConfiguration<MarketSegmentStat>
{
    public void Configure(EntityTypeBuilder<MarketSegmentStat> b)
    {
        b.ToTable("MarketSegmentStats").HasComment("آمار ساعتیِ هر سگمنت بازار؛ مرجع تشخیص زیرقیمت‌بودن");
        b.HasKey(x => x.Id);
        b.Property(x => x.SegmentKey).HasMaxLength(120).IsRequired().HasComment("کلید یکتای سگمنت (upsert)");
        b.Property(x => x.VehicleModelId).HasComment("مدل خودرو");
        b.Property(x => x.ModelYear).HasComment("سال مدل");
        b.Property(x => x.MileageBucket).HasConversion<byte>().HasComment("باکت کارکرد: 0=صفر..5=+۱۵۰هزار");
        b.Property(x => x.BodyCondition).HasConversion<byte>().HasComment("وضعیت بدنه");
        b.Property(x => x.ColorKey).HasMaxLength(40).HasComment("رنگ؛ NULL=در این سگمنت دخیل نیست");
        b.Property(x => x.SampleCount).HasComment("تعداد نمونه در پنجره");
        b.Property(x => x.MedianPriceToman).HasComment("میانه‌ی قیمت (مرجع اصلی)");
        b.Property(x => x.AveragePriceToman).HasComment("میانگین قیمت");
        b.Property(x => x.P25PriceToman).HasComment("چارک اول قیمت");
        b.Property(x => x.P75PriceToman).HasComment("چارک سوم قیمت");
        b.Property(x => x.WindowDays).HasComment("طول پنجره‌ی نمونه‌گیری (روز)");
        b.Property(x => x.ComputedAt).HasComment("زمان آخرین محاسبه");

        b.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.SegmentKey).IsUnique();
        b.HasIndex(x => new { x.VehicleModelId, x.ModelYear, x.MileageBucket, x.BodyCondition });
    }
}

public sealed class CrawlRunConfiguration : IEntityTypeConfiguration<CrawlRun>
{
    public void Configure(EntityTypeBuilder<CrawlRun> b)
    {
        b.ToTable("CrawlRuns").HasComment("لاگ هر پاس کراول برای پایش و عیب‌یابی");
        b.HasKey(x => x.Id);
        b.Property(x => x.Source).HasConversion<byte>().HasComment("منبع کراول");
        b.Property(x => x.CityId).HasComment("شهر هدف؛ NULL=همه");
        b.Property(x => x.StartedAt).HasComment("زمان شروع");
        b.Property(x => x.FinishedAt).HasComment("زمان پایان");
        b.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت: 1=درحال‌اجرا،2=موفق،3=خطا");
        b.Property(x => x.FetchedCount).HasComment("تعداد دریافت‌شده");
        b.Property(x => x.SavedCount).HasComment("تعداد تازه‌ی ذخیره‌شده");
        b.Property(x => x.SkippedCount).HasComment("تعداد ردشده");
        b.Property(x => x.RemovedCount).HasComment("تعداد حذف‌شده‌ی تشخیص‌داده‌شده");
        b.Property(x => x.ErrorCount).HasComment("تعداد خطا");
        b.Property(x => x.Error).HasColumnType("nvarchar(max)").HasComment("پیام خطا");
        b.HasIndex(x => new { x.Source, x.StartedAt });
    }
}
