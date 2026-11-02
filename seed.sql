-- =============================================================
-- Shekar – Seed Data  (اجرا بعد از EF migration)
-- جداول: Cities · Plans · PlanLimits · SystemSettings · VehicleModels
-- =============================================================

-- ---------------------------------------------------------------
-- 1. Cities
-- ---------------------------------------------------------------
SET IDENTITY_INSERT Cities ON;
INSERT INTO Cities (Id, Name, Slug, IsActive, Capacity) VALUES
(1, N'تهران',   'tehran',  1, 100),
(2, N'کرج',    'karaj',   0, 100),
(3, N'اصفهان', 'isfahan', 0, 100),
(4, N'مشهد',   'mashhad', 0, 100);
SET IDENTITY_INSERT Cities OFF;

-- ---------------------------------------------------------------
-- 2. Plans   (Type: 1=شکارچی  2=شکارچی حرفه‌ای)
-- ---------------------------------------------------------------
SET IDENTITY_INSERT Plans ON;
INSERT INTO Plans (Id, Type, Name, MonthlyPriceToman) VALUES
(1, 1, N'شکارچی',          990000),
(2, 2, N'شکارچی حرفه‌ای', 1499000);
SET IDENTITY_INSERT Plans OFF;

-- ---------------------------------------------------------------
-- 3. PlanLimits
-- ---------------------------------------------------------------
INSERT INTO PlanLimits
    (PlanId, UnlimitedAppraisals, MonthlyAppraisalLimit,
     AllowBellAlerts, BellAlertDurationHours, MaxActiveBells,
     TrialAllowed, TrialHours, RealtimeHunts, PrioritySupport)
VALUES
-- شکارچی: کارشناسی نامحدود، بدون زنگوله، دارای ۲۴ ساعت رایگان
(1, 1, NULL, 0, 24, 0,    1, 24, 1, 0),
-- حرفه‌ای: همه‌چیز + زنگوله + اولویت‌پشتیبانی، بدون آزمایشی رایگان
(2, 1, NULL, 1, 24, NULL, 0, 24, 1, 1);

-- ---------------------------------------------------------------
-- 4. SystemSettings
-- ---------------------------------------------------------------
DECLARE @now DATETIMEOFFSET = SYSDATETIMEOFFSET();
INSERT INTO SystemSettings (Key, Value, ValueType, Description, UpdatedAt) VALUES
('CityCapacity',                '100',      'int',     N'R1 — ظرفیت پیش‌فرض اشتراک فعال هر شهر',                           @now),
('FreeAppraisalsPerMonthGuest', '3',        'int',     N'R5 — سهمیه‌ی کارشناسی رایگان ماهانه برای کاربر بدون اشتراک',       @now),
('TrialHours',                  '24',       'int',     N'R3 — مدت اشتراک آزمایشی رایگان (ساعت)',                            @now),
('BellAlertHours',              '24',       'int',     N'R6 — مدت فعال‌بودن زنگوله پیش از خاموش‌شدن خودکار (ساعت)',        @now),
('GoldenScoreThreshold',        '85',       'int',     N'R8 — حداقل امتیاز برای «فرصت طلایی»',                             @now),
('TransactionCostFloorToman',   '15000000', 'int',     N'R9 — کف هزینه‌ی نقل‌وانتقال و فروش (تومان)',                      @now),
('TransactionCostRate',         '0.02',     'decimal', N'R9 — نرخ هزینه‌ی نقل‌وانتقال نسبت به قیمت آگهی',                 @now),
('SupportStartHour',            '9',        'int',     N'R12 — ساعت شروع پشتیبانی (Asia/Tehran)',                           @now),
('SupportEndHour',              '18',       'int',     N'R12 — ساعت پایان پشتیبانی (Asia/Tehran)',                          @now),
('MaxAdsPerMonthBeforeReview',  '20',       'int',     N'آستانه‌ی تعداد آگهی ماهانه برای بررسی دلال/اسپم',                 @now);

-- ---------------------------------------------------------------
-- 5. VehicleModels
--    Aliases: JSON array of strings
--    LiquidityScore: نقدشوندگی ۰..۱۰۰
-- ---------------------------------------------------------------
SET IDENTITY_INSERT VehicleModels ON;
INSERT INTO VehicleModels (Id, Brand, Model, Trim, BodyType, LiquidityScore, Aliases, ColorSensitive, IsActive) VALUES
( 1, N'پژو',         N'۲۰۷',        N'دستی',            N'هاچ‌بک',     90, N'["207","پژو 207","peugeot 207"]',              0, 1),
( 2, N'پژو',         N'۲۰۶',        N'تیپ ۵',           N'هاچ‌بک',     85, N'["206","پژو 206"]',                            0, 1),
( 3, N'پژو',         N'پارس',       NULL,                N'سدان',       80, N'["pars","پژو پارس"]',                          0, 1),
( 4, N'سایپا',       N'پراید',      N'۱۱۱',             N'هاچ‌بک',     88, N'["pride","پراید 111"]',                        0, 1),
( 5, N'سایپا',       N'تیبا',       NULL,                N'سدان',       78, N'["tiba","تیبا"]',                              0, 1),
( 6, N'سایپا',       N'کوییک',      N'اتوماتیک',         N'هاچ‌بک',     82, N'["quick","کوییک کراس"]',                      0, 1),
( 7, N'ایران‌خودرو', N'دنا',        N'پلاس توربو',       N'سدان',       84, N'["dena","دنا پلاس"]',                         0, 1),
( 8, N'ایران‌خودرو', N'تارا',       N'اتوماتیک',         N'سدان',       80, N'["tara","تارا"]',                             0, 1),
( 9, N'ایران‌خودرو', N'سمند',       N'سورن پلاس',        N'سدان',       76, N'["samand","سمند سورن"]',                      0, 1),
(10, N'ام‌وی‌ام',   N'X55',        N'پرو',              N'شاسی‌بلند',  70, N'["mvm x55","ام وی ام ایکس55"]',               0, 1),
(11, N'چانگان',      N'یونی‌وی',    NULL,                N'شاسی‌بلند',  68, N'["changan","univ","یونی وی"]',                0, 1),
(12, N'کیا',         N'سراتو',      N'۲۰۰۰',            N'سدان',       66, N'["cerato","کیا سراتو"]',                      0, 1),
(13, N'هیوندای',     N'سوناتا',     N'LF',               N'سدان',       58, N'["sonata","سوناتا"]',                         0, 1),
(14, N'بی‌ام‌و',    N'۷۳۰Li',      N'ام‌پکیج',          N'سدان',       45, N'["bmw 730","730li"]',                         1, 1),
(15, N'بنز',         N'E250',       NULL,                N'سدان',       42, N'["benz e250","e250"]',                        1, 1),
(16, N'تویوتا',      N'لندکروزر',   N'پرادو',             N'شاسی‌بلند',  55, N'["prado","لندکروزر"]',                       1, 1);
SET IDENTITY_INSERT VehicleModels OFF;
