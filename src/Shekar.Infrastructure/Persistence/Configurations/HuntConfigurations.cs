using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shekar.Domain.Entities;

namespace Shekar.Infrastructure.Persistence.Configurations;

public sealed class HuntConfiguration : IEntityTypeConfiguration<Hunt>
{
    public void Configure(EntityTypeBuilder<Hunt> b)
    {
        b.ToTable("Hunts").HasComment("شکارها: آگهی‌های زیر قیمت بازار با نقدشوندگی بالا (پیشنهادهای روزانه)");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان آگهی");
        b.Property(x => x.ModelYear).HasMaxLength(10).HasComment("سال مدل (شمسی/میلادی طبق آگهی)");
        b.Property(x => x.MileageKm).HasComment("کارکرد به کیلومتر؛ NULL برای صفر");
        b.Property(x => x.Location).HasMaxLength(80).HasComment("محله/منطقه‌ی آگهی");
        b.Property(x => x.CityId).HasComment("شهر آگهی");
        b.Property(x => x.PriceToman).HasComment("قیمت آگهی به تومان");
        b.Property(x => x.MarketMedianToman).HasComment("میانه‌ی قیمت بازار به تومان (R9)");
        b.Property(x => x.BelowMarketPercent).HasComment("درصد زیر بازار");
        b.Property(x => x.Score).HasComment("امتیاز شکار ۰ تا ۱۰۰ (≥۸۵ فرصت طلایی — R8)");
        b.Property(x => x.Liquidity).HasConversion<byte>().HasComment("نقدشوندگی: 1=متوسط، 2=بالا");
        b.Property(x => x.State).HasConversion<byte>().HasComment("وضعیت: 1=فعال، 2=حذف‌شده (R7)");
        b.Property(x => x.PublishedAt).HasComment("زمان انتشار آگهی");
        b.Property(x => x.IsNew).HasComment("نمایش برچسب «تازه»");
        b.Property(x => x.ImageFileName).HasMaxLength(120).HasComment("نام فایل تصویر؛ NULL=placeholder");
        b.Property(x => x.AdvertiserPhone).HasMaxLength(15).HasComment("شماره آگهی‌دهنده؛ برای حذف شماره‌های بلاک‌شده از پیشنهادها");
        b.Property(x => x.SourceListingId).HasComment("آگهی خامِ منبعِ این شکار (خروجی اسکورینگ)؛ NULL برای seed/دستی");

        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SourceListingId);
        b.HasOne(x => x.Analysis).WithOne().HasForeignKey<HuntAnalysis>("HuntId").OnDelete(DeleteBehavior.Cascade);

        // ایندکس‌های پرکاربرد لیست روزانه: شهر + تاریخ + وضعیت، و مرتب‌سازی‌ها.
        b.HasIndex(x => new { x.CityId, x.PublishedAt, x.State });
        b.HasIndex(x => x.Score);
        b.HasIndex(x => x.AdvertiserPhone);
    }
}

public sealed class HuntAnalysisConfiguration : IEntityTypeConfiguration<HuntAnalysis>
{
    public void Configure(EntityTypeBuilder<HuntAnalysis> b)
    {
        b.ToTable("HuntAnalyses").HasComment("تحلیل تفصیلی هر شکار (یک‌به‌یک با Hunts) برای صفحه‌ی جزئیات/کارشناسی");
        b.Property<int>("HuntId").HasComment("کلید اصلی و کلید خارجی به Hunts");
        b.HasKey("HuntId");
        b.Property(x => x.ComparableCount).HasComment("تعداد آگهی‌های مشابه مبنای تحلیل");
        b.Property(x => x.Confidence).HasMaxLength(20).HasComment("سطح اطمینان نمایشی، مثل «بالا»");
        b.Property(x => x.FairRangeMinToman).HasComment("کف بازه‌ی منصفانه (تومان)");
        b.Property(x => x.FairRangeMaxToman).HasComment("سقف بازه‌ی منصفانه (تومان)");
        b.Property(x => x.Summary).HasMaxLength(600).HasComment("خلاصه‌ی متنی هوش مصنوعی");

        // ریسک‌ها به‌صورت JSON در یک ستون.
        var risksConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<IReadOnlyList<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        var risksComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, c) => (a ?? new List<string>()).SequenceEqual(c ?? new List<string>()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());
        b.Property(x => x.Risks).HasConversion(risksConverter).Metadata.SetValueComparer(risksComparer);
        b.Property(x => x.Risks).HasColumnType("nvarchar(max)").HasComment("ریسک‌ها و نکته‌های پیش از خرید (JSON)");

        // سنجه‌های امتیاز به‌صورت جدول فرزند.
        b.OwnsMany(x => x.Factors, f =>
        {
            f.ToTable("HuntAnalysisFactors"); // توضیح جدول در اسکریپت T-SQL
            f.WithOwner().HasForeignKey("HuntId");
            f.Property<int>("Id").HasComment("کلید اصلی");
            f.HasKey("Id");
            f.Property(x => x.Name).HasMaxLength(40).HasComment("نام سنجه");
            f.Property(x => x.Rating).HasMaxLength(20).HasComment("برچسب کیفی، مثل «عالی»");
            f.Property(x => x.Detail).HasMaxLength(200).HasComment("توضیح سنجه");
        });
    }
}
