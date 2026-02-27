namespace Bookkeeping.Core.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Net => TotalIncome - TotalExpenses;
    public int Month { get; set; }
    public int Year { get; set; }
}
