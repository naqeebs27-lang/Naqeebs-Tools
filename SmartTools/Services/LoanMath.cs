namespace SmartTools.Services;

public record ScheduleRow(int Year, double Principal, double Interest, double Payment, double Balance);

/// <summary>EMI maths and year-wise amortisation (same formulas as the original web calculator).</summary>
public static class LoanMath
{
    public const double MaxMonths = 1200;

    public sealed class Result
    {
        public double Emi;
        public double Principal;
        public double Interest;
        public double Total;
        public List<ScheduleRow> Schedule = new List<ScheduleRow>();
    }

    public static Result Calculate(double principal, double annualRatePercent, double totalMonths)
    {
        var result = new Result();
        if (principal <= 0 || totalMonths <= 0) return result;
        if (totalMonths > MaxMonths) totalMonths = MaxMonths;

        double monthlyRate;
        double emi;
        double totalPayment;

        if (annualRatePercent <= 0)
        {
            monthlyRate = 0;
            emi = principal / totalMonths;
            totalPayment = principal;
        }
        else
        {
            monthlyRate = (annualRatePercent / 12.0) / 100.0;
            double pow = Math.Pow(1 + monthlyRate, totalMonths);
            emi = (principal * monthlyRate * pow) / (pow - 1);
            totalPayment = emi * totalMonths;
        }

        if (double.IsNaN(emi) || double.IsInfinity(emi) || double.IsNaN(totalPayment) || double.IsInfinity(totalPayment))
        {
            return new Result();
        }

        result.Emi = emi;
        result.Principal = principal;
        result.Total = totalPayment;
        result.Interest = Math.Max(0, totalPayment - principal);
        result.Schedule = BuildSchedule(principal, monthlyRate, totalMonths, emi);
        return result;
    }

    private static List<ScheduleRow> BuildSchedule(double principal, double monthlyRate, double totalMonths, double emi)
    {
        var rows = new List<ScheduleRow>();
        double balance = principal;
        int year = 1;
        double yearlyPrincipal = 0;
        double yearlyInterest = 0;

        for (int m = 1; m <= totalMonths; m++)
        {
            double interestForMonth = balance * monthlyRate;
            double principalForMonth = emi - interestForMonth;
            balance -= principalForMonth;
            if (balance < 0) balance = 0;

            yearlyPrincipal += principalForMonth;
            yearlyInterest += interestForMonth;

            if (m % 12 == 0 || m == totalMonths)
            {
                rows.Add(new ScheduleRow(year, yearlyPrincipal, yearlyInterest, yearlyPrincipal + yearlyInterest, balance));
                year++;
                yearlyPrincipal = 0;
                yearlyInterest = 0;
            }
        }
        return rows;
    }
}
