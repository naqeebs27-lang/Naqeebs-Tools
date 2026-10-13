namespace SmartTools.Services;

/// <summary>
/// Pakistan income-tax slabs. The salaried slabs are transcribed 1:1 from the original calculator
/// (tax years 2015 to 2027); the business slabs follow the original FBR business calculator.
/// These are estimates only - always confirm with FBR / a tax adviser.
/// </summary>
public static class TaxMath
{
    /// <summary>Annual income tax for a salaried person. year is the tax year (e.g. 2027 for 2026-2027).</summary>
    public static double SalariedAnnualTax(double income, int year)
    {
        double tax = 0;
    if (year == 2027) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.01;
        else if (income <= 2200000) tax = 6000 + (income - 1200000) * 0.11;
        else if (income <= 3200000) tax = 116000 + (income - 2200000) * 0.20;
        else if (income <= 4100000) tax = 316000 + (income - 3200000) * 0.25;
        else if (income <= 5600000) tax = 541000 + (income - 4100000) * 0.29;
        else if (income <= 7000000) tax = 976000 + (income - 5600000) * 0.32;
        else tax = 1424000 + (income - 7000000) * 0.35;
    }
    else if (year == 2026) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.01;
        else if (income <= 2200000) tax = 6000 + (income - 1200000) * 0.11;
        else if (income <= 3200000) tax = 116000 + (income - 2200000) * 0.23;
        else if (income <= 4100000) tax = 346000 + (income - 3200000) * 0.30;
        else tax = 616000 + (income - 4100000) * 0.35;
    }
    else if (year == 2025) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.05;
        else if (income <= 2200000) tax = 30000 + (income - 1200000) * 0.15;
        else if (income <= 3200000) tax = 180000 + (income - 2200000) * 0.25;
        else if (income <= 4100000) tax = 430000 + (income - 3200000) * 0.30;
        else tax = 700000 + (income - 4100000) * 0.35;
    }
    else if (year == 2024) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.025;
        else if (income <= 2400000) tax = 15000 + (income - 1200000) * 0.125;
        else if (income <= 3600000) tax = 165000 + (income - 2400000) * 0.225;
        else if (income <= 6000000) tax = 435000 + (income - 3600000) * 0.275;
        else tax = 1095000 + (income - 6000000) * 0.35;
    }
    else if (year == 2023) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.025;
        else if (income <= 2400000) tax = 15000 + (income - 1200000) * 0.125;
        else if (income <= 3600000) tax = 165000 + (income - 2400000) * 0.20;
        else if (income <= 6000000) tax = 405000 + (income - 3600000) * 0.25;
        else if (income <= 12000000) tax = 1005000 + (income - 6000000) * 0.325;
        else tax = 2955000 + (income - 12000000) * 0.35;
    }
    else if (year == 2022 || year == 2021 || year == 2020) {
        if (income <= 600000) tax = 0;
        else if (income <= 1200000) tax = (income - 600000) * 0.05;
        else if (income <= 1800000) tax = 30000 + (income - 1200000) * 0.10;
        else if (income <= 2500000) tax = 90000 + (income - 1800000) * 0.15;
        else if (income <= 3500000) tax = 195000 + (income - 2500000) * 0.175;
        else if (income <= 5000000) tax = 370000 + (income - 3500000) * 0.20;
        else if (income <= 8000000) tax = 670000 + (income - 5000000) * 0.225;
        else if (income <= 12000000) tax = 1345000 + (income - 8000000) * 0.25;
        else if (income <= 30000000) tax = 2345000 + (income - 12000000) * 0.275;
        else if (income <= 50000000) tax = 7295000 + (income - 30000000) * 0.30;
        else if (income <= 75000000) tax = 13295000 + (income - 50000000) * 0.325;
        else tax = 21420000 + (income - 75000000) * 0.35;
    }
    else if (year == 2019) {
        if (income <= 400000) tax = 0;
        else if (income <= 800000) tax = 1000;
        else if (income <= 1200000) tax = 2000;
        else if (income <= 2500000) tax = Math.Max(2000, (income - 1200000) * 0.05);
        else if (income <= 4000000) tax = 65000 + (income - 2500000) * 0.15;
        else if (income <= 8000000) tax = 290000 + (income - 4000000) * 0.20;
        else tax = 1090000 + (income - 8000000) * 0.25;
    }
    else if (year == 2018 || year == 2017 || year == 2016) {
        if (income <= 400000) tax = 0;
        else if (income <= 500000) tax = (income - 400000) * 0.02;
        else if (income <= 750000) tax = 2000 + (income - 500000) * 0.05;
        else if (income <= 1400000) tax = 14500 + (income - 750000) * 0.10;
        else if (income <= 1500000) tax = 79500 + (income - 1400000) * 0.125;
        else if (income <= 1800000) tax = 92000 + (income - 1500000) * 0.15;
        else if (income <= 2500000) tax = 137000 + (income - 1800000) * 0.175;
        else if (income <= 3000000) tax = 259500 + (income - 2500000) * 0.20;
        else if (income <= 3500000) tax = 359500 + (income - 3000000) * 0.225;
        else if (income <= 4000000) tax = 472000 + (income - 3500000) * 0.25;
        else if (income <= 7000000) tax = 597000 + (income - 4000000) * 0.275;
        else tax = 1422000 + (income - 7000000) * 0.30;
    }
    else if (year == 2015) {
        if (income <= 400000) tax = 0;
        else if (income <= 750000) tax = (income - 400000) * 0.05;
        else if (income <= 1400000) tax = 17500 + (income - 750000) * 0.10;
        else if (income <= 1500000) tax = 82500 + (income - 1400000) * 0.125;
        else if (income <= 1800000) tax = 95000 + (income - 1500000) * 0.15;
        else if (income <= 2500000) tax = 140000 + (income - 1800000) * 0.175;
        else if (income <= 3000000) tax = 262500 + (income - 2500000) * 0.20;
        else if (income <= 3500000) tax = 362500 + (income - 3000000) * 0.225;
        else if (income <= 4000000) tax = 475000 + (income - 3500000) * 0.25;
        else if (income <= 7000000) tax = 600000 + (income - 4000000) * 0.275;
        else tax = 1425000 + (income - 7000000) * 0.30;
    }
        return tax;
    }

    public sealed class BusinessTaxResult
    {
        public double Tax;
        public string Note = "";
    }

    /// <summary>Annual tax for a non-salaried / business individual (and AOP). year is the tax year.</summary>
    public static BusinessTaxResult BusinessAnnualTax(double income, int year)
    {
        double tax = 0;
        string note = "";

        if (year == 2027 || year == 2026 || year == 2025)
        {
            if (income <= 600000)
            {
                tax = 0;
            }
            else if (income <= 1200000)
            {
                tax = (income - 600000) * 0.15;
            }
            else if (income <= 1600000)
            {
                tax = 90000 + (income - 1200000) * 0.20;
            }
            else if (income <= 3200000)
            {
                tax = 170000 + (income - 1600000) * 0.30;
            }
            else if (income <= 5600000)
            {
                tax = 650000 + (income - 3200000) * 0.40;
            }
            else
            {
                tax = 1610000 + (income - 5600000) * 0.45;
                double cap = income * 0.40;
                if (tax > cap)
                {
                    tax = cap;
                    note = "*Professional AOP cap (40% of total income) applied.";
                }
            }

            // 10% surcharge under Section 4AB for business / non-salaried income above 10 million
            if (income > 10000000)
            {
                tax = tax * 1.10;
                if (note != "") note += " | ";
                note += "*Includes 10% Surcharge for business income exceeding Rs. 10 Million.";
            }
        }
        else if (year == 2024)
        {
            // Finance Act 2023 rates (TY 2023-2024)
            if (income <= 600000)
            {
                tax = 0;
            }
            else if (income <= 800000)
            {
                tax = (income - 600000) * 0.075;
            }
            else if (income <= 1200000)
            {
                tax = 15000 + (income - 800000) * 0.15;
            }
            else if (income <= 2400000)
            {
                tax = 75000 + (income - 1200000) * 0.20;
            }
            else if (income <= 3000000)
            {
                tax = 315000 + (income - 2400000) * 0.25;
            }
            else if (income <= 4000000)
            {
                tax = 465000 + (income - 3000000) * 0.30;
            }
            else
            {
                tax = 765000 + (income - 4000000) * 0.35;
            }
        }
        else if (year == 2023)
        {
            // Finance Act 2022 rates (TY 2022-2023)
            if (income <= 600000)
            {
                tax = 0;
            }
            else if (income <= 1200000)
            {
                tax = (income - 600000) * 0.05;
            }
            else if (income <= 2400000)
            {
                tax = 30000 + (income - 1200000) * 0.125;
            }
            else if (income <= 3000000)
            {
                tax = 180000 + (income - 2400000) * 0.175;
            }
            else if (income <= 4000000)
            {
                tax = 285000 + (income - 3000000) * 0.225;
            }
            else if (income <= 6000000)
            {
                tax = 510000 + (income - 4000000) * 0.275;
            }
            else
            {
                tax = 1060000 + (income - 6000000) * 0.35;
            }
        }

        return new BusinessTaxResult { Tax = tax, Note = note };
    }
}
