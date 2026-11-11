# چک‌لیستِ راه‌اندازیِ موتورِ تشخیص (Go-Live)

راهنمای رفتن از حالتِ توسعه به تولیدِ واقعی برای خط لولهٔ **کراول → نرمال‌سازی/اگریگیشن → اسکورینگ**.
هر بند را به‌ترتیب رد کن.

---

## ۱) داده و پایداری
- [ ] `Persistence` را در **هر دو** `Shekar.Web/appsettings` و `Shekar.Worker/appsettings` روی `"SqlServer"` بگذار
      (در حالت `Mock` داده درون‌حافظه است و بین Worker و Web **به‌اشتراک گذاشته نمی‌شود**).
- [ ] رشتهٔ اتصال `ConnectionStrings:Shekar` را به دیتابیسِ تولید اشاره بده.
- [ ] مهاجرت‌ها را اعمال کن:
      `dotnet ef database update -p src/Shekar.Infrastructure -s src/Shekar.Web`
      (شاملِ `AddModelIdentity`: ستون‌های `DivarKey` و `SourceModelKey` و ایندکسِ یکتا).
- [ ] شهرهای فعال و پلن‌ها در دیتابیس seed شده باشند.

## ۲) کراولِ واقعیِ دیوار
- [ ] `Crawl:Provider = "Divar"` (نه `"Mock"`).
- [ ] `Crawl:CitySlugs` نگاشتِ درستِ شهر→اسلاگ داشته باشد (مثلاً `1 → tehran`).
- [ ] تنظیماتِ مؤدب (انسان‌مانند) را محافظه‌کارانه نگه دار — **قرار نیست به دیوار فشار بیاوریم**:
  - `MinDelaySeconds`/`MaxDelaySeconds` (پیش‌فرض ۵..۱۶) و `LongPauseEvery…`.
  - `MaxPagesPerRun` (۲)، `MaxDetailFetchesPerRun` (۲۰)، `MaxRequestsPerRun` (۳۰).
  - `Crawl:IntervalMinutes` + `Crawl:StartJitterSeconds` (زمان‌بندیِ نامنظم).
- [ ] `Crawl:MaxAgeHours` (۲۴) و `Crawl:StaleAfterHours` (۷۲) متناسب با نرخِ تازه‌شدنِ بازار.
- [ ] یک پاسِ دستی بگیر و لاگِ `دیوار/{slug}: {Count} آگهی لیست، {Enriched} فچِ جزئیات، {WithYear} سال‌دار` را ببین.
      اگر `Count=0` بود → ساختارِ صفحه عوض شده یا بلاک شده‌ایم (به بند ۶ برو).

## ۳) نرمال‌سازی و پوششِ همهٔ مدل‌ها
- [ ] رجیستر خودکارِ مدل بر پایهٔ `brand_model` فعال است (هویتِ قطعی = `DivarKey`) تا **همهٔ مدل‌ها** پوشش داده شوند.
- [ ] لاگِ اگریگیشن را ببین: `نرمال‌سازی {Matched} (رجیستر خودکار {Auto})، بی‌تطبیق {Unmatched}`.
- [ ] نمونه‌های `بی‌تطبیق` را مرور کن؛ اگر زیاد بود، matcher/نرمال‌سازی را بازبینی کن.

## ۴) اسکورینگ و محافظ‌ها
- [ ] آستانه‌ها در `Shekar.Worker/appsettings` تنظیم شده‌اند:
  - `Score:MinBelowMarketPercent` (۵)، `Score:MinScore` (۶۰)، `Score:MinSamples` (۳).
  - `Score:SuspiciousBelowMarketPercent` (۴۰) — محافظِ کلاه‌برداری/پرت.
  - `Score:ConfidenceFullAtSamples` (۵).
- [ ] با داده‌ی واقعی چند پاس اسکور بزن و لاگ را ببین:
      `واجد {Qualified}، ساخت {Created}، مشکوک {Suspicious}، تکراری {Duplicates}`.
- [ ] نمونهٔ شکارهای «طلایی» را دستی بررسی کن که واقعاً فرصت‌اند (نه اقساطی/جعلی/تایپی).

## ۵) پایش و هشدار
- [ ] صفحهٔ **`/admin/status`** را با یک کاربرِ ادمین باز کن (شماره در `Admin:Mobiles`).
      باید وضعیتِ کلی، شمارش‌ها (آگهی فعال/نرمال‌شده/سگمنت/شکار)، و پاس‌های اخیرِ کراول را نشان دهد.
- [ ] آستانه‌های `Health:*` را در `Shekar.Web/appsettings` بازبینی کن (`StaleCrawlHours`, `MinMatchRatePercent`, …).
- [ ] هشدارهای کلیدی را بشناس:
  - **بحرانی**: هیچ کراولی ثبت نشده / کراول متوقف / آخرین پاس خطا / **صفر آگهی**.
  - **هشدار**: نرخ تطبیق پایین / آگهیِ نرمال‌شده هست ولی سگمنت نیست.
- [ ] لاگِ Worker را به یک مقصدِ ماندگار بفرست (فایل/Seq/…) تا هشدارها گم نشوند.

## ۶) مقاومت در برابرِ تغییرِ سایت (شکنندگی)
- [ ] تستِ رگرسیونِ پارسر سبز است:
      `dotnet test tests/Shekar.Infrastructure.Tests` (پارسِ لیست + غنی‌سازیِ جزئیات روی fixtureهای واقعی‌شکل).
- [ ] **مهم:** fixtureهای `tests/Shekar.Infrastructure.Tests/Fixtures/divar-*.html` هم‌شکلِ ساختارِ واقعیِ دیوار هستند
      ولی باید **هرازگاهی از یک کپچرِ تازهٔ HTML نو شوند** (به‌خصوص اگر `/admin/status` صفر آگهی نشان داد).
- [ ] اگر دیوار ساختارش را عوض کرد: لاگِ `بلاک __PRELOADED_STATE__ … پیدا نشد` را می‌بینی →
      پارسر (`DivarHtmlProvider`) را به‌روز و fixtureها را نو کن.

## ۷) پیش از انتشار
- [ ] `dotnet build Shekar.sln` بدونِ خطا/اخطار.
- [ ] `dotnet test Shekar.sln` همه سبز.
- [ ] پنلِ ادمین (`/admin/payments`, `/admin/status`) فقط برای `Admin:Mobiles` باز می‌شود (بقیه ۴۰۴).
- [ ] تعدادِ اشتراکِ فروخته‌شده محدود بماند (فشارِ کم روی دیوار؛ سیاستِ توافق‌شده).

## بازگشت (Rollback)
- اگر کراول مشکل‌ساز شد: `Crawl:Provider = "Mock"` → خط لوله روی داده‌ی ساختگی کار می‌کند، بی‌هیچ درخواستی به دیوار.
- آستانه‌های اسکورینگ همگی از `appsettings` قابل‌تنظیم‌اند؛ برای سخت‌گیریِ موقت `MinScore`/`MinSamples` را بالا ببر.
