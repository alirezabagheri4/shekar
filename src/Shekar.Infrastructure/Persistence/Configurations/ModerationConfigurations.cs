using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shekar.Domain.Entities;

namespace Shekar.Infrastructure.Persistence.Configurations;

public sealed class AdvertiserConfiguration : IEntityTypeConfiguration<Advertiser>
{
    public void Configure(EntityTypeBuilder<Advertiser> b)
    {
        b.ToTable("Advertisers").HasComment("آگهی‌دهنده‌ها و حجم آگهی‌گذاری‌شان؛ مبنای تشخیص دلال/اسپمر");
        b.HasKey(x => x.Id);
        b.Property(x => x.Phone).HasMaxLength(15).IsRequired().HasComment("شماره موبایل آگهی‌دهنده؛ یکتا");
        b.Property(x => x.DisplayName).HasMaxLength(80).HasComment("نام/مستعار آگهی‌دهنده");
        b.Property(x => x.FirstSeenAt).HasComment("اولین آگهی دیده‌شده از این شماره");
        b.Property(x => x.LastAdAt).HasComment("آخرین آگهی ثبت‌شده");
        b.Property(x => x.TotalAdsPosted).HasComment("مجموع کل آگهی‌های این شماره");
        b.Property(x => x.AdsPostedLast30Days).HasComment("تعداد آگهی در ۳۰ روز اخیر (تشخیص آگهی بیش از حد)");
        b.Property(x => x.IsTrusted).HasComment("آگهی‌دهنده‌ی معتبر (از فیلترها مستثنا)");
        b.HasIndex(x => x.Phone).IsUnique();
    }
}

public sealed class BlockedAdvertiserConfiguration : IEntityTypeConfiguration<BlockedAdvertiser>
{
    public void Configure(EntityTypeBuilder<BlockedAdvertiser> b)
    {
        b.ToTable("BlockedAdvertisers").HasComment("شماره‌های بلاک‌شده؛ آگهی‌هایشان از پیشنهادهای روزانه حذف می‌شود (ضد دلال/اسپم/دروغ)");
        b.HasKey(x => x.Id);
        b.Property(x => x.Phone).HasMaxLength(15).IsRequired().HasComment("شماره موبایل بلاک‌شده");
        b.Property(x => x.Reason).HasConversion<byte>().HasComment("دلیل: 1=آگهی‌زیاد،2=دلال،3=اطلاعات‌نادرست،4=تکراری،5=گزارش‌کاربر،6=دستی");
        b.Property(x => x.Note).HasMaxLength(300).HasComment("توضیح تکمیلی");
        b.Property(x => x.BlockedAt).HasComment("زمان بلاک");
        b.Property(x => x.BlockedByUserId).HasComment("ادمین بلاک‌کننده؛ NULL=سیستمی/خودکار");
        b.Property(x => x.ExpiresAt).HasComment("انقضای بلاک؛ NULL=دائمی");
        b.Property(x => x.IsActive).HasComment("آیا بلاک هم‌اکنون فعال است");
        // ایندکس تشخیص سریع بلاک فعالِ یک شماره.
        b.HasIndex(x => new { x.Phone, x.IsActive });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments").HasComment("پرداخت‌های کارت‌به‌کارت؛ اشتراک فوری فعال و منتظر تأیید/رد ادمین");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasComment("کاربر پرداخت‌کننده");
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired().HasComment("شماره موبایل کاربر در لحظه‌ی پرداخت (مبنای بلاک)");
        b.Property(x => x.PlanType).HasConversion<byte>().HasComment("پلن خریداری‌شده: 1=شکارچی،2=حرفه‌ای");
        b.Property(x => x.CityId).HasComment("شهر اشتراک (مبنای ظرفیت R1)");
        b.Property(x => x.AmountToman).HasComment("مبلغ واریزی به تومان");
        b.Property(x => x.Method).HasConversion<byte>().HasComment("روش رسید: 1=عکس،2=متن");
        b.Property(x => x.ReceiptImageKey).HasMaxLength(200).HasComment("کلید فایل رسید (خارج از wwwroot عمومی)");
        b.Property(x => x.ReceiptText).HasMaxLength(2000).HasComment("متن رسید (روش متن)");
        b.Property(x => x.Reference).HasMaxLength(32).IsRequired().HasComment("کد پیگیری نمایشی، مثل SHK-482193");
        b.Property(x => x.Ip).HasMaxLength(64).HasComment("IP ثبت‌کننده‌ی پرداخت");
        b.Property(x => x.SubmittedAt).HasComment("زمان ثبت پرداخت");
        b.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت بررسی: 1=در انتظار،2=تأیید،3=رد");
        b.Property(x => x.SubscriptionId).HasComment("اشتراک فعال/تمدیدشده با این پرداخت (برای غیرفعال‌سازی هنگام رد)");
        b.Property(x => x.ReviewedAt).HasComment("زمان بررسی ادمین");
        b.Property(x => x.ReviewedByUserId).HasComment("ادمین بررسی‌کننده");
        b.Property(x => x.ReviewNote).HasMaxLength(300).HasComment("توضیح بررسی (مثلاً دلیل رد)");
        // واکشی سریع پرداخت در انتظار هر کاربر و فهرست در انتظار بررسی.
        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => x.Status);
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.ToTable("SystemSettings").HasComment("محدودیت‌ها و تنظیمات سراسری کسب‌وکار (کلید/مقدار، قابل تغییر بدون انتشار)");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(80).HasComment("کلید یکتا، مثل «GoldenScoreThreshold»");
        b.Property(x => x.Value).HasMaxLength(200).IsRequired().HasComment("مقدار (طبق ValueType تفسیر می‌شود)");
        b.Property(x => x.ValueType).HasMaxLength(20).HasComment("نوع مقدار: int|decimal|bool|time|string");
        b.Property(x => x.Description).HasMaxLength(300).HasComment("توضیح کاربردی تنظیم");
        b.Property(x => x.UpdatedAt).HasComment("زمان آخرین تغییر");
    }
}
