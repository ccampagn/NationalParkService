using NationalParkServiceAPI.Models;
using Xunit;

namespace NationalParkServiceAPI.Tests;

public class CampgroundTests
{
    [Theory]
    [InlineData("2026-05-07", false)]
    [InlineData("2026-05-08", true)]
    [InlineData("2026-10-31", true)]
    [InlineData("2026-11-01", false)]
    public void IsOpenOn_SummerSeason(string night, bool open)
    {
        var campground = new Campground { SeasonOpens = "05-08", SeasonCloses = "10-31" };

        Assert.Equal(open, campground.IsOpenOn(DateOnly.Parse(night)));
    }

    [Theory]
    [InlineData("2026-10-14", false)]
    [InlineData("2026-10-15", true)]
    [InlineData("2027-01-01", true)]
    [InlineData("2027-04-15", true)]
    [InlineData("2027-04-16", false)]
    [InlineData("2027-07-04", false)]
    public void IsOpenOn_SeasonAcrossNewYear(string night, bool open)
    {
        var campground = new Campground { SeasonOpens = "10-15", SeasonCloses = "04-15" };

        Assert.Equal(open, campground.IsOpenOn(DateOnly.Parse(night)));
    }

    [Fact]
    public void IsOpenOn_YearRoundWithoutASeason()
    {
        Assert.True(new Campground().IsOpenOn(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void FirstClosedNight_IgnoresTheCheckOutMorning()
    {
        var campground = new Campground { SeasonOpens = "05-08", SeasonCloses = "10-31" };

        Assert.Null(campground.FirstClosedNight(new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 1)));
        Assert.Equal(new DateOnly(2026, 11, 1), campground.FirstClosedNight(new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 2)));
    }
}
