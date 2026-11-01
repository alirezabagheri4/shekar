/* =====================================================================================
   شکار (Shekar) — اسکریپت ساخت پایگاه‌داده (SQL Server / T-SQL)
   هماهنگ با مدل EF Core 6 در پروژه‌ی Shekar.Infrastructure.
   هر ستون کامنت کاربردی دارد که بیان می‌کند برای چیست.
   اعداد پولی بر حسب «تومان» و به‌صورت BIGINT نگهداری می‌شوند.
   زمان‌ها datetimeoffset هستند (آگاه به منطقه‌ی زمانی؛ محاسبات تهران در لایه‌ی اپ).
   ===================================================================================== */

SET NOCOUNT ON;
GO

/* ------------------------------------------------------------------ شهرها */
CREATE TABLE dbo.Cities (
    Id        INT            NOT NULL IDENTITY(1,1),              -- شناسه‌ی شهر
    Name      NVARCHAR(60)   NOT NULL,                           -- نام نمایشی، مثل «تهران»
    Slug      NVARCHAR(60)   NOT NULL,                           -- اسلاگ انگلیسی برای مسیرها، مثل «tehran»
    IsActive  BIT            NOT NULL CONSTRAINT DF_Cities_IsActive DEFAULT(0),   -- فعال است یا «به‌زودی»
    Capacity  INT            NOT NULL CONSTRAINT DF_Cities_Capacity DEFAULT(100), -- R1 — ظرفیت اشتراک فعال شهر
    CONSTRAINT PK_Cities PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX UX_Cities_Slug ON dbo.Cities(Slug);
GO

/* ------------------------------------------------------------------ پلن‌ها (R2: فقط ماهانه) */
CREATE TABLE dbo.Plans (
    Id                 INT          NOT NULL IDENTITY(1,1),       -- شناسه‌ی پلن
    [Type]             TINYINT      NOT NULL,                     -- نوع: 1=شکارچی، 2=شکارچی حرفه‌ای
    Name               NVARCHAR(60) NOT NULL,                     -- نام نمایشی پلن
    MonthlyPriceToman  BIGINT       NOT NULL,                     -- قیمت ماهانه به تومان
    CONSTRAINT PK_Plans PRIMARY KEY (Id),
    CONSTRAINT CK_Plans_Type CHECK ([Type] IN (1,2))
);
CREATE UNIQUE INDEX UX_Plans_Type ON dbo.Plans([Type]);
GO

/* ------------------------------------------------------------------ محدودیت‌های پلن (قوانین پلن، داده‌محور) */
CREATE TABLE dbo.PlanLimits (
    PlanId                 INT     NOT NULL,                      -- کلید اصلی و کلید خارجی به Plans
    UnlimitedAppraisals    BIT     NOT NULL CONSTRAINT DF_PlanLimits_Unl DEFAULT(0),  -- کارشناسی نامحدود (مشترک‌ها)
    MonthlyAppraisalLimit  INT     NULL,                          -- سقف کارشناسی ماهانه وقتی نامحدود نیست؛ NULL=نامحدود
    AllowBellAlerts        BIT     NOT NULL CONSTRAINT DF_PlanLimits_Bell DEFAULT(0), -- R6 — فعال‌بودن «خبرم کن» (فقط حرفه‌ای)
    BellAlertDurationHours INT     NOT NULL CONSTRAINT DF_PlanLimits_BellH DEFAULT(24), -- مدت فعال‌بودن هر زنگوله (ساعت)
    MaxActiveBells         INT     NULL,                          -- بیشینه‌ی زنگوله‌های فعال هم‌زمان؛ NULL=نامحدود
    TrialAllowed           BIT     NOT NULL CONSTRAINT DF_PlanLimits_Trial DEFAULT(0),-- R3 — داشتن اشتراک آزمایشی رایگان
    TrialHours             INT     NOT NULL CONSTRAINT DF_PlanLimits_TrialH DEFAULT(24), -- مدت اشتراک آزمایشی (ساعت)
    RealtimeHunts          BIT     NOT NULL CONSTRAINT DF_PlanLimits_RT DEFAULT(1),   -- نمایش شکارها در لحظه
    PrioritySupport        BIT     NOT NULL CONSTRAINT DF_PlanLimits_Prio DEFAULT(0), -- پشتیبانی اولویت‌دار
    CONSTRAINT PK_PlanLimits PRIMARY KEY (PlanId),
    CONSTRAINT FK_PlanLimits_Plans FOREIGN KEY (PlanId) REFERENCES dbo.Plans(Id) ON DELETE CASCADE
);
GO

/* ------------------------------------------------------------------ کاربران */
CREATE TABLE dbo.Users (
    Id               INT           NOT NULL IDENTITY(1,1),        -- شناسه‌ی کاربر
    Mobile           NVARCHAR(15)  NOT NULL,                      -- شماره موبایل؛ شناسه‌ی یکتای ورود
    FirstName        NVARCHAR(50)  NULL,                          -- نام
    LastName         NVARCHAR(50)  NULL,                          -- نام خانوادگی
    CityId           INT           NULL,                          -- شهر انتخاب‌شده در پروفایل
    CreatedAt        DATETIMEOFFSET NOT NULL,                     -- زمان ثبت‌نام
    HasUsedTrial     BIT           NOT NULL CONSTRAINT DF_Users_Trial DEFAULT(0),   -- R3 — آیا از ۲۴ ساعت رایگان استفاده کرده
    ProfileCompleted BIT           NOT NULL CONSTRAINT DF_Users_Profile DEFAULT(0), -- آیا پروفایل (ورود اول) تکمیل شده
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT FK_Users_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities(Id) ON DELETE SET NULL
);
CREATE UNIQUE INDEX UX_Users_Mobile ON dbo.Users(Mobile);
GO

/* ------------------------------------------------------------------ اشتراک‌ها (R1/R2/R3) */
CREATE TABLE dbo.Subscriptions (
    Id        INT            NOT NULL IDENTITY(1,1),              -- شناسه‌ی اشتراک
    UserId    INT            NOT NULL,                            -- کاربر صاحب اشتراک
    CityId    INT            NOT NULL,                            -- شهر اشتراک (مبنای ظرفیت)
    PlanType  TINYINT        NOT NULL,                            -- نوع پلن: 1/2
    [Status]  TINYINT        NOT NULL,                            -- وضعیت: 1=Trial، 2=Active، 3=Expired (Trial+Active در ظرفیت)
    StartsAt  DATETIMEOFFSET NOT NULL,                            -- شروع اشتراک
    ExpiresAt DATETIMEOFFSET NOT NULL,                            -- پایان اعتبار (Trial: +۲۴ساعت، Active: ماهانه)
    CONSTRAINT PK_Subscriptions PRIMARY KEY (Id),
    CONSTRAINT FK_Subscriptions_Users  FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)  ON DELETE CASCADE,
    CONSTRAINT FK_Subscriptions_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Subscriptions_Status CHECK ([Status] IN (1,2,3))
);
-- شمارش سریع اشتراک فعال هر شهر (ظرفیت R1):
CREATE INDEX IX_Subscriptions_City_Status_Exp ON dbo.Subscriptions(CityId, [Status], ExpiresAt);
CREATE INDEX IX_Subscriptions_User_Exp        ON dbo.Subscriptions(UserId, ExpiresAt);
GO

/* ------------------------------------------------------------------ شکارها (پیشنهادهای روزانه) */
CREATE TABLE dbo.Hunts (
    Id                 INT            NOT NULL IDENTITY(1,1),      -- شناسه‌ی شکار
    Title              NVARCHAR(200)  NOT NULL,                   -- عنوان آگهی
    ModelYear          NVARCHAR(10)   NULL,                       -- سال مدل (شمسی/میلادی)
    MileageKm          INT            NULL,                       -- کارکرد (کیلومتر)؛ NULL=صفر
    Location           NVARCHAR(80)   NULL,                       -- محله/منطقه‌ی آگهی
    CityId             INT            NOT NULL,                   -- شهر آگهی
    PriceToman         BIGINT         NOT NULL,                   -- قیمت آگهی (تومان)
    MarketMedianToman  BIGINT         NOT NULL,                   -- میانه‌ی قیمت بازار (تومان) — R9
    BelowMarketPercent INT            NOT NULL,                   -- درصد زیر بازار
    Score              INT            NOT NULL,                   -- امتیاز ۰..۱۰۰ (≥۸۵ فرصت طلایی — R8)
    Liquidity          TINYINT        NOT NULL,                   -- نقدشوندگی: 1=متوسط، 2=بالا
    [State]            TINYINT        NOT NULL,                   -- وضعیت: 1=فعال، 2=حذف‌شده (R7)
    PublishedAt        DATETIMEOFFSET NOT NULL,                   -- زمان انتشار آگهی
    IsNew              BIT            NOT NULL CONSTRAINT DF_Hunts_New DEFAULT(0), -- برچسب «تازه»
    ImageFileName      NVARCHAR(120)  NULL,                       -- نام فایل تصویر؛ NULL=placeholder
    AdvertiserPhone    NVARCHAR(15)   NULL,                       -- شماره آگهی‌دهنده؛ برای حذف شماره‌های بلاک‌شده
    CONSTRAINT PK_Hunts PRIMARY KEY (Id),
    CONSTRAINT FK_Hunts_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Hunts_Score CHECK (Score BETWEEN 0 AND 100),
    CONSTRAINT CK_Hunts_Liquidity CHECK (Liquidity IN (1,2)),
    CONSTRAINT CK_Hunts_State CHECK ([State] IN (1,2))
);
-- لیست روزانه‌ی هر شهر + مرتب‌سازی‌ها + پیوست بلاک:
CREATE INDEX IX_Hunts_City_Pub_State ON dbo.Hunts(CityId, PublishedAt, [State]);
CREATE INDEX IX_Hunts_Score          ON dbo.Hunts(Score);
CREATE INDEX IX_Hunts_Advertiser     ON dbo.Hunts(AdvertiserPhone);
GO

/* ------------------------------------------------------------------ تحلیل جزئیات شکار (۱:۱) */
CREATE TABLE dbo.HuntAnalyses (
    HuntId            INT            NOT NULL,                    -- کلید اصلی و کلید خارجی به Hunts
    ComparableCount   INT            NOT NULL CONSTRAINT DF_HA_Cmp DEFAULT(0), -- تعداد آگهی مشابه مبنای تحلیل
    Confidence        NVARCHAR(20)   NULL,                       -- سطح اطمینان، مثل «بالا»
    FairRangeMinToman BIGINT         NOT NULL CONSTRAINT DF_HA_Min DEFAULT(0), -- کف بازه‌ی منصفانه (تومان)
    FairRangeMaxToman BIGINT         NOT NULL CONSTRAINT DF_HA_Max DEFAULT(0), -- سقف بازه‌ی منصفانه (تومان)
    Summary           NVARCHAR(600)  NULL,                       -- خلاصه‌ی متنی هوش مصنوعی
    Risks             NVARCHAR(MAX)  NULL,                       -- ریسک‌ها و نکته‌های پیش از خرید (آرایه‌ی JSON)
    CONSTRAINT PK_HuntAnalyses PRIMARY KEY (HuntId),
    CONSTRAINT FK_HuntAnalyses_Hunts FOREIGN KEY (HuntId) REFERENCES dbo.Hunts(Id) ON DELETE CASCADE
);
GO

/* ------------------------------------------------------------------ سنجه‌های امتیاز (فرزند تحلیل) */
CREATE TABLE dbo.HuntAnalysisFactors (
    Id      INT           NOT NULL IDENTITY(1,1),                 -- شناسه‌ی سنجه
    HuntId  INT           NOT NULL,                               -- کلید خارجی به HuntAnalyses
    Name    NVARCHAR(40)  NULL,                                   -- نام سنجه (قیمت/نقدشوندگی/کارکرد/وضعیت فنی/ریسک)
    Rating  NVARCHAR(20)  NULL,                                   -- برچسب کیفی، مثل «عالی»
    Detail  NVARCHAR(200) NULL,                                   -- توضیح سنجه
    CONSTRAINT PK_HuntAnalysisFactors PRIMARY KEY (Id),
    CONSTRAINT FK_HAF_HuntAnalyses FOREIGN KEY (HuntId) REFERENCES dbo.HuntAnalyses(HuntId) ON DELETE CASCADE
);
CREATE INDEX IX_HAF_HuntId ON dbo.HuntAnalysisFactors(HuntId);
GO

/* ------------------------------------------------------------------ کارشناسی‌ها (R5) */
CREATE TABLE dbo.Appraisals (
    Id        INT            NOT NULL IDENTITY(1,1),              -- شناسه‌ی کارشناسی
    UserId    INT            NOT NULL,                            -- کاربر انجام‌دهنده
    AdUrl     NVARCHAR(500)  NOT NULL,                            -- لینک آگهی کارشناسی‌شده
    Title     NVARCHAR(200)  NULL,                                -- عنوان خودرو
    Score     INT            NOT NULL,                            -- امتیاز نتیجه ۰..۱۰۰
    Verdict   NVARCHAR(40)   NULL,                                -- برچسب: «فرصت طلایی»/«منصفانه»/«گران»
    CreatedAt DATETIMEOFFSET NOT NULL,                            -- زمان انجام (مبنای سهمیه‌ی ماهانه)
    CONSTRAINT PK_Appraisals PRIMARY KEY (Id),
    CONSTRAINT FK_Appraisals_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Appraisals_Score CHECK (Score BETWEEN 0 AND 100)
);
-- شمارش سهمیه‌ی ماهانه‌ی هر کاربر:
CREATE INDEX IX_Appraisals_User_Created ON dbo.Appraisals(UserId, CreatedAt);
GO

/* ------------------------------------------------------------------ زنگوله‌های «خبرم کن» (R6) */
CREATE TABLE dbo.BellAlerts (
    Id          INT            NOT NULL IDENTITY(1,1),            -- شناسه‌ی زنگوله
    UserId      INT            NOT NULL,                          -- کاربر صاحب زنگوله
    Category    TINYINT        NOT NULL,                          -- دسته‌ی قیمتی ۱..۸
    ActivatedAt DATETIMEOFFSET NULL,                              -- زمان روشن‌شدن؛ NULL=خاموش
    ExpiresAt   DATETIMEOFFSET NULL,                              -- خاموشی خودکار (ActivatedAt + ۲۴ ساعت)
    CONSTRAINT PK_BellAlerts PRIMARY KEY (Id),
    CONSTRAINT FK_BellAlerts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_BellAlerts_Category CHECK (Category BETWEEN 1 AND 8)
);
CREATE UNIQUE INDEX UX_BellAlerts_User_Category ON dbo.BellAlerts(UserId, Category);
GO

/* ------------------------------------------------------------------ لیست انتظار (R1) */
CREATE TABLE dbo.WaitlistEntries (
    Id        INT            NOT NULL IDENTITY(1,1),              -- شناسه‌ی ردیف صف
    CityId    INT            NOT NULL,                            -- شهر لیست انتظار
    Mobile    NVARCHAR(15)   NOT NULL,                            -- شماره موبایل متقاضی
    CreatedAt DATETIMEOFFSET NOT NULL,                            -- زمان ثبت در صف
    Position  INT            NOT NULL,                            -- نوبت نمایش‌داده‌شده در صف
    CONSTRAINT PK_WaitlistEntries PRIMARY KEY (Id),
    CONSTRAINT FK_Waitlist_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX UX_Waitlist_City_Mobile ON dbo.WaitlistEntries(CityId, Mobile);
GO

/* ------------------------------------------------------------------ کدهای یک‌بارمصرف ورود */
CREATE TABLE dbo.OtpCodes (
    Id        INT            NOT NULL IDENTITY(1,1),              -- شناسه‌ی کد
    Mobile    NVARCHAR(15)   NOT NULL,                            -- شماره موبایل مقصد
    Code      NVARCHAR(10)   NOT NULL,                            -- کد ۵ رقمی (در Mock ثابت ۱۲۳۴۵)
    CreatedAt DATETIMEOFFSET NOT NULL,                            -- زمان تولید
    ExpiresAt DATETIMEOFFSET NOT NULL,                            -- زمان انقضا
    Consumed  BIT            NOT NULL CONSTRAINT DF_Otp_Consumed DEFAULT(0), -- آیا استفاده/تأیید شده
    CONSTRAINT PK_OtpCodes PRIMARY KEY (Id)
);
CREATE INDEX IX_OtpCodes_Mobile_Created ON dbo.OtpCodes(Mobile, CreatedAt);
GO

/* ------------------------------------------------------------------ آگهی‌دهنده‌ها (حجم آگهی‌گذاری) */
CREATE TABLE dbo.Advertisers (
    Id                 INT            NOT NULL IDENTITY(1,1),      -- شناسه
    Phone              NVARCHAR(15)   NOT NULL,                   -- شماره موبایل آگهی‌دهنده؛ یکتا
    DisplayName        NVARCHAR(80)   NULL,                       -- نام/مستعار
    FirstSeenAt        DATETIMEOFFSET NOT NULL,                   -- اولین آگهی دیده‌شده
    LastAdAt           DATETIMEOFFSET NULL,                       -- آخرین آگهی ثبت‌شده
    TotalAdsPosted     INT            NOT NULL CONSTRAINT DF_Adv_Total DEFAULT(0), -- مجموع آگهی‌های این شماره
    AdsPostedLast30Days INT           NOT NULL CONSTRAINT DF_Adv_30 DEFAULT(0),    -- آگهی در ۳۰ روز اخیر (تشخیص آگهی زیاد)
    IsTrusted          BIT            NOT NULL CONSTRAINT DF_Adv_Trusted DEFAULT(0), -- معتبر (از فیلترها مستثنا)
    CONSTRAINT PK_Advertisers PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX UX_Advertisers_Phone ON dbo.Advertisers(Phone);
GO

/* ------------------------------------------------------------------ شماره‌های بلاک‌شده (ضد دلال/اسپم/دروغ)
   آگهی‌های این شماره‌ها از لیست پیشنهادهای روزانه (شکارها) حذف می‌شوند. */
CREATE TABLE dbo.BlockedAdvertisers (
    Id              INT            NOT NULL IDENTITY(1,1),         -- شناسه
    Phone           NVARCHAR(15)   NOT NULL,                      -- شماره موبایل بلاک‌شده
    Reason          TINYINT        NOT NULL,                      -- دلیل: 1=آگهی‌زیاد،2=دلال،3=اطلاعات‌نادرست،4=تکراری،5=گزارش‌کاربر،6=دستی
    Note            NVARCHAR(300)  NULL,                          -- توضیح، مثل «۴۷ آگهی در ۳۰ روز»
    BlockedAt       DATETIMEOFFSET NOT NULL,                      -- زمان بلاک
    BlockedByUserId INT            NULL,                          -- ادمین بلاک‌کننده؛ NULL=سیستمی/خودکار
    ExpiresAt       DATETIMEOFFSET NULL,                          -- انقضای بلاک؛ NULL=دائمی
    IsActive        BIT            NOT NULL CONSTRAINT DF_Blocked_Active DEFAULT(1), -- آیا بلاک فعال است
    CONSTRAINT PK_BlockedAdvertisers PRIMARY KEY (Id),
    CONSTRAINT FK_Blocked_Users FOREIGN KEY (BlockedByUserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Blocked_Reason CHECK (Reason BETWEEN 1 AND 6)
);
-- تشخیص سریع بلاک فعالِ یک شماره (هنگام ساخت لیست روزانه):
CREATE INDEX IX_Blocked_Phone_Active ON dbo.BlockedAdvertisers(Phone, IsActive);
GO

/* ------------------------------------------------------------------ تنظیمات/محدودیت‌های سراسری (کلید/مقدار) */
CREATE TABLE dbo.SystemSettings (
    [Key]       NVARCHAR(80)   NOT NULL,                          -- کلید یکتا، مثل «GoldenScoreThreshold»
    [Value]     NVARCHAR(200)  NOT NULL,                          -- مقدار (طبق ValueType تفسیر می‌شود)
    ValueType   NVARCHAR(20)   NOT NULL CONSTRAINT DF_SS_Type DEFAULT('string'), -- نوع: int|decimal|bool|time|string
    [Description] NVARCHAR(300) NULL,                             -- توضیح کاربردی تنظیم
    UpdatedAt   DATETIMEOFFSET NOT NULL,                          -- زمان آخرین تغییر
    CONSTRAINT PK_SystemSettings PRIMARY KEY ([Key])
);
GO

/* =====================================================================================
   داده‌ی اولیه (Seed)
   ===================================================================================== */
INSERT INTO dbo.Cities (Name, Slug, IsActive, Capacity) VALUES
    (N'تهران',  N'tehran',  1, 100),
    (N'کرج',    N'karaj',   0, 100),
    (N'اصفهان', N'isfahan', 0, 100),
    (N'مشهد',   N'mashhad', 0, 100);
GO

SET IDENTITY_INSERT dbo.Plans ON;
INSERT INTO dbo.Plans (Id, [Type], Name, MonthlyPriceToman) VALUES
    (1, 1, N'شکارچی',          990000),
    (2, 2, N'شکارچی حرفه‌ای', 1499000);
SET IDENTITY_INSERT dbo.Plans OFF;
GO

INSERT INTO dbo.PlanLimits
    (PlanId, UnlimitedAppraisals, MonthlyAppraisalLimit, AllowBellAlerts, BellAlertDurationHours, MaxActiveBells, TrialAllowed, TrialHours, RealtimeHunts, PrioritySupport)
VALUES
    (1, 1, NULL, 0, 24, 0,    1, 24, 1, 0),   -- شکارچی: ۲۴ ساعت رایگان دارد، بدون «خبرم کن»
    (2, 1, NULL, 1, 24, NULL, 0, 24, 1, 1);   -- حرفه‌ای: «خبرم کن» + اولویت پشتیبانی، بدون ۲۴ ساعت رایگان
GO

INSERT INTO dbo.SystemSettings ([Key], [Value], ValueType, [Description], UpdatedAt) VALUES
    (N'CityCapacity',               N'100',      N'int',     N'R1 — ظرفیت پیش‌فرض اشتراک فعال هر شهر',               SYSDATETIMEOFFSET()),
    (N'FreeAppraisalsPerMonthGuest',N'3',        N'int',     N'R5 — سهمیه‌ی کارشناسی رایگان ماهانه برای کاربر بدون اشتراک', SYSDATETIMEOFFSET()),
    (N'TrialHours',                 N'24',       N'int',     N'R3 — مدت اشتراک آزمایشی رایگان (ساعت)',                SYSDATETIMEOFFSET()),
    (N'BellAlertHours',             N'24',       N'int',     N'R6 — مدت فعال‌بودن زنگوله پیش از خاموشی خودکار (ساعت)', SYSDATETIMEOFFSET()),
    (N'GoldenScoreThreshold',       N'85',       N'int',     N'R8 — حداقل امتیاز برای «فرصت طلایی»',                  SYSDATETIMEOFFSET()),
    (N'TransactionCostFloorToman',  N'15000000', N'int',     N'R9 — کف هزینه‌ی نقل‌وانتقال و فروش (تومان)',           SYSDATETIMEOFFSET()),
    (N'TransactionCostRate',        N'0.02',     N'decimal', N'R9 — نرخ هزینه‌ی نقل‌وانتقال نسبت به قیمت آگهی',       SYSDATETIMEOFFSET()),
    (N'SupportStartHour',           N'9',        N'int',     N'R12 — ساعت شروع پشتیبانی (Asia/Tehran)',               SYSDATETIMEOFFSET()),
    (N'SupportEndHour',             N'18',       N'int',     N'R12 — ساعت پایان پشتیبانی (Asia/Tehran)',              SYSDATETIMEOFFSET()),
    (N'MaxAdsPerMonthBeforeReview', N'20',       N'int',     N'آستانه‌ی تعداد آگهی ماهانه برای بررسی دلال/اسپم',      SYSDATETIMEOFFSET());
GO

/* =====================================================================================
   توضیحات جدول‌ها (MS_Description) — علاوه بر کامنت‌های ستونیِ بالا.
   توضیحات ستون‌ها در صورت ساخت دیتابیس با EF Migrations از HasComment نیز اعمال می‌شود.
   ===================================================================================== */
EXEC sys.sp_addextendedproperty 'MS_Description', N'شهرهای تحت پوشش سرویس',                                         'SCHEMA','dbo','TABLE','Cities';
EXEC sys.sp_addextendedproperty 'MS_Description', N'پلن‌های اشتراک (فقط پرداخت ماهانه — R2)',                       'SCHEMA','dbo','TABLE','Plans';
EXEC sys.sp_addextendedproperty 'MS_Description', N'محدودیت‌ها و امکانات هر پلن (داده‌محور کردن قوانین پلن)',       'SCHEMA','dbo','TABLE','PlanLimits';
EXEC sys.sp_addextendedproperty 'MS_Description', N'کاربران ثبت‌نام‌شده (ورود با موبایل + OTP)',                   'SCHEMA','dbo','TABLE','Users';
EXEC sys.sp_addextendedproperty 'MS_Description', N'اشتراک‌ها؛ Trial+Active در ظرفیت شهر شمرده می‌شوند (R1)',      'SCHEMA','dbo','TABLE','Subscriptions';
EXEC sys.sp_addextendedproperty 'MS_Description', N'شکارها: آگهی‌های زیر قیمت بازار (پیشنهادهای روزانه)',          'SCHEMA','dbo','TABLE','Hunts';
EXEC sys.sp_addextendedproperty 'MS_Description', N'تحلیل تفصیلی هر شکار (۱:۱ با Hunts)',                          'SCHEMA','dbo','TABLE','HuntAnalyses';
EXEC sys.sp_addextendedproperty 'MS_Description', N'سنجه‌های امتیاز هر تحلیل',                                      'SCHEMA','dbo','TABLE','HuntAnalysisFactors';
EXEC sys.sp_addextendedproperty 'MS_Description', N'کارشناسی‌های قیمت کاربران (R5)',                               'SCHEMA','dbo','TABLE','Appraisals';
EXEC sys.sp_addextendedproperty 'MS_Description', N'زنگوله‌های «خبرم کن» برای دسته‌های قیمتی (R6)',                'SCHEMA','dbo','TABLE','BellAlerts';
EXEC sys.sp_addextendedproperty 'MS_Description', N'لیست انتظار شهرِ پرظرفیت (R1)',                                'SCHEMA','dbo','TABLE','WaitlistEntries';
EXEC sys.sp_addextendedproperty 'MS_Description', N'کدهای یک‌بارمصرف ورود',                                        'SCHEMA','dbo','TABLE','OtpCodes';
EXEC sys.sp_addextendedproperty 'MS_Description', N'آگهی‌دهنده‌ها و حجم آگهی‌گذاری‌شان',                           'SCHEMA','dbo','TABLE','Advertisers';
EXEC sys.sp_addextendedproperty 'MS_Description', N'شماره‌های بلاک‌شده؛ از پیشنهادهای روزانه حذف می‌شوند',         'SCHEMA','dbo','TABLE','BlockedAdvertisers';
EXEC sys.sp_addextendedproperty 'MS_Description', N'محدودیت‌ها و تنظیمات سراسری کسب‌وکار (کلید/مقدار)',            'SCHEMA','dbo','TABLE','SystemSettings';
GO

/* نمونه‌ی کوئری ساخت «لیست پیشنهادهای روزانه» با حذف شماره‌های بلاک‌شده:

   SELECT h.*
   FROM dbo.Hunts h
   WHERE h.CityId = @CityId
     AND h.[State] = 1                                   -- فقط فعال‌ها
     AND h.PublishedAt >= @DayStartUtc AND h.PublishedAt < @DayEndUtc
     AND NOT EXISTS (
         SELECT 1 FROM dbo.BlockedAdvertisers b
         WHERE b.Phone = h.AdvertiserPhone
           AND b.IsActive = 1
           AND (b.ExpiresAt IS NULL OR b.ExpiresAt > SYSDATETIMEOFFSET())
     )
   ORDER BY h.Score DESC;
*/


/* =====================================================================================
   پایپ‌لاین داده (کراولر → اگریگیشن → اسکورینگ) — فاز ۱: مدل داده
   ===================================================================================== */

/* ---------------------------------------------- کاتالوگ خودرو + نقدشوندگی دستی */
CREATE TABLE dbo.VehicleModels (
    Id             INT           NOT NULL IDENTITY(1,1),          -- شناسه
    Brand          NVARCHAR(60)  NOT NULL,                        -- برند، مثل «پژو»
    Model          NVARCHAR(60)  NOT NULL,                        -- مدل، مثل «۲۰۷»
    Trim           NVARCHAR(80)  NULL,                            -- تریم/تیپ، مثل «دستی پانوراما»
    BodyType       NVARCHAR(40)  NULL,                            -- نوع بدنه: هاچ‌بک/سدان/شاسی‌بلند
    LiquidityScore INT           NOT NULL CONSTRAINT DF_VM_Liq DEFAULT(50), -- نقدشوندگی دستی ۰..۱۰۰ (سرعت فروش)
    Aliases        NVARCHAR(MAX) NULL,                            -- نام‌های مستعار برای تطبیق متن آگهی (JSON)
    ColorSensitive BIT           NOT NULL CONSTRAINT DF_VM_Color DEFAULT(0), -- آیا رنگ همیشه در قیمت مهم است
    IsActive       BIT           NOT NULL CONSTRAINT DF_VM_Active DEFAULT(1),-- آیا مدل فعال است
    Notes          NVARCHAR(300) NULL,                            -- یادداشت
    CONSTRAINT PK_VehicleModels PRIMARY KEY (Id),
    CONSTRAINT CK_VM_Liquidity CHECK (LiquidityScore BETWEEN 0 AND 100)
);
CREATE INDEX IX_VehicleModels_Brand_Model_Trim ON dbo.VehicleModels(Brand, Model, Trim);
GO

/* ---------------------------------------------- آگهی‌های خام کراول‌شده */
CREATE TABLE dbo.CarListings (
    Id              BIGINT         NOT NULL IDENTITY(1,1),         -- شناسه
    Source          TINYINT        NOT NULL,                      -- منبع: 1=دیوار، 2=باما
    ExternalId      NVARCHAR(80)   NOT NULL,                      -- شناسه‌ی آگهی در منبع (یکتا با Source)
    Url             NVARCHAR(500)  NULL,                          -- آدرس آگهی
    Title           NVARCHAR(200)  NOT NULL,                      -- عنوان آگهی
    [Description]   NVARCHAR(MAX)  NULL,                          -- توضیحات آگهی
    VehicleModelId  INT            NULL,                          -- مدل پس از نرمال‌سازی؛ NULL تا تطبیق
    RawBrand        NVARCHAR(60)   NULL,                          -- برند خام
    RawModel        NVARCHAR(60)   NULL,                          -- مدل خام
    RawTrim         NVARCHAR(80)   NULL,                          -- تریم خام
    ModelYear       INT            NULL,                          -- سال مدل
    MileageKm       INT            NULL,                          -- کارکرد (کیلومتر)؛ NULL=صفر/نامشخص
    Color           NVARCHAR(40)   NULL,                          -- رنگ خودرو
    BodyCondition   TINYINT        NOT NULL CONSTRAINT DF_CL_Body DEFAULT(0), -- 0=نامشخص،1=سالم،2=رنگ،3=تعویض،4=تصادفی
    PriceToman      BIGINT         NOT NULL,                      -- قیمت آگهی (تومان)
    CityId          INT            NULL,                          -- شهر نگاشت‌شده؛ NULL=نامشخص
    RawLocation     NVARCHAR(120)  NULL,                          -- شهر/محله‌ی خام
    AdvertiserPhone NVARCHAR(15)   NULL,                          -- شماره آگهی‌دهنده (فیلتر بلاک)
    ImageCount      INT            NOT NULL CONSTRAINT DF_CL_Img DEFAULT(0), -- تعداد عکس (شرط ذخیره: >۰)
    FirstImageUrl   NVARCHAR(500)  NULL,                          -- آدرس اولین عکس
    PublishedAt     DATETIMEOFFSET NOT NULL,                      -- زمان انتشار (فیلتر ۲۴ ساعت)
    FetchedAt       DATETIMEOFFSET NOT NULL,                      -- زمان اولین دریافت
    LastSeenAt      DATETIMEOFFSET NOT NULL,                      -- آخرین دیده‌شدن (تشخیص حذف)
    [State]         TINYINT        NOT NULL CONSTRAINT DF_CL_State DEFAULT(1), -- 1=فعال، 2=حذف‌شده
    RemovedAt       DATETIMEOFFSET NULL,                          -- زمان تشخیص حذف
    IsScored        BIT            NOT NULL CONSTRAINT DF_CL_Scored DEFAULT(0), -- آیا اسکورینگ پردازش کرده
    ContentHash     NVARCHAR(64)   NULL,                          -- اثرانگشت محتوا (تشخیص تکرار نزدیک)
    RawJson         NVARCHAR(MAX)  NULL,                          -- بدنه‌ی خام آگهی (JSON)
    CONSTRAINT PK_CarListings PRIMARY KEY (Id),
    CONSTRAINT FK_CarListings_VehicleModels FOREIGN KEY (VehicleModelId) REFERENCES dbo.VehicleModels(Id) ON DELETE SET NULL,
    CONSTRAINT FK_CarListings_Cities        FOREIGN KEY (CityId)         REFERENCES dbo.Cities(Id)        ON DELETE SET NULL,
    CONSTRAINT CK_CarListings_Body  CHECK (BodyCondition BETWEEN 0 AND 4),
    CONSTRAINT CK_CarListings_State CHECK ([State] IN (1,2))
);
CREATE UNIQUE INDEX UX_CarListings_Source_External ON dbo.CarListings(Source, ExternalId); -- ضد تکرار
CREATE INDEX IX_CarListings_State_Scored  ON dbo.CarListings([State], IsScored);           -- صف اسکورینگ
CREATE INDEX IX_CarListings_City_Pub_State ON dbo.CarListings(CityId, PublishedAt, [State]);
CREATE INDEX IX_CarListings_Advertiser    ON dbo.CarListings(AdvertiserPhone);
GO

/* ---------------------------------------------- آمار ساعتیِ سگمنت‌ها */
CREATE TABLE dbo.MarketSegmentStats (
    Id                INT            NOT NULL IDENTITY(1,1),       -- شناسه
    SegmentKey        NVARCHAR(120)  NOT NULL,                    -- کلید یکتای سگمنت (upsert)
    VehicleModelId    INT            NOT NULL,                    -- مدل خودرو
    ModelYear         INT            NOT NULL,                    -- سال مدل
    MileageBucket     TINYINT        NOT NULL,                    -- باکت کارکرد: 0=صفر..5=+۱۵۰هزار
    BodyCondition     TINYINT        NOT NULL,                    -- وضعیت بدنه
    ColorKey          NVARCHAR(40)   NULL,                        -- رنگ؛ NULL=در این سگمنت دخیل نیست
    SampleCount       INT            NOT NULL,                    -- تعداد نمونه در پنجره
    MedianPriceToman  BIGINT         NOT NULL,                    -- میانه‌ی قیمت (مرجع اصلی)
    AveragePriceToman BIGINT         NOT NULL,                    -- میانگین قیمت
    P25PriceToman     BIGINT         NOT NULL,                    -- چارک اول قیمت
    P75PriceToman     BIGINT         NOT NULL,                    -- چارک سوم قیمت
    WindowDays        INT            NOT NULL,                    -- طول پنجره‌ی نمونه‌گیری (روز)
    ComputedAt        DATETIMEOFFSET NOT NULL,                    -- زمان آخرین محاسبه
    CONSTRAINT PK_MarketSegmentStats PRIMARY KEY (Id),
    CONSTRAINT FK_MSS_VehicleModels FOREIGN KEY (VehicleModelId) REFERENCES dbo.VehicleModels(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX UX_MSS_SegmentKey ON dbo.MarketSegmentStats(SegmentKey);
CREATE INDEX IX_MSS_Model_Year_Mileage_Body ON dbo.MarketSegmentStats(VehicleModelId, ModelYear, MileageBucket, BodyCondition);
GO

/* ---------------------------------------------- لاگ پاس‌های کراول */
CREATE TABLE dbo.CrawlRuns (
    Id           BIGINT         NOT NULL IDENTITY(1,1),           -- شناسه
    Source       TINYINT        NOT NULL,                        -- منبع کراول
    CityId       INT            NULL,                            -- شهر هدف؛ NULL=همه
    StartedAt    DATETIMEOFFSET NOT NULL,                        -- زمان شروع
    FinishedAt   DATETIMEOFFSET NULL,                            -- زمان پایان
    [Status]     TINYINT        NOT NULL,                        -- 1=درحال‌اجرا،2=موفق،3=خطا
    FetchedCount INT            NOT NULL CONSTRAINT DF_CR_Fetched DEFAULT(0), -- تعداد دریافت‌شده
    SavedCount   INT            NOT NULL CONSTRAINT DF_CR_Saved   DEFAULT(0), -- تعداد تازه‌ی ذخیره‌شده
    SkippedCount INT            NOT NULL CONSTRAINT DF_CR_Skipped DEFAULT(0), -- تعداد ردشده
    RemovedCount INT            NOT NULL CONSTRAINT DF_CR_Removed DEFAULT(0), -- تعداد حذف‌شده
    ErrorCount   INT            NOT NULL CONSTRAINT DF_CR_Errors  DEFAULT(0), -- تعداد خطا
    Error        NVARCHAR(MAX)  NULL,                            -- پیام خطا
    CONSTRAINT PK_CrawlRuns PRIMARY KEY (Id),
    CONSTRAINT CK_CrawlRuns_Status CHECK ([Status] IN (1,2,3))
);
CREATE INDEX IX_CrawlRuns_Source_Started ON dbo.CrawlRuns(Source, StartedAt);
GO

/* توضیحات جدول‌های پایپ‌لاین */
EXEC sys.sp_addextendedproperty 'MS_Description', N'کاتالوگ خودرو + نقدشوندگیِ دستی هر مدل', 'SCHEMA','dbo','TABLE','VehicleModels';
EXEC sys.sp_addextendedproperty 'MS_Description', N'آگهی‌های خام کراول‌شده پیش از اسکورینگ',   'SCHEMA','dbo','TABLE','CarListings';
EXEC sys.sp_addextendedproperty 'MS_Description', N'آمار ساعتیِ هر سگمنت بازار',               'SCHEMA','dbo','TABLE','MarketSegmentStats';
EXEC sys.sp_addextendedproperty 'MS_Description', N'لاگ هر پاس کراول',                         'SCHEMA','dbo','TABLE','CrawlRuns';
GO

/* داده‌ی اولیه‌ی کاتالوگ خودرو (نقدشوندگی قابل‌ویرایش توسط ادمین) */
INSERT INTO dbo.VehicleModels (Brand, Model, Trim, BodyType, LiquidityScore, Aliases, ColorSensitive, IsActive) VALUES
    (N'پژو',      N'۲۰۷',      N'دستی',        N'هاچ‌بک', 90, N'["207","پژو 207","peugeot 207"]', 0, 1),
    (N'پژو',      N'۲۰۶',      N'تیپ ۵',       N'هاچ‌بک', 85, N'["206","پژو 206"]',               0, 1),
    (N'پژو',      N'پارس',     NULL,           N'سدان',   80, N'["pars","پژو پارس"]',             0, 1),
    (N'سایپا',    N'پراید',    N'۱۱۱',         N'هاچ‌بک', 88, N'["pride","پراید 111"]',           0, 1),
    (N'سایپا',    N'تیبا',     NULL,           N'سدان',   78, N'["tiba","تیبا"]',                 0, 1),
    (N'سایپا',    N'کوییک',    N'اتوماتیک',    N'هاچ‌بک', 82, N'["quick","کوییک کراس"]',          0, 1),
    (N'ایران‌خودرو', N'دنا',   N'پلاس توربو',  N'سدان',   84, N'["dena","دنا پلاس"]',             0, 1),
    (N'ایران‌خودرو', N'تارا',  N'اتوماتیک',    N'سدان',   80, N'["tara","تارا"]',                 0, 1),
    (N'ایران‌خودرو', N'سمند',  N'سورن پلاس',   N'سدان',   76, N'["samand","سمند سورن"]',          0, 1),
    (N'ام‌وی‌ام',  N'X55',      N'پرو',         N'شاسی‌بلند', 70, N'["mvm x55","ام وی ام ایکس55"]', 0, 1),
    (N'چانگان',   N'یونی‌وی',  NULL,           N'شاسی‌بلند', 68, N'["changan","univ","یونی وی"]', 0, 1),
    (N'کیا',      N'سراتو',    N'۲۰۰۰',        N'سدان',   66, N'["cerato","کیا سراتو"]',          0, 1),
    (N'هیوندای',  N'سوناتا',   N'LF',          N'سدان',   58, N'["sonata","سوناتا"]',             0, 1),
    (N'بی‌ام‌و',   N'۷۳۰Li',    N'ام‌پکیج',     N'سدان',   45, N'["bmw 730","730li"]',             1, 1),
    (N'بنز',      N'E250',     NULL,           N'سدان',   42, N'["benz e250","e250"]',            1, 1),
    (N'تویوتا',   N'لندکروزر', N'پرادو',       N'شاسی‌بلند', 55, N'["prado","لندکروزر"]',         1, 1);
GO


/* فاز ۴ — لینک شکار به آگهی منبع (خروجی اسکورینگ) */
ALTER TABLE dbo.Hunts ADD SourceListingId BIGINT NULL;  -- آگهی خامِ منبعِ این شکار؛ NULL برای seed/دستی
GO
CREATE INDEX IX_Hunts_SourceListing ON dbo.Hunts(SourceListingId);
GO
EXEC sys.sp_addextendedproperty 'MS_Description', N'آگهی خامِ منبعِ این شکار (خروجی اسکورینگ)؛ NULL برای seed/دستی',
    'SCHEMA','dbo','TABLE','Hunts','COLUMN','SourceListingId';
GO
