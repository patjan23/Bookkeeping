using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.DTOs;

public class MonthlySummaryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#808080";
    public decimal TotalAmount { get; set; }
    public TransactionType Type { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Populated by ViewModel for bar-chart width
    public double RelativeWidth { get; set; }
}
