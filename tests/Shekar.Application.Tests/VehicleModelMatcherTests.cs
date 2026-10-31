using Shekar.Application.Services;
using Shekar.Domain.Entities;
using Xunit;

namespace Shekar.Application.Tests;

public class VehicleModelMatcherTests
{
    private static readonly IReadOnlyList<VehicleModel> Catalog = new[]
    {
        new VehicleModel { Id = 1, Brand = "پژو", Model = "۲۰۷", Trim = "دستی", Aliases = new[] { "207" }, IsActive = true },
        new VehicleModel { Id = 2, Brand = "پژو", Model = "۲۰۶", Trim = "تیپ ۵", Aliases = new[] { "206" }, IsActive = true },
        new VehicleModel { Id = 4, Brand = "سایپا", Model = "پراید", Trim = "۱۱۱", Aliases = new[] { "pride" }, IsActive = true },
        new VehicleModel { Id = 10, Brand = "ام‌وی‌ام", Model = "X55", Trim = "پرو", Aliases = new[] { "mvm x55" }, IsActive = true }
    };

    private readonly VehicleModelMatcher _matcher = new();

    [Theory]
    [InlineData("پژو ۲۰۷ دنده‌ای پانوراما ۱۴۰۲", 1)]
    [InlineData("پژو ۲۰۶ تیپ ۵ مدل ۱۴۰۱", 2)]
    [InlineData("سایپا پراید ۱۱۱ مدل ۱۳۹۸", 4)]
    [InlineData("MVM X55 Pro اکسلنت", 10)]     // تطبیق با ارقام/لاتین
    [InlineData("پراید 111 دوگانه", 4)]        // ارقام لاتین در متن
    public void Match_FindsExpectedModel(string title, int expectedId)
    {
        var listing = new CarListing { Title = title };
        Assert.Equal(expectedId, _matcher.Match(listing, Catalog));
    }

    [Fact]
    public void Match_ReturnsNull_WhenUnknown()
    {
        var listing = new CarListing { Title = "دوچرخه کوهستان" };
        Assert.Null(_matcher.Match(listing, Catalog));
    }

    [Fact]
    public void Match_PrefersTrimSpecific_PeugeotModels()
    {
        // «پژو ۲۰۷» نباید با «۲۰۶» اشتباه شود.
        var listing = new CarListing { RawBrand = "پژو", RawModel = "۲۰۷", Title = "فروش پژو ۲۰۷" };
        Assert.Equal(1, _matcher.Match(listing, Catalog));
    }
}
