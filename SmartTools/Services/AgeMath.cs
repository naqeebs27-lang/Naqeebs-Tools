namespace SmartTools.Services;

public sealed class AgeResult
{
    public int Years;
    public int Months;
    public int Days;
    public int TotalMonths;
    public long TotalDays;
    public long Weeks;
    public long RemainingDays;
    public long Hours;
    public long Minutes;
    public long Seconds;
}

public static class AgeMath
{
    public static AgeResult Compute(DateTime birth, DateTime end)
    {
        birth = birth.Date;
        end = end.Date;

        int years = end.Year - birth.Year;
        int months = end.Month - birth.Month;
        int days = end.Day - birth.Day;

        if (days < 0)
        {
            months--;
            // last day of the month before "end"
            int prevMonthLastDay = new DateTime(end.Year, end.Month, 1).AddDays(-1).Day;
            days += prevMonthLastDay;
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        days++;
        long totalDays = (long)(end - birth).TotalDays + 1;
        long hours = totalDays * 24;
        long minutes = hours * 60;
        long seconds = minutes * 60;

        return new AgeResult
        {
            Years = years,
            Months = months,
            Days = days,
            TotalMonths = years * 12 + months,
            TotalDays = totalDays,
            Weeks = totalDays / 7,
            RemainingDays = totalDays % 7,
            Hours = hours,
            Minutes = minutes,
            Seconds = seconds
        };
    }
}
