using System.Globalization;

namespace NationalParkServiceAPI.Models;

public class Campground
{
    public int CampgroundId { get; set; }
    public int ParkId { get; set; }
    public string CampgroundName { get; set; } = string.Empty;
    public int NightlyRate { get; set; }
    public int MaxGuests { get; set; }
    // First and last nights of the season as "MM-dd", e.g. "05-22" and "09-20". A season can run across New Year
    // ("10-15" to "04-15"). Null means open year-round.
    public string? SeasonOpens { get; set; }
    public string? SeasonCloses { get; set; }

    public bool IsOpenOn(DateOnly night)
    {
        if (SeasonOpens == null || SeasonCloses == null)
        {
            return true;
        }

        var day = night.ToString("MM-dd", CultureInfo.InvariantCulture);
        var afterOpen = string.CompareOrdinal(day, SeasonOpens) >= 0;
        var beforeClose = string.CompareOrdinal(day, SeasonCloses) <= 0;
        return string.CompareOrdinal(SeasonOpens, SeasonCloses) <= 0
            ? afterOpen && beforeClose
            : afterOpen || beforeClose;
    }

    // The first night in [checkIn, checkOut) the campground is closed, or null if it's open every night.
    public DateOnly? FirstClosedNight(DateOnly checkIn, DateOnly checkOut)
    {
        for (var night = checkIn; night < checkOut; night = night.AddDays(1))
        {
            if (!IsOpenOn(night))
            {
                return night;
            }
        }
        return null;
    }

    // e.g. "Big Meadows is closed for the season on Dec 1. It's open Mar 27 to Nov 30."
    public string ClosedMessage(DateOnly night) =>
        $"{CampgroundName} is closed for the season on {night.ToString("MMM d", CultureInfo.InvariantCulture)}. " +
        $"It's open {FormatSeasonDay(SeasonOpens!)} to {FormatSeasonDay(SeasonCloses!)}.";

    // 2000 is a leap year, so "02-29" parses.
    private static string FormatSeasonDay(string monthDay) =>
        DateOnly.ParseExact("2000-" + monthDay, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToString("MMM d", CultureInfo.InvariantCulture);
}
