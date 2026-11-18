using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shekar.Domain.Entities;

namespace Shekar.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.ToTable("Cities").HasComment("شهرهای تحت پوشش سرویس");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(60).IsRequired().HasComment("نام نمایشی شهر، مثل «تهران»");
        b.Property(x => x.Slug).HasMaxLength(60).IsRequired().HasComment("اسلاگ انگلیسی برای مسیرها، مثل «tehran»");
        b.Property(x => x.IsActive).HasComment("آیا شهر فعال است یا «به‌زودی»");
        b.Property(x => x.Capacity).HasComment("ظرفیت اشتراک فعال شهر (پیش‌فرض ۱۰۰ طبق قانون R1)");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasData(SeedData.Cities());
    }
}

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plans").HasComment("پلن‌های اشتراک (فقط پرداخت ماهانه — R2)");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<byte>().HasComment("نوع پلن: 1=شکارچی، 2=شکارچی حرفه‌ای");
        b.Property(x => x.Name).HasMaxLength(60).IsRequired().HasComment("نام نمایشی پلن");
        b.Property(x => x.MonthlyPriceToman).HasComment("قیمت ماهانه به تومان");
        b.Ignore(x => x.Features);      // ویژگی‌های نمایشی؛ در PlanFeatures نگهداری می‌شود (اختیاری) — فعلاً ثابت در کد
        b.Ignore(x => x.HasBellAlerts); // مشتق از PlanLimit.AllowBellAlerts
        b.HasIndex(x => x.Type).IsUnique();
        b.HasData(SeedData.Plans());
    }
}

public sealed class PlanLimitConfiguration : IEntityTypeConfiguration<PlanLimit>
{
    public void Configure(EntityTypeBuilder<PlanLimit> b)
    {
        b.ToTable("PlanLimits").HasComment("محدودیت‌ها و امکانات هر پلن (داده‌محور کردن قوانین پلن)");
        b.HasKey(x => x.PlanId);
        b.Property(x => x.PlanId).ValueGeneratedNever().HasComment("کلید اصلی و کلید خارجی به Plans");
        b.Property(x => x.UnlimitedAppraisals).HasComment("آیا کارشناسی نامحدود است (مشترک‌ها)");
        b.Property(x => x.MonthlyAppraisalLimit).HasComment("سقف کارشناسی ماهانه وقتی نامحدود نیست؛ NULL=نامحدود");
        b.Property(x => x.AllowBellAlerts).HasComment("R6 — فعال‌بودن «خبرم کن» (فقط پلن حرفه‌ای)");
        b.Property(x => x.BellAlertDurationHours).HasComment("مدت فعال‌بودن هر زنگوله به ساعت (۲۴)");
        b.Property(x => x.MaxActiveBells).HasComment("بیشینه‌ی زنگوله‌های فعال هم‌زمان؛ NULL=نامحدود");
        b.Property(x => x.TrialAllowed).HasComment("R3 — داشتن اشتراک آزمایشی رایگان");
        b.Property(x => x.TrialHours).HasComment("مدت اشتراک آزمایشی به ساعت (۲۴)");
        b.Property(x => x.RealtimeHunts).HasComment("نمایش شکارها در لحظه");
        b.Property(x => x.PrioritySupport).HasComment("پشتیبانی اولویت‌دار");
        b.HasOne(x => x.Plan).WithOne().HasForeignKey<PlanLimit>(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasData(SeedData.PlanLimits());
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users").HasComment("کاربران ثبت‌نام‌شده (ورود با موبایل + OTP)");
        b.HasKey(x => x.Id);
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired().HasComment("شماره موبایل؛ شناسه‌ی یکتای ورود");
        b.Property(x => x.FirstName).HasMaxLength(50).HasComment("نام");
        b.Property(x => x.LastName).HasMaxLength(50).HasComment("نام خانوادگی");
        b.Property(x => x.CityId).HasComment("شهر انتخاب‌شده در پروفایل");
        b.Property(x => x.CreatedAt).HasComment("زمان ثبت‌نام");
        b.Property(x => x.HasUsedTrial).HasComment("R3 — آیا پیش‌تر از ۲۴ ساعت رایگان استفاده کرده");
        b.Property(x => x.ProfileCompleted).HasComment("آیا پروفایل (ورود اول) تکمیل شده");
        b.Ignore(x => x.FullName);
        b.HasIndex(x => x.Mobile).IsUnique();
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions").HasComment("اشتراک‌های کاربران؛ Trial+Active در ظرفیت شهر شمرده می‌شوند (R1)");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasComment("کاربر صاحب اشتراک");
        b.Property(x => x.CityId).HasComment("شهر اشتراک");
        b.Property(x => x.PlanType).HasConversion<byte>().HasComment("نوع پلن");
        b.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت: 1=Trial، 2=Active، 3=Expired");
        b.Property(x => x.StartsAt).HasComment("شروع اشتراک");
        b.Property(x => x.ExpiresAt).HasComment("پایان اعتبار (Trial: +۲۴ ساعت، Active: ماهانه)");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        // ایندکس ظرفیت شهر: شمارش اشتراک‌های فعال هر شهر.
        b.HasIndex(x => new { x.CityId, x.Status, x.ExpiresAt });
        b.HasIndex(x => new { x.UserId, x.ExpiresAt });
    }
}
